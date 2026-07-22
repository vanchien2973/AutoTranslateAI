using Application.Interfaces;
using MediatR;

namespace Application.Features.Media.UploadLogo;

public sealed class UploadLogoCommandHandler : IRequestHandler<UploadLogoCommand, UploadLogoResponse>
{
    private readonly IStorageService _storage;

    public UploadLogoCommandHandler(IStorageService storage) => _storage = storage;

    public async Task<UploadLogoResponse> Handle(UploadLogoCommand request, CancellationToken cancellationToken)
    {
        // Random key: two users uploading "logo.png" must not overwrite each other.
        var key = $"logos/{Guid.NewGuid():N}{ExtensionFor(request.ContentType)}";
        await _storage.UploadAsync(request.Content, key, request.ContentType, cancellationToken);

        return new UploadLogoResponse(key);
    }

    private static string ExtensionFor(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/webp" => ".webp",
        _ => ".png",
    };
}
