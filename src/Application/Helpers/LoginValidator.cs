namespace Application.Helpers;

public static class LoginValidator
{
    public static bool IsValid(string? email, string? password, string adminEmail, string adminPassword) =>
        !string.IsNullOrWhiteSpace(email)
        && !string.IsNullOrEmpty(password)
        && string.Equals(email.Trim(), adminEmail.Trim(), StringComparison.OrdinalIgnoreCase)
        && string.Equals(password, adminPassword, StringComparison.Ordinal);
}
