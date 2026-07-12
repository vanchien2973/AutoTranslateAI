using MediatR;

namespace Application.Features.Publishing.GenerateSeoMetadata;

public sealed record GenerateSeoMetadataQuery(Guid JobId) : IRequest<GenerateSeoMetadataResponse>;
