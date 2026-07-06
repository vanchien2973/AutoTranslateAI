namespace Application.Helpers;

public readonly record struct TtsTiming(double Start, double End, double RateFactor)
{
    public double Window => End - Start;
}
