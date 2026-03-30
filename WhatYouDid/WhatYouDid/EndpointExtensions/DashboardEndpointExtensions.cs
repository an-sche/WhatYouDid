using WhatYouDid.Services;

namespace WhatYouDid.EndpointExtensions;

public static class DashboardEndpointExtensions
{
    public static WebApplication MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard", async (IDashboardService service, int? year = null) =>
        {
            return await service.GetDashboardForUserAsync(year);
        })
        .RequireAuthorization();

        return app;
    }
}
