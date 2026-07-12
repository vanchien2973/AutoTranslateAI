namespace Application.Features.Publishing.GetPublishResults;

public sealed record GetPublishResultsResponse(IReadOnlyList<PublishResultDto> Results);
