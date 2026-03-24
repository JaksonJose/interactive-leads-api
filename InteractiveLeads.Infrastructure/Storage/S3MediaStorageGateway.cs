using Amazon.S3;
using Amazon.S3.Model;
using InteractiveLeads.Application.Feature.Inbound.Media;
using InteractiveLeads.Application.Interfaces;
using InteractiveLeads.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Storage;

public sealed class S3MediaStorageGateway(
    IAmazonS3 s3Client,
    IOptions<MediaProcessingOptions> options,
    ILogger<S3MediaStorageGateway> logger) : IMediaStorageGateway
{
    private readonly MediaProcessingOptions _options = options.Value;

    public async Task<Stream> DownloadAsync(string sourceUrlOrKey, CancellationToken cancellationToken)
    {
        var key = ResolveKey(sourceUrlOrKey);
        var response = await s3Client.GetObjectAsync(_options.BucketName, key, cancellationToken);
        var memory = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;
        return memory;
    }

    public async Task<MediaObjectDescriptor> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (content.CanSeek)
            content.Position = 0;

        await RetryAsync(async ct =>
        {
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = Normalize(objectKey),
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false
            };
            await s3Client.PutObjectAsync(request, ct);
        }, cancellationToken);

        return new MediaObjectDescriptor
        {
            ObjectKey = Normalize(objectKey),
            PublicUrl = BuildPublicUrl(Normalize(objectKey)),
            ContentType = contentType
        };
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = Normalize(objectKey)
            };
            await s3Client.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        await RetryAsync(async ct =>
        {
            await s3Client.DeleteObjectAsync(_options.BucketName, Normalize(objectKey), ct);
        }, cancellationToken);
    }

    private string ResolveKey(string sourceUrlOrKey)
    {
        var trimmed = sourceUrlOrKey.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return Normalize(trimmed);

        return Normalize(Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')));
    }

    private string BuildPublicUrl(string key)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return $"https://{_options.BucketName}.s3.amazonaws.com/{key}";

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";
    }

    private static string Normalize(string key) => key.Trim().TrimStart('/');

    private async Task RetryAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var delays = new[] { 250, 500, 1000 };
        Exception? lastException = null;

        for (var i = 0; i < delays.Length; i++)
        {
            try
            {
                await action(cancellationToken);
                return;
            }
            catch (Exception ex) when (i < delays.Length - 1)
            {
                lastException = ex;
                logger.LogWarning(ex, "S3 operation failed. Retrying attempt {Attempt}", i + 2);
                await Task.Delay(delays[i], cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("S3 retry failed.");
    }
}
