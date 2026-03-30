using WhatYouDid.Services;
using WhatYouDid.Shared;

namespace WhatYouDid.EndpointExtensions;

public static class WorkoutEndpointExtensions
{
    public static WebApplication MapWorkoutEndpoints(this WebApplication app)
    {
        app.MapGet("/api/workouts", async (
            IWorkoutService service,
            int startIndex = 0,
            int count = 10,
            string? search = null) =>
        {
            var workouts = await service.GetWorkoutsAsync(startIndex, count, search);
            return workouts.Select(w => new WorkoutDto
            {
                WorkoutId = w.WorkoutId,
                RoutineId = w.RoutineId ?? 0,
                RoutineName = w.RoutineName,
                StartTime = w.StartTime,
                EndTime = w.EndTime,
            });
        })
        .RequireAuthorization();

        app.MapGet("/api/workouts/count", async (IWorkoutService service, string? search = null) =>
        {
            return await service.GetWorkoutsCountAsync(search);
        })
        .RequireAuthorization();

        app.MapGet("/api/workouts/{workoutId:guid}", async (Guid workoutId, IWorkoutService service) =>
        {
            var dto = await service.GetCompletedWorkoutDtoAsync(workoutId);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization();

        app.MapPatch("/api/workouts/{workoutId:guid}/exercises/{exerciseId:int}", async (
            Guid workoutId,
            int exerciseId,
            WorkoutExerciseDto exercise,
            IWorkoutService service) =>
        {
            var updated = await service.UpdateWorkoutExerciseAsync(workoutId, exercise);
            return updated ? Results.Ok() : Results.NotFound();
        })
        .RequireAuthorization();

        app.MapDelete("/api/workouts/{workoutId:guid}", async (Guid workoutId, IWorkoutService service) =>
        {
            var deleted = await service.DeleteWorkoutAsync(workoutId);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization();

        app.MapGet("/api/workouts/start/{routineId}", async (
            int routineId,
            IWorkoutService service) =>
        {
            return await service.GetStartWorkoutDtoAsync(routineId);
        })
        .RequireAuthorization();

        app.MapPost("/api/workouts", async (
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
        })
        .RequireAuthorization();

        return app;
    }
}
