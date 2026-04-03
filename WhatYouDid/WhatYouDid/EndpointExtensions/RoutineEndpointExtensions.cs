using WhatYouDid.Shared;

namespace WhatYouDid.EndpointExtensions;

public static class RoutineEndpointExtensions
{
    public static IEndpointRouteBuilder MapRoutineApiEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/routines");

        group.MapGet("", async (IRoutineService service) =>
        {
            return await service.GetUserRoutinesAsync();
        });

        group.MapGet("/{routineId:int}", async (int routineId, IRoutineService service) =>
        {
            var routine = await service.GetRoutineAsync(routineId);
            return routine is null ? Results.NotFound() : Results.Ok(routine);
        });

        group.MapGet("/{routineId:int}/exercises", async (int routineId, IRoutineService service) =>
        {
            return await service.GetExercisesAsync(routineId);
        });

        group.MapPost("", async (CreateRoutineDto dto, IRoutineService service) =>
        {
            try
            {
                await service.AddRoutineAsync(dto);
                return Results.Created();
            }
            catch
            {
                return Results.Problem("Failed to create routine.");
            }
        });

        return routes;
    }
}
