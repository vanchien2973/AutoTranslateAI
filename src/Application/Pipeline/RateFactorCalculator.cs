namespace Application.Pipeline;

public static class RateFactorCalculator
{
    public const double MinFactor = 0.5;
    public const double MaxFactor = 2.0;

    public static double Compute(double naturalDurationSeconds, double targetDurationSeconds)
    {
        if (naturalDurationSeconds <= 0 || targetDurationSeconds <= 0)
        {
            return 1.0;
        }

        var factor = naturalDurationSeconds / targetDurationSeconds;
        return Math.Clamp(factor, MinFactor, MaxFactor);
    }
}
