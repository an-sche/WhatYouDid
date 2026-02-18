using System.Security.Claims;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace WhatYouDid.Services;

/// <summary>
/// Circuit handler that sets the current tenant for the Blazor Server circuit
/// based on the authenticated user. This ensures the scoped ITenantService
/// has the correct tenant id during interactive components.
/// </summary>
public class TenantCircuitHandler(
    ITenantService tenantService, 
    IHttpContextAccessor httpContextAccessor) 
: CircuitHandler
{
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        try
        {
            var ctx = httpContextAccessor.HttpContext;
            var user = ctx?.User;
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
            tenantService.SetTenant(string.Empty);
        }

        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Clear tenant when connection goes down to avoid leaking tenant id into other scopes.
        tenantService.SetTenant(string.Empty);
        return Task.CompletedTask;
    }
}
