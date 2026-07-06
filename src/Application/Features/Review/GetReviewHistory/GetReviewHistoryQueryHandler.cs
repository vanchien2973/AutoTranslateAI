using Application.Interfaces;
using MediatR;

namespace Application.Features.Review.GetReviewHistory;

public sealed class GetReviewHistoryQueryHandler : IRequestHandler<GetReviewHistoryQuery, GetReviewHistoryResponse>
{
    private readonly IReviewSessionStore _sessions;

    public GetReviewHistoryQueryHandler(IReviewSessionStore sessions) => _sessions = sessions;

    public Task<GetReviewHistoryResponse> Handle(GetReviewHistoryQuery request, CancellationToken cancellationToken)
    {
        var (page, skip, take) = Pagination.Normalize(request.Page, request.PageSize);
        var all = _sessions.GetHistory(request.JobId);
        var items = all.Skip(skip).Take(take).ToList();

        return Task.FromResult(new GetReviewHistoryResponse(new PagedResult<ChatMessage>(items, page, take, all.Count)));
    }
}
