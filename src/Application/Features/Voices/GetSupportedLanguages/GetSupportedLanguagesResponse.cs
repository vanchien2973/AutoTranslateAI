namespace Application.Features.Voices.GetSupportedLanguages;

public sealed record GetSupportedLanguagesResponse(IReadOnlyCollection<string> AudioLanguages);
