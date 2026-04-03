using WhatYouDid.Shared;

namespace WhatYouDid.EndpointExtensions;

public static class WorkoutEndpointExtensions
{
    public static IEndpointRouteBuilder MapWorkoutEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/workouts");

        group.MapGet("", async (
            IWorkoutService service,
            int startIndex = 0,
            int count = 10,
            string? search = null) =>
        {
            return await service.GetWorkoutsAsync(startIndex, count, search);
        });

        group.MapGet("/count", async (IWorkoutService service, string? search = null) =>
        {
            return await service.GetWorkoutsCountAsync(search);
        });

        group.MapGet("/{workoutId:guid}", async (Guid workoutId, IWorkoutService service) =>
        {
            var dto = await service.GetCompletedWorkoutDtoAsync(workoutId);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapPatch("/{workoutId:guid}/exercises/{exerciseId:int}", async (
            Guid workoutId,
            int exerciseId,
            WorkoutExerciseDto exercise,
            IWorkoutService service) =>
        {
            var updated = await service.UpdateWorkoutExerciseAsync(workoutId, exercise);
            return updated ? Results.Ok() : Results.NotFound();
        });

        group.MapDelete("/{workoutId:guid}", async (Guid workoutId, IWorkoutService service) =>
        {
            var deleted = await service.DeleteWorkoutAsync(workoutId);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        group.MapGet("/start/{routineId}", async (
            int routineId,
            IWorkoutService service) =>
        {
            return await service.GetStartWorkoutDtoAsync(routineId);
        });

        group.MapPost("", async (
            WorkoutDto dto,
            IWorkoutService service) =>
        {
            try
            {
                await service.SaveWorkoutAsync(dto);
                return Results.Created();
            }
            catch
            {
                return Results.Problem("Failed to create workout");
            }
        });

        return routes;
    }
}
