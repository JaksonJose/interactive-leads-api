using System.Diagnostics;
using InteractiveLeads.Application.Feature.Chat.Media;
using InteractiveLeads.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace InteractiveLeads.Infrastructure.Media;

public sealed class FfmpegOutboundAudioTranscoder(IOptions<OutboundMediaUploadOptions> options) : IOutboundAudioTranscoder
{
    private readonly OutboundMediaUploadOptions _options = options.Value;

    public async Task<MemoryStream> TranscodeToOggOpusAsync(Stream source, CancellationToken cancellationToken)
    {
        var exe = (_options.FfmpegExecutable ?? "ffmpeg").Trim();
        if (string.IsNullOrEmpty(exe))
            throw new InvalidOperationException("FFmpeg executable path is not configured.");

        var tempDir = Path.GetTempPath();
        var id = Guid.NewGuid().ToString("N");
        var tempIn = Path.Combine(tempDir, $"{id}_in");
        var tempOut = Path.Combine(tempDir, $"{id}_out.ogg");

        try
        {
            await using (var fs = new FileStream(tempIn, FileMode.Create, FileAccess.Write, FileShare.None, 65536,
                           FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(fs, cancellationToken);
            }

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"-nostdin -hide_banner -loglevel error -y -i \"{tempIn}\" -c:a libopus -b:a 64k -ac 1 -vn \"{tempOut}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc is null)
                throw new InvalidOperationException("Could not start FFmpeg process.");

            await proc.WaitForExitAsync(cancellationToken);
            var err = await proc.StandardError.ReadToEndAsync(cancellationToken);
            if (proc.ExitCode != 0)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(err) ? $"FFmpeg exited with code {proc.ExitCode}." : err.Trim());

            await using var outFs = new FileStream(tempOut, FileMode.Open, FileAccess.Read, FileShare.Read, 65536,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var ms = new MemoryStream();
            await outFs.CopyToAsync(ms, cancellationToken);
            if (ms.Length == 0)
                throw new InvalidOperationException("FFmpeg produced an empty output file.");

            ms.Position = 0;
            return ms;
        }
        finally
        {
            TryDelete(tempIn);
            TryDelete(tempOut);
        }
    }

    public async Task<MemoryStream> TranscodeToM4aAacAsync(Stream source, CancellationToken cancellationToken)
    {
        var exe = (_options.FfmpegExecutable ?? "ffmpeg").Trim();
        if (string.IsNullOrEmpty(exe))
            throw new InvalidOperationException("FFmpeg executable path is not configured.");

        var tempDir = Path.GetTempPath();
        var id = Guid.NewGuid().ToString("N");
        var tempIn = Path.Combine(tempDir, $"{id}_in");
        var tempOut = Path.Combine(tempDir, $"{id}_out.m4a");

        try
        {
            await using (var fs = new FileStream(tempIn, FileMode.Create, FileAccess.Write, FileShare.None, 65536,
                           FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(fs, cancellationToken);
            }

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"-nostdin -hide_banner -loglevel error -y -i \"{tempIn}\" -c:a aac -b:a 128k -ac 1 -vn -movflags +faststart \"{tempOut}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc is null)
                throw new InvalidOperationException("Could not start FFmpeg process.");

            await proc.WaitForExitAsync(cancellationToken);
            var err = await proc.StandardError.ReadToEndAsync(cancellationToken);
            if (proc.ExitCode != 0)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(err) ? $"FFmpeg exited with code {proc.ExitCode}." : err.Trim());

            await using var outFs = new FileStream(tempOut, FileMode.Open, FileAccess.Read, FileShare.Read, 65536,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var ms = new MemoryStream();
            await outFs.CopyToAsync(ms, cancellationToken);
            if (ms.Length == 0)
                throw new InvalidOperationException("FFmpeg produced an empty output file.");

            ms.Position = 0;
            return ms;
        }
        finally
        {
            TryDelete(tempIn);
            TryDelete(tempOut);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            /* best-effort */
        }
    }
}
