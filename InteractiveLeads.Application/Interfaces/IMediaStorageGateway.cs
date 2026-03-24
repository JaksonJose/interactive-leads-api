using InteractiveLeads.Application.Feature.Inbound.Media;

namespace InteractiveLeads.Application.Interfaces;

public interface IMediaStorageGateway
{
    Task<Stream> DownloadAsync(string sourceUrlOrKey, CancellationToken cancellationToken);
    Task<MediaObjectDescriptor> UploadAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
