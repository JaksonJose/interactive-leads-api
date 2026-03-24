namespace InteractiveLeads.Application.Interfaces;

public interface IMediaContentInspector
{
    Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken);
}
