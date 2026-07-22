namespace Application.Features.Languages.GetTranslationLanguages;

public sealed record GetTranslationLanguagesResponse(IReadOnlyCollection<string> SubtitleLanguages);
