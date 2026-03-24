using Finbuckle.MultiTenant.Abstractions;
using InteractiveLeads.Application;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Identity.Impersonation;
using InteractiveLeads.Application.Feature.Identity.Tokens;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Infrastructure.Constants;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using InteractiveLeads.Infrastructure.Tenancy.Strategies;
using InteractiveLeads.Infrastructure.Identity.Models;
using InteractiveLeads.Infrastructure.Context.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace InteractiveLeads.Infrastructure.Identity.Tokens
{
    public class TokenService : ITokenService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMultiTenantContextAccessor<InteractiveTenantInfo> _multiTenantContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenService(
            UserManager<ApplicationUser> userManager,
            IMultiTenantContextAccessor<InteractiveTenantInfo> multiTenantContextAccessor,
            ApplicationDbContext context,
            IOptions<JwtSettings> jwtSettings,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _multiTenantContextAccessor = multiTenantContextAccessor;
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<SingleResponse<TokenResponse>> LoginAsync(TokenRequest request, CancellationToken ct = default)
        {
            ResultResponse result = new();

            #region validations
            if (_multiTenantContextAccessor.MultiTenantContext.TenantInfo is null)
            {
                result.AddErrorMessage("Incorrect username or password", "auth.invalid_credentials");
                throw new UnauthorizedException(result);
            }

            var tenantInfo = _multiTenantContextAccessor.MultiTenantContext.TenantInfo;
            bool isGlobalContext = tenantInfo.Id == null;

            // Allow login even when tenant is deactivated or expired so users can enter to edit/reactivate.
            // Note: the multi-tenant strategy resolves tenant by treating `userName` as email for /token/login.
            // Therefore, support both username and email for Identity lookup.
            var normalizedInput = (request.UserName ?? string.Empty).Trim();
            var userInDb = await FindUserForLoginAsync(normalizedInput, tenantInfo.Id);
            if (userInDb is null || !await _userManager.CheckPasswordAsync(userInDb, request.Password))
            {
                result.AddErrorMessage("Incorrect username or password", "auth.invalid_credentials");
                throw new UnauthorizedException(result);
            }

            var tenantId = tenantInfo.Id;
            var userRoles = await GetUserRoleNamesAsync(userInDb.Id);
            var hasGlobalRole = userRoles.Any(r => RoleConstants.CrossTenantRoles.Contains(r));
            var hasOnlyTenantRoles = userRoles.All(r => RoleConstants.TenantRoles.Contains(r));

            if (isGlobalContext)
            {
                if (!hasGlobalRole || userInDb.TenantId != null)
                {
                    result.AddErrorMessage("Incorrect username or password", "auth.invalid_credentials");
                    throw new UnauthorizedException(result);
                }
            }
            else
            {
                if (hasOnlyTenantRoles && userInDb.TenantId != tenantId)
                {
                    result.AddErrorMessage("Incorrect username or password", "auth.invalid_credentials");
                    throw new UnauthorizedException(result);
                }
                if (hasGlobalRole)
                {
                    result.AddErrorMessage("Incorrect username or password", "auth.invalid_credentials");
                    throw new UnauthorizedException(result);
                }
            }

            if (!userInDb.IsActive)
            {
                result.AddErrorMessage("User not active. Contact administrator.", "auth.user_not_active");
                throw new UnauthorizedException(result);
            }

            // Allow login when tenant is expired or deactivated so user can enter to edit/reactivate/renew.
            #endregion

            var tokenResponse = await GenerateJwtTokenAndUpdateUserAsync(userInDb);
            return new SingleResponse<TokenResponse>(tokenResponse);
        }

        public async Task<SingleResponse<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
        {
            ResultResponse response = new();

            var userPrincipal = GetClaimsPrincipalFromExpiringToken(request.CurrentJwt);
            var userEmail = userPrincipal.GetEmail();

            var userInDb = await _userManager.FindByEmailAsync(userEmail);
            if (userInDb is null)
            {
                response.AddErrorMessage("Authentication failed.", "auth.authentication_failed");
                throw new UnauthorizedException(response);
            }

            // Find the refresh token in the database
            var hashedRefreshToken = HashRefreshToken(request.CurrentRefreshToken);
            var refreshTokenInDb = await _context.RefreshTokens
                .Where(rt => rt.UserId == userInDb.Id && rt.Token == hashedRefreshToken && !rt.IsRevoked)
                .FirstOrDefaultAsync(ct);

            if (refreshTokenInDb is null)
            {
                response.AddErrorMessage("Invalid refresh token.", "auth.invalid_refresh_token");
                throw new UnauthorizedException(response);
            }

            // Check if the refresh token has not expired
            if (refreshTokenInDb.ExpirationTime < DateTime.UtcNow)
            {
                response.AddErrorMessage("Refresh token expired.", "auth.refresh_token_expired");
                throw new UnauthorizedException(response);
            }

            // Revoke the current refresh token (one-time use)
            refreshTokenInDb.IsRevoked = true;
            refreshTokenInDb.UpdatedAt = DateTime.UtcNow;

            var tokenResponse = await GenerateJwtTokenAndUpdateUserAsync(userInDb);
            
            var tokenResponseObject = new SingleResponse<TokenResponse>(tokenResponse);
            tokenResponseObject.AddSuccessMessage("Token refreshed successfully", "auth.token_refreshed_successfully");
            return tokenResponseObject;
        }

        private ClaimsPrincipal GetClaimsPrincipalFromExpiringToken(string expiringToken)
        {
            ResultResponse response = new();

            var tokenValidationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role,
                ValidateLifetime = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(expiringToken, tokenValidationParams, out var securitytoken);

            if (securitytoken is not JwtSecurityToken jwtSecurityToken 
                || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase)) 
            {
                response.AddErrorMessage("Invalid token provided. Failed to generate new token.", "auth.invalid_token");
                throw new UnauthorizedException(response);
            }

            return principal;
        }

        private async Task<TokenResponse> GenerateJwtTokenAndUpdateUserAsync(ApplicationUser user)
        {
            var newJwt = await GenerateJwtTokenAsync(user);

            // Generate new refresh token
            var refreshTokenValue = GenerateRefreshToken();
            var hashedRefreshToken = HashRefreshToken(refreshTokenValue);

            // Create new refresh token in the database
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = hashedRefreshToken,
                ExpirationTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshExpiresInDays),
                IsRevoked = false,
                DeviceInfo = ResolveDeviceInfo(),
                IpAddress = ResolveIpAddress(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Clean up expired refresh tokens for the user
            await CleanExpiredRefreshTokensAsync(user.Id);

            return new TokenResponse
            {
                Jwt = newJwt,
                RefreshToken = refreshTokenValue, // Return the original value (not hashed)
                RefreshTokenExpirationDate = refreshToken.ExpirationTime
            };
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            return GenerateEncryptedToken(GenerateSigningCredentials(), await GetUserClaimsAsync(user));
        }

        private string GenerateEncryptedToken(SigningCredentials signingCredentials, IEnumerable<Claim> claims)
        {
            var token = new JwtSecurityToken(
                claims: claims,
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.TokenExpiresInMinutes),
               signingCredentials: signingCredentials);

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }

        private SigningCredentials GenerateSigningCredentials()
        {
            byte[] secret = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
            return new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256);
        }

        private async Task<IEnumerable<Claim>> GetUserClaimsAsync(ApplicationUser user)
        {
            var userRoles = await GetUserRoleNamesAsync(user.Id);
            var roleClaims = userRoles
                .Select(role => new Claim(ClaimTypes.Role, role))
                .ToList();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
                new(JwtTenantFallbackStrategy.TenantIdClaimType, user.TenantId ?? string.Empty),
            };
            claims.AddRange(roleClaims);
            return claims;
        }

        private async Task<List<string>> GetUserRoleNamesAsync(Guid userId)
        {
            var userRoleIds = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (userRoleIds.Count == 0)
                return [];

            // Important after Finbuckle/EF upgrades: role entity may have tenant filters.
            // Ignore filters so JWT always gets the effective roles assigned to the user.
            return await _context.Roles
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(r => userRoleIds.Contains(r.Id))
                .Select(r => r.Name!)
                .Distinct()
                .ToListAsync();
        }

        private async Task<ApplicationUser?> FindUserForLoginAsync(string loginInput, string? tenantId)
        {
            // 1) Fast path through Identity APIs (respects current tenant context).
            var user = await _userManager.FindByNameAsync(loginInput);
            if (user != null)
                return user;

            user = await _userManager.FindByEmailAsync(loginInput);
            if (user != null)
                return user;

            // 2) Fallback for tenant-resolution edge cases after Finbuckle/EF upgrades.
            // Query users ignoring filters, then constrain by resolved tenant context.
            var normalized = loginInput.ToUpperInvariant();
            var query = _context.Users
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(u => u.NormalizedUserName == normalized || u.NormalizedEmail == normalized);

            if (tenantId == null)
                query = query.Where(u => u.TenantId == null);
            else
                query = query.Where(u => u.TenantId == tenantId);

            return await query.FirstOrDefaultAsync();
        }

        private static string GenerateRefreshToken()
        {
            byte[] randomNumber = new byte[32];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Generates a SHA-256 hash of the refresh token for secure storage.
        /// </summary>
        /// <param name="refreshToken">The refresh token in plain text.</param>
        /// <returns>The SHA-256 hash of the refresh token.</returns>
        private static string HashRefreshToken(string refreshToken)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// Removes all expired refresh tokens for the specified user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        private async Task CleanExpiredRefreshTokensAsync(Guid userId)
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.ExpirationTime < DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Revokes all active refresh tokens for a user (useful for logout from all devices).
        /// </summary>
        /// <param name="userId">The user ID.</param>
        public async Task<ResultResponse> RevokeUserRefreshTokensAsync(Guid userId)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpirationTime > DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = DateTime.UtcNow;
            }

            if (activeTokens.Any())
            {
                await _context.SaveChangesAsync();
            }

            var response = new ResultResponse();
            response.AddSuccessMessage("Logout successful", "auth.logout_successful");
            return response;
        }

        /// <summary>
        /// Revokes a specific refresh token for a user (useful for logout from current device).
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="refreshToken">The refresh token to revoke.</param>
        public async Task<ResultResponse> RevokeSpecificRefreshTokenAsync(Guid userId, string refreshToken)
        {
            var hashedRefreshToken = HashRefreshToken(refreshToken);
            var tokenToRevoke = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.Token == hashedRefreshToken && !rt.IsRevoked && rt.ExpirationTime > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            var response = new ResultResponse();

            if (tokenToRevoke == null)
            {
                response.AddErrorMessage("Refresh token not found or already revoked", "auth.token_not_found_or_revoked");
                return response;
            }

            tokenToRevoke.IsRevoked = true;
            tokenToRevoke.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            response.AddSuccessMessage("Device logout successful", "auth.device_logout_successful");
            return response;
        }

        /// <inheritdoc />
        public async Task<SingleResponse<TokenResponse>> GenerateTokenForImpersonationAsync(UserLookupResult targetUser, Guid impersonatedByUserId, CancellationToken ct = default)
        {
            var claims = BuildClaimsForImpersonation(targetUser, impersonatedByUserId);
            var newJwt = GenerateEncryptedToken(GenerateSigningCredentials(), claims);

            var refreshTokenValue = GenerateRefreshToken();
            var hashedRefreshToken = HashRefreshToken(refreshTokenValue);
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = targetUser.Id,
                Token = hashedRefreshToken,
                ExpirationTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshExpiresInDays),
                IsRevoked = false,
                DeviceInfo = ResolveDeviceInfo(),
                IpAddress = ResolveIpAddress(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync(ct);
            await CleanExpiredRefreshTokensAsync(targetUser.Id);

            var tokenResponse = new TokenResponse
            {
                Jwt = newJwt,
                RefreshToken = refreshTokenValue,
                RefreshTokenExpirationDate = refreshToken.ExpirationTime
            };
            var response = new SingleResponse<TokenResponse>(tokenResponse);
            response.AddSuccessMessage("Impersonation token issued.", "auth.impersonation_successful");
            return response;
        }

        private static List<Claim> BuildClaimsForImpersonation(UserLookupResult targetUser, Guid impersonatedByUserId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, targetUser.Id.ToString()),
                new(ClaimTypes.Email, targetUser.Email ?? string.Empty),
                new(ClaimTypes.Name, targetUser.FirstName),
                new(ClaimTypes.Surname, targetUser.LastName),
                new(ClaimTypes.MobilePhone, targetUser.PhoneNumber ?? string.Empty),
                new(JwtTenantFallbackStrategy.TenantIdClaimType, targetUser.TenantId ?? string.Empty),
                new(JwtTenantFallbackStrategy.ImpersonatedByClaimType, impersonatedByUserId.ToString())
            };
            foreach (var role in targetUser.Roles)
                claims.Add(new Claim(ClaimTypes.Role, role));
            return claims;
        }

        private string? ResolveDeviceInfo()
        {
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString().Trim();
            if (string.IsNullOrWhiteSpace(userAgent))
                return null;

            // DB limit for RefreshToken.DeviceInfo.
            return userAgent.Length > 200 ? userAgent[..200] : userAgent;
        }

        private string? ResolveIpAddress()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is null)
                return null;

            var forwardedFor = request.Headers["X-Forwarded-For"].ToString();
            var candidate = !string.IsNullOrWhiteSpace(forwardedFor)
                ? forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()
                : request.HttpContext.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrWhiteSpace(candidate))
                return null;

            // DB limit for RefreshToken.IpAddress.
            return candidate.Length > 45 ? candidate[..45] : candidate;
        }
    }
}
