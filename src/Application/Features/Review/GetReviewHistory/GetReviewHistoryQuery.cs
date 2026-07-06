using MediatR;

namespace Application.Features.Review.GetReviewHistory;

public sealed record GetReviewHistoryQuery(Guid JobId, int Page, int PageSize) : IRequest<GetReviewHistoryResponse>;
