using System.Text.Json.Serialization;
using MediatR;

namespace Application.Features.Review.ReviewChat;

public sealed record ReviewChatCommand(
    [property: JsonIgnore] Guid JobId,
    string UserMessage) : IRequest<ReviewChatResponse>;
