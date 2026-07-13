using Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly ApiKeyOptions _auth;

    public AuthController(IOptions<ApiKeyOptions> auth) => _auth = auth.Value;

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!LoginValidator.IsValid(request.Email, request.Password, _auth.AdminEmail, _auth.AdminPassword))
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        return Ok(new { token = _auth.AdminPassword, header = _auth.HeaderName });
    }
}

public sealed record LoginRequest(string Email, string Password);
