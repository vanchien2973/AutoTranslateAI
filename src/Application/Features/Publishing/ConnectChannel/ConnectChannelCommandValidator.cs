using FluentValidation;

namespace Application.Features.Publishing.ConnectChannel;

public sealed class ConnectChannelCommandValidator : AbstractValidator<ConnectChannelCommand>
{
    public ConnectChannelCommandValidator()
    {
        RuleFor(command => command.Platform).IsInEnum();
        RuleFor(command => command.Code).NotEmpty();
        RuleFor(command => command.RedirectUri)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("RedirectUri must be an absolute URL.");
    }
}
