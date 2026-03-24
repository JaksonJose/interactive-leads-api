namespace InteractiveLeads.Application.Interfaces;

/// <summary>
/// Outbound audio: Ogg Opus for WhatsApp delivery; M4A (AAC) for CRM / DB as the optimized asset.
/// </summary>
public interface IOutboundAudioTranscoder
{
    Task<MemoryStream> TranscodeToOggOpusAsync(Stream source, CancellationToken cancellationToken);
    Task<MemoryStream> TranscodeToM4aAacAsync(Stream source, CancellationToken cancellationToken);
}
