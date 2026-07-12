using Application.Interfaces;
using MediatR;

namespace Application.Features.Publishing.GetPublishResults;

public sealed class GetPublishResultsQueryHandler
    : IRequestHandler<GetPublishResultsQuery, GetPublishResultsResponse>
{
    private readonly IPublishResultRepository _results;

    public GetPublishResultsQueryHandler(IPublishResultRepository results) => _results = results;

    public async Task<GetPublishResultsResponse> Handle(
        GetPublishResultsQuery request,
        CancellationToken cancellationToken)
    {
        var results = await _results.ListByJobAsync(request.JobId, cancellationToken);
        return new GetPublishResultsResponse(results.Select(result => result.ToDto()).ToList());
    }
}
