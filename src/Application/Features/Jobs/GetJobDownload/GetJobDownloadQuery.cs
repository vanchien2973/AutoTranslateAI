using MediatR;

namespace Application.Features.Jobs.GetJobDownload;

public sealed record GetJobDownloadQuery(Guid JobId) : IRequest<GetJobDownloadResponse>;
