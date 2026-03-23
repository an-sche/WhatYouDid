using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace WhatYouDid.Tests.Infrastructure;

/// <summary>
/// Replaces the real ASP.NET Identity auth in the test host.
/// Requests that include an X-Test-UserId header are authenticated as that user.
/// Requests without the header are anonymous and will receive 401 from protected endpoints.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-UserId";
    public const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = userIdValues.FirstOrDefault();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        if (Request.Headers.TryGetValue(RolesHeader, out var rolesValues))
        {
            foreach (var role in rolesValues.FirstOrDefault()?.Split(',') ?? [])
                if (!string.IsNullOrWhiteSpace(role))
                    claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
