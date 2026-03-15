namespace InteractiveLeads.Infrastructure.Identity.Models
{
    /// <summary>
    /// Represents a refresh token used for authentication token renewal.
    /// This entity is separate from ApplicationUser to provide better security,
    /// flexibility for multiple device support, and improved audit capabilities.
    /// </summary>
    public class RefreshToken : AuditableEntity
    {
        /// <summary>
        /// Unique identifier for the refresh token.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Foreign key reference to the user who owns this refresh token.
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// The actual refresh token value (hashed/encrypted).
        /// </summary>
        public string Token { get; set; } = string.Empty;
        
        /// <summary>
        /// When this refresh token expires and becomes invalid.
        /// </summary>
        public DateTime ExpirationTime { get; set; }
        
        /// <summary>
        /// Indicates whether this refresh token has been revoked.
        /// Revoked tokens cannot be used for authentication.
        /// </summary>
        public bool IsRevoked { get; set; }
        
        /// <summary>
        /// Optional information about the device that created this token.
        /// Useful for security auditing and device management.
        /// </summary>
        public string? DeviceInfo { get; set; }
        
        /// <summary>
        /// Optional IP address from which this token was created.
        /// Useful for security auditing and suspicious activity detection.
        /// </summary>
        public string? IpAddress { get; set; }
        
        /// <summary>
        /// Navigation property to the user who owns this refresh token.
        /// </summary>
        public ApplicationUser User { get; set; } = null!;
    }
}
