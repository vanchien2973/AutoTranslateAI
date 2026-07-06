namespace Application.Features.Review.GetReviewHistory;

public sealed record GetReviewHistoryResponse(PagedResult<ChatMessage> Messages);
