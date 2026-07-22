using Application.Dtos;
using Infrastructure.Configuration;

namespace Infrastructure.Media.FFmpeg;

internal static class LogoResolver
{
    public static RenderRequest Resolve(RenderRequest request, LogoOptions logo)
    {
        if (!string.IsNullOrWhiteSpace(request.LogoPath))
        {
            return request;
        }

        if (!IsUsable(logo.Path))
        {
            return request;
        }

        return request with
        {
            LogoPath = logo.Path,
            LogoPosition = logo.Position,
            LogoScalePercent = logo.ScalePercent,
            LogoMargin = logo.Margin,
        };
    }

    public static bool IsUsable(string? path) =>
        !string.IsNullOrWhiteSpace(path) && (IsRemote(path) || File.Exists(path));

    private static bool IsRemote(string path) =>
        path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
