using Application.Dtos;

namespace Infrastructure.Media.Demucs;

internal static class DemucsArguments
{
    public static IReadOnlyList<string> BuildSeparate(DemucsRequest request)
    {
        var arguments = new List<string>
        {
            "-n", request.Model,
            "-o", request.OutputDirectory,
        };

        if (request.TwoStems)
        {
            // Collapse to vocals + everything-else (faster, enough for dubbing).
            arguments.Add("--two-stems");
            arguments.Add("vocals");
        }

        arguments.Add(request.InputAudioPath);
        return arguments;
    }
}
