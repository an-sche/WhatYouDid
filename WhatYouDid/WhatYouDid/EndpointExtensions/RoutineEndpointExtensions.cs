using WhatYouDid.Services;
using WhatYouDid.Shared;

namespace WhatYouDid.EndpointExtensions;

public static class RoutineEndpointExtensions
{
    public static WebApplication MapRoutineApiEndpoints(this WebApplication app)
    {
        app.MapGet("/api/routines", async (IRoutineService service) =>
        {
            var routines = await service.GetUserRoutinesAsync();
            return routines.Select(r => new RoutineDto { RoutineId = r.RoutineId, Name = r.Name });
        })
        .RequireAuthorization();

        app.MapGet("/api/routines/{routineId:int}", async (int routineId, IRoutineService service) =>
        {
            var routine = await service.GetRoutineAsync(routineId);
            if (routine is null) return Results.NotFound();
            var dto = new RoutineDetailDto
            {
                RoutineId = routine.RoutineId,
                Name = routine.Name,
                Exercises = routine.Exercises.Select(e => new ExerciseDto
                {
                    ExerciseId = e.ExerciseId,
                    Name = e.Name,
                    Description = e.Description,
                    Sequence = e.Sequence,
                    Sets = e.Sets,
                    HasReps = e.HasReps,
                    HasWeight = e.HasWeight,
                    HasDuration = e.HasDuration,
                }).ToList()
            };
            return Results.Ok(dto);
        })
        .RequireAuthorization();

        app.MapGet("/api/routines/{routineId:int}/exercises", async (int routineId, IRoutineService service) =>
        {
            var exercises = await service.GetExercisesAsync(routineId);
            return exercises.Select(e => new ExerciseDto
            {
                ExerciseId = e.ExerciseId,
                Name = e.Name,
                Description = e.Description,
                Sequence = e.Sequence,
                Sets = e.Sets,
                HasReps = e.HasReps,
                HasWeight = e.HasWeight,
                HasDuration = e.HasDuration,
            });
        })
        .RequireAuthorization();

        app.MapPost("/api/routines", async (CreateRoutineDto dto, IRoutineService service) =>
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
        })
        .RequireAuthorization();

        return app;
    }
}
