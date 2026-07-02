using CliWrap;
using CliWrap.Buffered;

namespace Infrastructure.Media.FFmpeg;

internal static class FfmpegRunner
{
    public static async Task RunAsync(
        string ffmpegPath,
        IReadOnlyList<string> arguments,
        string expectedOutputPath,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(expectedOutputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var result = await Cli.Wrap(ffmpegPath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"ffmpeg failed (exit {result.ExitCode}): {result.StandardError}");
        }

        if (!File.Exists(expectedOutputPath))
        {
            throw new InvalidOperationException(
                $"ffmpeg reported success but '{expectedOutputPath}' was not produced.");
        }
    }
}
