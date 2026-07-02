using Application.Dtos;
using Application.Interfaces;
using CliWrap;
using CliWrap.Buffered;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Media.Demucs;

public sealed class DemucsService : IDemucsService
{
    private readonly MediaToolsOptions _tools;
    private readonly ILogger<DemucsService> _logger;

    public DemucsService(IOptions<MediaToolsOptions> tools, ILogger<DemucsService> logger)
    {
        _tools = tools.Value;
        _logger = logger;
    }

    public async Task<DemucsResult> SeparateAsync(DemucsRequest request, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        var arguments = DemucsArguments.BuildSeparate(request);
        _logger.LogInformation("Separating {Input} with demucs model {Model}", request.InputAudioPath, request.Model);

        var result = await Cli.Wrap(_tools.DemucsPath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"demucs failed (exit {result.ExitCode}) for '{request.InputAudioPath}': {result.StandardError}");
        }

        var stems = DemucsOutputResolver.Resolve(request);
        if (!File.Exists(stems.VocalsPath))
        {
            throw new InvalidOperationException(
                $"demucs reported success but '{stems.VocalsPath}' was not produced.");
        }

        return stems;
    }
}
