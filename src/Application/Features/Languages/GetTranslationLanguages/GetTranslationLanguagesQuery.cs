using MediatR;

namespace Application.Features.Languages.GetTranslationLanguages;

public sealed record GetTranslationLanguagesQuery : IRequest<GetTranslationLanguagesResponse>;
