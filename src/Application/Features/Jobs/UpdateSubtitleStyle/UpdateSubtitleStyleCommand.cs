using Domain.Enums;
using MediatR;

namespace Application.Features.Jobs.UpdateSubtitleStyle;

public sealed record UpdateSubtitleStyleCommand(
    string? FontFamily,
    int FontSize,
    SubtitlePosition Position,
    bool Bold,
    bool Italic,
    Guid JobId = default) : IRequest<UpdateSubtitleStyleResponse>;
