using InteractiveLeads.Application.Feature.Chat.Media;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Configuration;

/// <summary>
/// Sets <see cref="OutboundMediaUploadOptions.StorageRootPrefix"/> from <see cref="MediaProcessingOptions.FinalPrefix"/>
/// so outbound files live under the same <c>whatsapp/{tenantId}/...</c> tree as inbound processing.
/// </summary>
internal sealed class OutboundMediaUploadStorageRootConfigure(IOptions<MediaProcessingOptions> mediaProcessing)
    : IConfigureOptions<OutboundMediaUploadOptions>
{
    public void Configure(OutboundMediaUploadOptions options)
    {
        var prefix = mediaProcessing.Value.FinalPrefix;
        if (!string.IsNullOrWhiteSpace(prefix))
            options.StorageRootPrefix = prefix.Trim();
    }
}
