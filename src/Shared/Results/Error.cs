namespace Shared.Results;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string message) => new("validation", message);
    public static Error NotFound(string message) => new("not_found", message);
    public static Error Conflict(string message) => new("conflict", message);
    public static Error Unauthorized(string message) => new("unauthorized", message);
    public static Error Failure(string message) => new("failure", message);

    public override string ToString() =>
        string.IsNullOrEmpty(Code) ? Message : $"{Code}: {Message}";
}
