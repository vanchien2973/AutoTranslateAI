using Application.Interfaces;
using MediatR;

namespace Application.Features.Usage.GetUsageSummary;

public sealed class GetUsageSummaryQueryHandler : IRequestHandler<GetUsageSummaryQuery, GetUsageSummaryResponse>
{
    private const int MaxDays = 365;

    private readonly IUsageRepository _usage;

    public GetUsageSummaryQueryHandler(IUsageRepository usage) => _usage = usage;

    public async Task<GetUsageSummaryResponse> Handle(GetUsageSummaryQuery request, CancellationToken cancellationToken)
    {
        var days = Math.Clamp(request.Days, 1, MaxDays);
        var since = DateTimeOffset.UtcNow.AddDays(-days);
        var records = await _usage.ListSinceAsync(since, cancellationToken);
        return new GetUsageSummaryResponse(days, UsageSummarizer.Summarize(records));
    }
}
