namespace Application.Tests.Auth;

public class ApiKeyValidatorTests
{
    private static readonly string[] Keys = ["secret-1", "secret-2"];

    [Theory]
    [InlineData("secret-1", true)]
    [InlineData("secret-2", true)]
    [InlineData("wrong", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Given_Key_When_IsAuthorized_Then_MatchesConfigured(string? provided, bool expected)
    {
        ApiKeyValidator.IsAuthorized(provided, Keys).Should().Be(expected);
    }

    [Fact]
    public void Given_BlankConfiguredEntries_When_IsAuthorized_Then_IgnoresThem()
    {
        ApiKeyValidator.IsAuthorized("", ["", "  "]).Should().BeFalse();
        ApiKeyValidator.IsAuthorized("real", ["", "real"]).Should().BeTrue();
    }
}

public class LoginValidatorTests
{
    private const string Email = "admin@gmail.com";
    private const string Password = "Admin@123";

    [Fact]
    public void Given_CorrectCredentials_When_IsValid_Then_True()
    {
        LoginValidator.IsValid(Email, Password, Email, Password).Should().BeTrue();
    }

    [Fact]
    public void Given_EmailDifferentCasingAndSpaces_When_IsValid_Then_True()
    {
        LoginValidator.IsValid("  Admin@Gmail.com ", Password, Email, Password).Should().BeTrue();
    }

    [Theory]
    [InlineData("admin@gmail.com", "wrong")]
    [InlineData("someone@gmail.com", "Admin@123")]
    [InlineData("", "Admin@123")]
    [InlineData("admin@gmail.com", "")]
    public void Given_WrongCredentials_When_IsValid_Then_False(string email, string password)
    {
        LoginValidator.IsValid(email, password, Email, Password).Should().BeFalse();
    }

    [Fact]
    public void Given_PasswordDifferentCasing_When_IsValid_Then_False()
    {
        LoginValidator.IsValid(Email, "admin@123", Email, Password).Should().BeFalse();
    }
}
