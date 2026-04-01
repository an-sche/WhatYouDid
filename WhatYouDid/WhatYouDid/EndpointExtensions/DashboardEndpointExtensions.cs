using WhatYouDid.Services;

namespace WhatYouDid.EndpointExtensions;

public static class DashboardEndpointExtensions
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/dashboard");

        group.MapGet("", async (IDashboardService service, int? year = null) =>
        {
            return await service.GetDashboardForUserAsync(year);
        });

        group.MapGet("/years", async (IDashboardService service) =>
        {
            return await service.GetActiveYearsAsync();
        });

        return routes;
    }
}
