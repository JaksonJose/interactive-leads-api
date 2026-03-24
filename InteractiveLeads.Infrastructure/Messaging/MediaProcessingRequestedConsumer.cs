using System.Diagnostics;
using System.Text.Json;
using InteractiveLeads.Application.Feature.Chat.Messages;
using InteractiveLeads.Application.Feature.Inbound.Media;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Application.Messaging.Contracts;
using InteractiveLeads.Application.Realtime.Models;
using InteractiveLeads.Application.Realtime.Services;
using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InteractiveLeads.Infrastructure.Messaging;

public sealed class MediaProcessingRequestedConsumer(
    IServiceProvider serviceProvider,
    ILogger<MediaProcessingRequestedConsumer> logger) : IConsumer<MediaProcessingRequested>
{
    public async Task Consume(ConsumeContext<MediaProcessingRequested> context)
    {
        var msg = context.Message;
        var sw = Stopwatch.StartNew();
        logger.LogInformation(
            "Media processing job started. messageId {MessageId} conversationId {ConversationId} type {MediaType} tempUrlHost {TempUrlHost}",
            msg.MessageId,
            msg.ConversationId,
            msg.MediaType,
            Uri.TryCreate(msg.TempUrl, UriKind.Absolute, out var tempUri) ? tempUri.Host : "(relative)");

        await using var scope = serviceProvider.CreateAsyncScope();
        var crossTenantService = scope.ServiceProvider.GetRequiredService<ICrossTenantService>();

        await crossTenantService.ExecuteInTenantContextForSystemAsync(msg.TenantId, async sp =>
        {
            var db = sp.GetRequiredService<IApplicationDbContext>();
            var mediaProcessor = sp.GetRequiredService<IMediaProcessor>();
            var realtimeService = sp.GetRequiredService<IRealtimeService>();

            var message = await db.Messages
                .Include(m => m.Media)
                .FirstOrDefaultAsync(m => m.Id == msg.MessageId, context.CancellationToken);

            if (message is null)
            {
                logger.LogWarning(
                    "Media processing skipped: message {MessageId} not found (elapsed {ElapsedMs} ms).",
                    msg.MessageId,
                    sw.ElapsedMilliseconds);
                return;
            }

            try
            {
                var mediaResult = await mediaProcessor.ProcessAsync(new ProcessMediaRequest
                {
                    TenantId = msg.TenantId,
                    MediaUrl = msg.TempUrl,
                    MediaType = msg.MediaType,
                    MimeType = msg.MimeType,
                    OriginalFileName = msg.OriginalFileName,
                    ExternalMessageId = msg.ExternalMessageId
                }, context.CancellationToken);

                var media = message.Media.OrderBy(m => m.Id).FirstOrDefault();
                if (media is null)
                {
                    media = new MessageMedia
                    {
                        Id = Guid.NewGuid(),
                        MessageId = message.Id,
                        MediaType = ParseMediaType(msg.MediaType),
                        Url = mediaResult.OptimizedUrl ?? mediaResult.OriginalUrl,
                        MimeType = msg.MimeType ?? string.Empty,
                        FileSize = 0,
                        FileName = string.IsNullOrWhiteSpace(msg.OriginalFileName) ? null : msg.OriginalFileName.Trim(),
                        Caption = msg.Caption
                    };
                    db.MessageMedia.Add(media);
                }
                else
                {
                    media.Url = mediaResult.OptimizedUrl ?? mediaResult.OriginalUrl;
                    media.Caption = msg.Caption;
                    media.MimeType = msg.MimeType ?? media.MimeType;
                    if (!string.IsNullOrWhiteSpace(msg.OriginalFileName))
                        media.FileName = msg.OriginalFileName.Trim();
                }

                message.Metadata = JsonSerializer.Serialize(new
                {
                    mediaProcessingStatus = "completed",
                    mediaProcessing = mediaResult
                });

                await db.SaveChangesAsync(context.CancellationToken);

                var evt = new RealtimeEvent<MessageUpdatedPayloadDto>
                {
                    Type = "message.updated",
                    TenantId = msg.TenantId,
                    Timestamp = DateTime.UtcNow,
                    Payload = new MessageUpdatedPayloadDto
                    {
                        Id = msg.MessageId,
                        ConversationId = msg.ConversationId,
                        MediaProcessingStatus = "completed",
                        Media = new MessageMediaListItemDto
                        {
                            Url = mediaResult.OptimizedUrl ?? mediaResult.OriginalUrl,
                            OptimizedUrl = mediaResult.OptimizedUrl,
                            ThumbnailUrl = mediaResult.ThumbnailUrl,
                            MimeType = msg.MimeType ?? string.Empty,
                            FileName = media.FileName,
                            Caption = msg.Caption
                        }
                    }
                };

                await realtimeService.SendToConversationAsync(msg.ConversationId.ToString("D"), evt);

                logger.LogInformation(
                    "Media processing completed. messageId {MessageId} elapsedMs {ElapsedMs}",
                    msg.MessageId,
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Media worker failed for message {MessageId} after {ElapsedMs} ms",
                    msg.MessageId,
                    sw.ElapsedMilliseconds);
                message.Metadata = JsonSerializer.Serialize(new
                {
                    mediaProcessingStatus = "failed",
                    error = ex.Message
                });
                await db.SaveChangesAsync(context.CancellationToken);

                var evt = new RealtimeEvent<MessageUpdatedPayloadDto>
                {
                    Type = "message.updated",
                    TenantId = msg.TenantId,
                    Timestamp = DateTime.UtcNow,
                    Payload = new MessageUpdatedPayloadDto
                    {
                        Id = msg.MessageId,
                        ConversationId = msg.ConversationId,
                        MediaProcessingStatus = "failed",
                        Media = null
                    }
                };

                await realtimeService.SendToConversationAsync(msg.ConversationId.ToString("D"), evt);
                throw;
            }
        });
    }

    private static MediaType ParseMediaType(string value) => value.Trim().ToLowerInvariant() switch
    {
        "image" => MediaType.Image,
        "video" => MediaType.Video,
        "audio" => MediaType.Audio,
        "document" => MediaType.Document,
        "sticker" => MediaType.Sticker,
        _ => MediaType.Document
    };
}
