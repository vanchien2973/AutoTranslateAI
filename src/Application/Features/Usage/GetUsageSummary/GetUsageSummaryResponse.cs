namespace Application.Features.Usage.GetUsageSummary;

public sealed record GetUsageSummaryResponse(int Days, UsageSummary Summary);
