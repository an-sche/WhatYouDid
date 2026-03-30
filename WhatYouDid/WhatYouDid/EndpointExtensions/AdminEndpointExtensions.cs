using WhatYouDid.Services;

namespace WhatYouDid.EndpointExtensions;

public static class AdminEndpointExtensions
{
    record CreateUserRequest(string Email, string Password);
    record ResetPasswordRequest(string NewPassword);

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/admin")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/users", async (IAdminService service) =>
        {
            return await service.GetUsersAsync();
        });

        group.MapPost("/users", async (CreateUserRequest request, IAdminService service) =>
        {
            var result = await service.CreateUserAsync(request.Email, request.Password);
            if (result.Succeeded)
                return Results.Created();
            return Results.BadRequest(result.Errors.Select(e => e.Description));
        });

        group.MapPost("/users/{userId}/reset-password", async (
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
        });

        return routes;
    }
}
