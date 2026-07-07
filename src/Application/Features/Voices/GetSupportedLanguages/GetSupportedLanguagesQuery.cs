using MediatR;

namespace Application.Features.Voices.GetSupportedLanguages;

public sealed record GetSupportedLanguagesQuery : IRequest<GetSupportedLanguagesResponse>;
