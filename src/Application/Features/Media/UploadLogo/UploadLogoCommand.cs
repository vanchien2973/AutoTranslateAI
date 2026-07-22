using MediatR;

namespace Application.Features.Media.UploadLogo;

public sealed record UploadLogoCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long Length) : IRequest<UploadLogoResponse>;
