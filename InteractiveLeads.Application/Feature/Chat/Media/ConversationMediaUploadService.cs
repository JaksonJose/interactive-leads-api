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
    IOutboundAudioTranscoder outboundAudioTranscoder,
    IMediaContentInspector contentInspector,
    IOptions<OutboundMediaUploadOptions> options,
    ILogger<ConversationMediaUploadService> logger) : IConversationMediaUploadService
{
    private static readonly string[] OutboundAudioTranscodeSourceMimeTypes =
    [
        "audio/webm",
        "audio/wav",
        "audio/x-wav",
        "audio/mp4",
        "audio/x-m4a",
        "audio/m4a"
    ];

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
            "video" => _options.MaxVideoBytes,
            _ => _options.MaxDocumentBytes
        };

        // Images are validated after normalization (JPEG/PNG); encoded output can differ in size from the upload.
        if (category != "image" && request.ContentLength > maxBytes)
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

        if (category == "audio")
        {
            return await UploadAudioForWhatsappAsync(
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
    /// WhatsApp delivery: Ogg Opus. CRM/DB: M4A (AAC) as optimized when transcoding.
    /// <see cref="ConversationMediaUploadResultDto.Url"/> is the file sent to the provider; <see cref="ConversationMediaUploadResultDto.OptimizedUrl"/> is persisted in the DB.
    /// </summary>
    private async Task<ConversationMediaUploadResultDto> UploadAudioForWhatsappAsync(
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

        if (buffer.Length > _options.MaxAudioBytes)
        {
            var response = new ResultResponse();
            response.AddErrorMessage("File exceeds the maximum allowed size for this media type.", "chat.media.file_too_large");
            throw new BadRequestException(response);
        }

        var unique = Guid.NewGuid().ToString("N");
        var folder = InboundStyleMediaFolder("audio");

        if (RequiresWhatsappAudioTranscode(effectiveMime))
        {
            buffer.Position = 0;
            MemoryStream oggStream;
            MemoryStream m4aStream;
            try
            {
                oggStream = await outboundAudioTranscoder.TranscodeToOggOpusAsync(buffer, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbound audio transcoding (Ogg) failed for {Mime}", effectiveMime);
                var response = new ResultResponse();
                response.AddErrorMessage(
                    "Could not convert audio for WhatsApp. Install FFmpeg on the server or use Ogg/MP3.",
                    "chat.media.transcode_failed");
                throw new BadRequestException(response);
            }

            buffer.Position = 0;
            try
            {
                m4aStream = await outboundAudioTranscoder.TranscodeToM4aAacAsync(buffer, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await oggStream.DisposeAsync();
                logger.LogError(ex, "Outbound audio transcoding (M4A) failed for {Mime}", effectiveMime);
                var response = new ResultResponse();
                response.AddErrorMessage(
                    "Could not produce optimized audio (M4A). Install FFmpeg on the server.",
                    "chat.media.transcode_failed");
                throw new BadRequestException(response);
            }

            await using (oggStream)
            await using (m4aStream)
            {
                if (oggStream.Length > _options.MaxAudioBytes || m4aStream.Length > _options.MaxAudioBytes)
                {
                    var response = new ResultResponse();
                    response.AddErrorMessage("Converted audio exceeds the maximum allowed size.", "chat.media.file_too_large");
                    throw new BadRequestException(response);
                }

                buffer.Position = 0;
                var rawKey = $"{root}/{tenantSegment}/{folder}/{unique}_{safeName}";
                var rawDescriptor = await storageGateway.UploadAsync(rawKey, buffer, effectiveMime, cancellationToken);

                var deliveryFileName = Path.ChangeExtension(safeName, ".ogg");
                if (string.IsNullOrEmpty(Path.GetExtension(deliveryFileName)))
                    deliveryFileName = $"{Path.GetFileNameWithoutExtension(safeName)}.ogg";

                var optimizedFileName = Path.ChangeExtension(safeName, ".m4a");
                if (string.IsNullOrEmpty(Path.GetExtension(optimizedFileName)))
                    optimizedFileName = $"{Path.GetFileNameWithoutExtension(safeName)}.m4a";

                m4aStream.Position = 0;
                var optimizedKey = $"{root}/{tenantSegment}/{folder}/{unique}_{optimizedFileName}";
                var optimizedDescriptor = await storageGateway.UploadAsync(optimizedKey, m4aStream, "audio/mp4", cancellationToken);

                oggStream.Position = 0;
                var deliveryKey = $"{root}/{tenantSegment}/{folder}/{unique}_{deliveryFileName}";
                var descriptor = await storageGateway.UploadAsync(deliveryKey, oggStream, "audio/ogg", cancellationToken);

                return new ConversationMediaUploadResultDto
                {
                    Url = descriptor.PublicUrl,
                    ObjectKey = descriptor.ObjectKey,
                    FileName = deliveryFileName,
                    MimeType = "audio/ogg",
                    SizeBytes = oggStream.Length,
                    MediaType = "audio",
                    OriginalUrl = rawDescriptor.PublicUrl,
                    OptimizedUrl = optimizedDescriptor.PublicUrl,
                    OptimizedMimeType = "audio/mp4",
                    OptimizedFileName = optimizedFileName
                };
            }
        }

        buffer.Position = 0;
        var deliveryKeyDirect = $"{root}/{tenantSegment}/{folder}/{unique}_{safeName}";
        var directDescriptor = await storageGateway.UploadAsync(deliveryKeyDirect, buffer, effectiveMime, cancellationToken);

        return new ConversationMediaUploadResultDto
        {
            Url = directDescriptor.PublicUrl,
            ObjectKey = directDescriptor.ObjectKey,
            FileName = safeName,
            MimeType = effectiveMime,
            SizeBytes = buffer.Length,
            MediaType = "audio",
            OptimizedUrl = directDescriptor.PublicUrl,
            OptimizedMimeType = null,
            OptimizedFileName = null
        };
    }

    private static bool RequiresWhatsappAudioTranscode(string mime) =>
        OutboundAudioTranscodeSourceMimeTypes.Any(m => string.Equals(m, mime, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Normalizes to JPEG or PNG for WhatsApp, stores that in S3 (<see cref="ConversationMediaUploadResultDto.Url"/>).
    /// Runs <see cref="IImageProcessor"/> afterwards for CRM WebP variants (optional; failures do not fail upload).
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

        buffer.Position = 0;
        OutboundWhatsAppImageEncodingResult encoded;
        try
        {
            encoded = await imageProcessor.EncodeForWhatsAppDeliveryAsync(buffer, effectiveMime, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Outbound image encoding failed for {Mime}", effectiveMime);
            var response = new ResultResponse();
            response.AddErrorMessage(
                "Could not process this image. Use JPEG, PNG, or WebP.",
                "chat.media.image_encode_failed");
            throw new BadRequestException(response);
        }

        await using (encoded)
        {
            if (encoded.Stream.Length > _options.MaxImageBytes)
            {
                var response = new ResultResponse();
                response.AddErrorMessage(
                    "File exceeds the maximum allowed size for this media type.",
                    "chat.media.file_too_large");
                throw new BadRequestException(response);
            }

            var deliveryFileName = Path.ChangeExtension(safeName, encoded.FileExtension.TrimStart('.'));
            var unique = Guid.NewGuid().ToString("N");
            var rawKey = $"{root}/{tenantSegment}/{InboundStyleMediaFolder("image")}/{unique}_{deliveryFileName}";
            encoded.Stream.Position = 0;
            var rawDescriptor = await storageGateway.UploadAsync(rawKey, encoded.Stream, encoded.ContentType, cancellationToken);

            string? optimizedUrl = null;
            string? thumbnailUrl = null;

            encoded.Stream.Position = 0;
            string hash;
            try
            {
                hash = await contentInspector.ComputeSha256Async(encoded.Stream, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to hash outbound image for CRM variants");
                return new ConversationMediaUploadResultDto
                {
                    Url = rawDescriptor.PublicUrl,
                    ObjectKey = rawDescriptor.ObjectKey,
                    FileName = deliveryFileName,
                    MimeType = encoded.ContentType,
                    SizeBytes = encoded.Stream.Length,
                    MediaType = "image",
                    OriginalUrl = rawDescriptor.PublicUrl
                };
            }

            encoded.Stream.Position = 0;
            try
            {
                var result = await imageProcessor.ProcessAsync(
                    encoded.Stream,
                    new ProcessMediaRequest
                    {
                        TenantId = tenantSegment,
                        MediaUrl = string.Empty,
                        MediaType = "image",
                        MimeType = encoded.ContentType,
                        OriginalFileName = deliveryFileName
                    },
                    hash,
                    cancellationToken);
                optimizedUrl = result.OptimizedUrl;
                thumbnailUrl = result.ThumbnailUrl;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "CRM image variants skipped; outbound image was stored successfully");
            }

            return new ConversationMediaUploadResultDto
            {
                Url = rawDescriptor.PublicUrl,
                ObjectKey = rawDescriptor.ObjectKey,
                FileName = deliveryFileName,
                MimeType = encoded.ContentType,
                SizeBytes = encoded.Stream.Length,
                MediaType = "image",
                OriginalUrl = rawDescriptor.PublicUrl,
                OptimizedUrl = optimizedUrl,
                ThumbnailUrl = thumbnailUrl
            };
        }
    }

    private void ValidateAgainstAllowList(string category, string mime)
    {
        var allowed = category switch
        {
            "image" => _options.AllowedImageMimeTypes,
            "document" => _options.AllowedDocumentMimeTypes,
            "audio" => _options.AllowedAudioMimeTypes.Concat(OutboundAudioTranscodeSourceMimeTypes),
            "video" => _options.AllowedVideoMimeTypes,
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

        if (hint is "image" or "document" or "audio" or "video")
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

        if (OutboundAudioTranscodeSourceMimeTypes.Any(m => string.Equals(m, mime, StringComparison.OrdinalIgnoreCase)))
            return ("audio", mime);

        if (_options.AllowedVideoMimeTypes.Any(m => string.Equals(m, mime, StringComparison.OrdinalIgnoreCase)))
            return ("video", mime);

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
        ".aac" => "audio/aac",
        ".amr" => "audio/amr",
        ".wav" => "audio/wav",
        ".webm" => "audio/webm",
        ".mp4" or ".m4v" => "video/mp4",
        ".3gp" or ".3gpp" => "video/3gpp",
        _ => null
    };

    /// <summary>Same folder names as inbound processors (images, documents, audios).</summary>
    private static string InboundStyleMediaFolder(string category) =>
        category switch
        {
            "image" => "images",
            "audio" => "audios",
            "video" => "videos",
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
