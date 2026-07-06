namespace Application.Helpers;

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

    public static TtsTiming Fit(double start, double end, double naturalDurationSeconds, double nextStart)
    {
        var borrowedEnd = end;
        var window = end - start;

        // Only borrow when the natural speech overflows the slot (the "too fast" case).
        if (naturalDurationSeconds > window)
        {
            var needed = naturalDurationSeconds - window;
            var gap = Math.Max(0.0, nextStart - end);
            borrowedEnd = end + Math.Min(needed, gap);
        }

        return new TtsTiming(start, borrowedEnd, Compute(naturalDurationSeconds, borrowedEnd - start));
    }
}
