using System.Text.RegularExpressions;
using InteractiveLeads.Application.Exceptions;
using InteractiveLeads.Application.Feature.Chat;
using InteractiveLeads.Application.Feature.Inbound.Media;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Responses;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace InteractiveLeads.Application.Feature.Chat.Media;

public sealed partial class ConversationMediaUploadService(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IMediaStorageGateway storageGateway,
    IImageProcessor imageProcessor,
    IMediaContentInspector contentInspector,
    IOptions<OutboundMediaUploadOptions> options,
    ILogger<ConversationMediaUploadService> logger) : IConversationMediaUploadService
{
    private readonly OutboundMediaUploadOptions _options = options.Value;

    public async Task<ConversationMediaUploadResultDto> UploadAsync(
        UploadConversationMediaRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength <= 0)
        {
            var empty = new ResultResponse();
            empty.AddErrorMessage("File is empty.", "chat.media.empty_file");
            throw new BadRequestException(empty);
        }

        var companyId = await ChatContext.GetCompanyIdAsync(db, currentUserService, cancellationToken);

        var conversation = await db.Conversations
            .AsNoTracking()
            .Include(c => c.Inbox)
            .Include(c => c.Integration)
            .Where(c => c.Id == request.ConversationId && c.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Conversation not found.", "general.not_found");
            throw new NotFoundException(response);
        }

        await ChatContext.EnsureInboxAccessAsync(db, currentUserService, conversation.InboxId, companyId, cancellationToken);

        if (conversation.Integration.Type != IntegrationType.WhatsApp)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Media upload is only supported for WhatsApp conversations.", "chat.media.integration_not_supported");
            throw new BadRequestException(response);
        }

        var tenantId = currentUserService.GetUserTenant();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Tenant context is required.", "general.bad_request");
            throw new BadRequestException(response);
        }

        var safeName = SanitizeFileName(request.FileName);
        var normalizedMime = NormalizeMime(request.ContentType);
        var hint = request.MediaType?.Trim().ToLowerInvariant();

        var (category, effectiveMime) = ResolveCategoryAndMime(normalizedMime, hint, safeName);

        ValidateAgainstAllowList(category, effectiveMime);

        var maxBytes = category switch
        {
            "image" => _options.MaxImageBytes,
            "document" => _options.MaxDocumentBytes,
            "audio" => _options.MaxAudioBytes,
            _ => _options.MaxDocumentBytes
        };

        if (request.ContentLength > maxBytes)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("File exceeds the maximum allowed size for this media type.", "chat.media.file_too_large");
            throw new BadRequestException(response);
        }

        var root = _options.StorageRootPrefix.Trim().Trim('/');
        var tenantSegment = tenantId.Trim();

        if (category == "image")
        {
            return await UploadImageRawForDeliveryAndProcessVariantsForCrmAsync(
                request,
                safeName,
                effectiveMime,
                tenantSegment,
                root,
                cancellationToken);
        }

        var unique = Guid.NewGuid().ToString("N");
        var mediaFolder = InboundStyleMediaFolder(category);
        var objectKey = $"{root}/{tenantSegment}/{mediaFolder}/{unique}_{safeName}";

        var descriptor = await storageGateway.UploadAsync(objectKey, request.Content, effectiveMime, cancellationToken);

        return new ConversationMediaUploadResultDto
        {
            Url = descriptor.PublicUrl,
            ObjectKey = descriptor.ObjectKey,
            FileName = safeName,
            MimeType = effectiveMime,
            SizeBytes = request.ContentLength,
            MediaType = category
        };
    }

    /// <summary>
    /// Stores the file <b>unchanged</b> in S3 (<see cref="ConversationMediaUploadResultDto.Url"/> → RabbitMQ / WhatsApp).
    /// Runs <see cref="IImageProcessor"/> afterwards only to produce CRM WebP variants (optional; failures do not fail upload).
    /// </summary>
    private async Task<ConversationMediaUploadResultDto> UploadImageRawForDeliveryAndProcessVariantsForCrmAsync(
        UploadConversationMediaRequest request,
        string safeName,
        string effectiveMime,
        string tenantSegment,
        string root,
        CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length == 0)
        {
            var empty = new ResultResponse();
            empty.AddErrorMessage("File is empty.", "chat.media.empty_file");
            throw new BadRequestException(empty);
        }

        var unique = Guid.NewGuid().ToString("N");
        var rawKey = $"{root}/{tenantSegment}/{InboundStyleMediaFolder("image")}/{unique}_{safeName}";
        buffer.Position = 0;
        var rawDescriptor = await storageGateway.UploadAsync(rawKey, buffer, effectiveMime, cancellationToken);

        string? optimizedUrl = null;
        string? thumbnailUrl = null;

        buffer.Position = 0;
        string hash;
        try
        {
            hash = await contentInspector.ComputeSha256Async(buffer, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to hash outbound image for CRM variants");
            return new ConversationMediaUploadResultDto
            {
                Url = rawDescriptor.PublicUrl,
                ObjectKey = rawDescriptor.ObjectKey,
                FileName = safeName,
                MimeType = effectiveMime,
                SizeBytes = buffer.Length,
                MediaType = "image",
                OriginalUrl = rawDescriptor.PublicUrl
            };
        }

        buffer.Position = 0;
        try
        {
            var result = await imageProcessor.ProcessAsync(
                buffer,
                new ProcessMediaRequest
                {
                    TenantId = tenantSegment,
                    MediaUrl = string.Empty,
                    MediaType = "image",
                    MimeType = effectiveMime,
                    OriginalFileName = safeName
                },
                hash,
                cancellationToken);
            optimizedUrl = result.OptimizedUrl;
            thumbnailUrl = result.ThumbnailUrl;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "CRM image variants skipped; raw outbound file was stored successfully");
        }

        return new ConversationMediaUploadResultDto
        {
            Url = rawDescriptor.PublicUrl,
            ObjectKey = rawDescriptor.ObjectKey,
            FileName = safeName,
            MimeType = effectiveMime,
            SizeBytes = buffer.Length,
            MediaType = "image",
            OriginalUrl = rawDescriptor.PublicUrl,
            OptimizedUrl = optimizedUrl,
            ThumbnailUrl = thumbnailUrl
        };
    }

    private void ValidateAgainstAllowList(string category, string mime)
    {
        var allowed = category switch
        {
            "image" => _options.AllowedImageMimeTypes,
            "document" => _options.AllowedDocumentMimeTypes,
            "audio" => _options.AllowedAudioMimeTypes,
            _ => _options.AllowedDocumentMimeTypes
        };

        if (!allowed.Any(m => string.Equals(m, mime, StringComparison.OrdinalIgnoreCase)))
        {
            var response = new ResultResponse();
            response.AddErrorMessage("File type is not allowed.", "chat.media.mime_not_allowed");
            throw new BadRequestException(response);
        }
    }

    private (string category, string effectiveMime) ResolveCategoryAndMime(string normalizedMime, string? hint, string safeFileName)
    {
        var fromDeclared = TryCategoryForMime(normalizedMime);
        if (fromDeclared != null)
        {
            if (!string.IsNullOrEmpty(hint) && !string.Equals(hint, fromDeclared.Value.category, StringComparison.Ordinal))
            {
                var response = new ResultResponse();
                response.AddErrorMessage("mediaType does not match file content.", "chat.media.hint_mismatch");
                throw new BadRequestException(response);
            }

            return (fromDeclared.Value.category, fromDeclared.Value.mime);
        }

        var ext = Path.GetExtension(safeFileName).ToLowerInvariant();
        var inferredMime = TryInferMimeFromExtension(ext);
        if (inferredMime != null)
        {
            var fromInferred = TryCategoryForMime(inferredMime);
            if (fromInferred != null)
            {
                if (!string.IsNullOrEmpty(hint) && !string.Equals(hint, fromInferred.Value.category, StringComparison.Ordinal))
                {
                    var response = new ResultResponse();
                    response.AddErrorMessage("mediaType does not match file extension.", "chat.media.hint_mismatch");
                    throw new BadRequestException(response);
                }

                return (fromInferred.Value.category, fromInferred.Value.mime);
            }
        }

        if (hint is "image" or "document" or "audio")
        {
            var response = new ResultResponse();
            response.AddErrorMessage("Could not determine file type. Use a recognized extension or Content-Type.", "chat.media.type_unknown");
            throw new BadRequestException(response);
        }

        {
            var response = new ResultResponse();
            response.AddErrorMessage("Could not determine media type. Send Content-Type or use a recognized file extension.", "chat.media.type_unknown");
            throw new BadRequestException(response);
        }
    }

    private (string category, string mime)? TryCategoryForMime(string mime)
    {
        if (_options.AllowedImageMimeTypes.Any(m => string.Equals(m, mime, StringComparison.OrdinalIgnoreCase)))
            return ("image", mime);

        if (_options.AllowedAudioMimeTypes.Any(m => string.Equals(m, mime, StringComparison.OrdinalIgnoreCase)))
            return ("audio", mime);

        if (_options.AllowedDocumentMimeTypes.Any(m => string.Equals(m, mime, StringComparison.OrdinalIgnoreCase)))
            return ("document", mime);

        return null;
    }

    private static string? TryInferMimeFromExtension(string ext) => ext switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        ".txt" => "text/plain",
        ".csv" => "text/csv",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".ogg" => "audio/ogg",
        ".mp3" => "audio/mpeg",
        ".m4a" => "audio/mp4",
        ".wav" => "audio/wav",
        ".webm" => "audio/webm",
        _ => null
    };

    /// <summary>Same folder names as inbound processors (images, documents, audios).</summary>
    private static string InboundStyleMediaFolder(string category) =>
        category switch
        {
            "image" => "images",
            "audio" => "audios",
            _ => "documents"
        };

    private static string NormalizeMime(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return string.Empty;

        var semi = contentType.IndexOf(';', StringComparison.Ordinal);
        return semi >= 0 ? contentType[..semi].Trim().ToLowerInvariant() : contentType.Trim().ToLowerInvariant();
    }

    private static string SanitizeFileName(string? original)
    {
        var name = Path.GetFileName(string.IsNullOrWhiteSpace(original) ? "file" : original.Trim());
        name = InvalidFileNameChars().Replace(name, "_");
        if (name.Length > 200)
            name = name[..200];
        return string.IsNullOrEmpty(name) ? "file" : name;
    }

    [GeneratedRegex(@"[^\w\.\-]+")]
    private static partial Regex InvalidFileNameChars();
}
