using Application.Enums;

namespace Infrastructure.Configuration;

public sealed class LogoOptions
{
    public const string SectionName = "Logo";
    public string? Path { get; init; }
    public LogoPosition Position { get; init; } = LogoPosition.BottomRight;
    public double ScalePercent { get; init; } = 0.1;
    public int Margin { get; init; } = 16;
}
