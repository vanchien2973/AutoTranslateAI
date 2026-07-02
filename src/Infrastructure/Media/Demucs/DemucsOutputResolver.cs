using Application.Dtos;

namespace Infrastructure.Media.Demucs;

internal static class DemucsOutputResolver
{
    public static DemucsResult Resolve(DemucsRequest request)
    {
        var track = Path.GetFileNameWithoutExtension(request.InputAudioPath);
        var stemDirectory = Path.Combine(request.OutputDirectory, request.Model, track);

        var vocals = Path.Combine(stemDirectory, "vocals.wav");
        var accompaniment = Path.Combine(stemDirectory, request.TwoStems ? "no_vocals.wav" : "other.wav");

        return new DemucsResult(vocals, accompaniment);
    }
}