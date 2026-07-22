using FluentValidation;

namespace Application.Features.Media.UploadLogo;

public sealed class UploadLogoCommandValidator : AbstractValidator<UploadLogoCommand>
{
    public const long MaxBytes = 2 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes = ["image/png", "image/jpeg", "image/webp"];

    public UploadLogoCommandValidator()
    {
        RuleFor(command => command.Length)
            .GreaterThan(0).WithMessage("The logo file is empty.")
            .LessThanOrEqualTo(MaxBytes).WithMessage("The logo must be 2 MB or smaller.");

        RuleFor(command => command.ContentType)
            .Must(type => AllowedContentTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("The logo must be a PNG, JPEG, or WebP image.");
    }
}
