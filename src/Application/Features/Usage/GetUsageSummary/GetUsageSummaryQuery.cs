using MediatR;

namespace Application.Features.Usage.GetUsageSummary;

public sealed record GetUsageSummaryQuery(int Days = 30) : IRequest<GetUsageSummaryResponse>;
