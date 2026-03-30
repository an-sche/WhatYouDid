using WhatYouDid.Services;

namespace WhatYouDid.EndpointExtensions;

public static class AdminEndpointExtensions
{
    record CreateUserRequest(string Email, string Password);
    record ResetPasswordRequest(string NewPassword);

    public static WebApplication MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet("/api/admin/users", async (IAdminService service) =>
        {
            return await service.GetUsersAsync();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        app.MapPost("/api/admin/users", async (CreateUserRequest request, IAdminService service) =>
        {
            var result = await service.CreateUserAsync(request.Email, request.Password);
            if (result.Succeeded)
                return Results.Created();
            return Results.BadRequest(result.Errors.Select(e => e.Description));
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        app.MapPost("/api/admin/users/{userId}/reset-password", async (
            string userId,
            ResetPasswordRequest request,
            IAdminService service) =>
        {
            var result = await service.ResetPasswordAsync(userId, request.NewPassword);
            if (result.Succeeded)
                return Results.Ok();
            var firstError = result.Errors.FirstOrDefault();
            if (firstError?.Code == "UserNotFound")
                return Results.NotFound();
            return Results.BadRequest(result.Errors.Select(e => e.Description));
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        return app;
    }
}
