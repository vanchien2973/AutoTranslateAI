using FluentValidation;

namespace Application.Features.Publishing.SetPlatformCredential;

public sealed class SetPlatformCredentialCommandValidator : AbstractValidator<SetPlatformCredentialCommand>
{
    public SetPlatformCredentialCommandValidator()
    {
        RuleFor(command => command.Platform).IsInEnum();
        RuleFor(command => command.ClientId).NotEmpty().MaximumLength(256);
        RuleFor(command => command.ClientSecret).NotEmpty().MaximumLength(512);
        RuleFor(command => command.DefaultRedirectUri)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .When(command => !string.IsNullOrEmpty(command.DefaultRedirectUri))
            .WithMessage("DefaultRedirectUri must be an absolute URL.");
    }
}
