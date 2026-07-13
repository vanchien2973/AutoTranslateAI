namespace Api.Auth;

public sealed class ApiKeyOptions
{
    public const string SectionName = "Auth";
    public bool Enabled { get; init; } = true;
    public string HeaderName { get; init; } = "X-Api-Key";
    public string AdminEmail { get; init; } = "admin@gmail.com";
    public string AdminPassword { get; init; } = "Admin@123";
    public string[] ApiKeys { get; init; } = [];

    public IReadOnlyList<string> ValidTokens() =>
        string.IsNullOrWhiteSpace(AdminPassword) ? ApiKeys : [AdminPassword, .. ApiKeys];
}
