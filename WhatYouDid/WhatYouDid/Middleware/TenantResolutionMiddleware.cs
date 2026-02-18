using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WhatYouDid.Services;

namespace WhatYouDid.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        try
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                tenantService.SetTenant(userId);
            }
            else
            {
                tenantService.SetTenant(string.Empty);
            }
        }
        catch
        {
            // Swallow errors to avoid blocking requests. Tenant will be empty.
            tenantService.SetTenant(string.Empty);
        }

        await _next(context);
    }
}
