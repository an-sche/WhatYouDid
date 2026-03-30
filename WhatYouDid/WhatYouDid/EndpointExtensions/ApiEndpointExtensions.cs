namespace WhatYouDid.EndpointExtensions;

public static class ApiEndpointExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();

        api.MapRoutineApiEndpoints();
        api.MapWorkoutEndpoints();
        api.MapDashboardEndpoints();
        api.MapAdminEndpoints();

        return app;
    }
}
