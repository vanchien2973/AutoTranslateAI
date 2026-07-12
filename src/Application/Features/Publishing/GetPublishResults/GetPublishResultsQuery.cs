using MediatR;

namespace Application.Features.Publishing.GetPublishResults;

public sealed record GetPublishResultsQuery(Guid JobId) : IRequest<GetPublishResultsResponse>;
