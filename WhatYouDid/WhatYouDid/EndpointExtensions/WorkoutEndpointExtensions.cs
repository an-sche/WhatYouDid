using System.Text;
using WhatYouDid.Shared;

namespace WhatYouDid.EndpointExtensions;

public static class WorkoutEndpointExtensions
{
    public static IEndpointRouteBuilder MapWorkoutEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/workouts");

        group.MapGet("", async (
            IWorkoutService service,
            int page = 0,
            int pageSize = 10,
            string? search = null) =>
        {
            return await service.GetWorkoutsAsync(page, pageSize, search);
        });

        group.MapGet("/{workoutId:guid}", async (Guid workoutId, IWorkoutService service) =>
        {
            var dto = await service.GetCompletedWorkoutDtoAsync(workoutId);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapPatch("/{workoutId:guid}/exercises", async (
            Guid workoutId,
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

        group.MapGet("/history/{exerciseId:int}", async (int exerciseId, IWorkoutService service, int? last = null) =>
        {
            var history = await service.GetExerciseHistoryAsync(exerciseId, last);
            return history is null ? Results.NotFound() : Results.Ok(history);
        });

        group.MapGet("/export/csv", async (IWorkoutService service, int? year = null) =>
        {
            var rows = await service.GetAllWorkoutsForExportAsync(year);

            var sb = new StringBuilder();
            sb.AppendLine("StartTime,WorkoutDuration,RoutineName,ExerciseName,SetNumber,Reps,Weight,Duration,AlternateReps,AlternateWeight,AlternateDuration,Note");
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(',',
                    row.StartTime.ToString("yyyy-MM-dd HH:mm"),
                    FormatDuration(row.StartTime, row.EndTime),
                    CsvField(row.RoutineName),
                    CsvField(row.ExerciseName),
                    row.SetNumber,
                    row.Reps,
                    row.Weight,
                    row.Duration,
                    row.AlternateReps,
                    row.AlternateWeight,
                    row.AlternateDuration,
                    CsvField(row.Note)
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var filename = year.HasValue ? $"workouts-{year}.csv" : "workouts.csv";
            return Results.File(bytes, "text/csv", filename);
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

    private static string FormatDuration(DateTimeOffset start, DateTimeOffset? end)
    {
        if (end is null) return "";
        var total = (int)(end.Value - start).TotalMinutes;
        if (total < 0) return "";
        return total < 60 ? $"{total} min" : $"{total / 60}h {total % 60}m";
    }

    private static string CsvField(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
