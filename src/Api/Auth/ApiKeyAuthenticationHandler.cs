using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Api.Auth;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";

    private readonly ApiKeyOptions _apiKey;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiKeyOptions> apiKey)
        : base(options, logger, encoder) => _apiKey = apiKey.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var provided = ExtractKey();
        if (string.IsNullOrEmpty(provided))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!ApiKeyValidator.IsAuthorized(provided, _apiKey.ValidTokens()))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "api-key")], Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private string ExtractKey()
    {
        if (Request.Headers.TryGetValue(_apiKey.HeaderName, out var apiKeyHeader) &&
            !string.IsNullOrEmpty(apiKeyHeader))
        {
            return apiKeyHeader.ToString();
        }

        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization["Bearer ".Length..].Trim();
        }

        return Request.Query["access_token"].ToString();
    }
}
