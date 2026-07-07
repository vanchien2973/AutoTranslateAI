using MediatR;

namespace Application.Features.Voices.GetVoices;

public sealed record GetVoicesQuery(string LanguageCode) : IRequest<GetVoicesResponse>;
