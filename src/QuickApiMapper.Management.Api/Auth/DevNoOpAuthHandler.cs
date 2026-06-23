using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace QuickApiMapper.Management.Api.Auth;

/// <summary>
/// Development-only authentication handler that authenticates every request as a local
/// admin principal. This handler MUST NOT be used in production; configure a real
/// identity provider via "Auth:Authority" in appsettings before exposing the Management
/// API to untrusted callers.
/// </summary>
internal sealed class DevNoOpAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevNoOpAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "dev-local"),
            new Claim(ClaimTypes.Name, "Developer"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
