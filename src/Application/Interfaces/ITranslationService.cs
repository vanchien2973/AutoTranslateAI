namespace Application.Interfaces;

public interface ITranslationService
{
    Task<IReadOnlyList<string>> TranslateBatchAsync(
        IReadOnlyList<string> texts,
        string sourceLang,
        string targetLang,
        CancellationToken cancellationToken);
}
