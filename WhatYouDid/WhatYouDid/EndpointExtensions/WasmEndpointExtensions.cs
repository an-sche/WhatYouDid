using WhatYouDid.Services;
using WhatYouDid.Shared;

namespace WhatYouDid.EndpointExtensions;

public static class WasmEndpointExtensions
{
    public static WebApplication MapRoutineEndpoints(this WebApplication app)
    {
        app.MapGet("/api/workouts/start/{routineId}", async (
            int routineId,
            IWhatYouDidApi service) =>
        {
            return await service.GetStartWorkoutDtoAsync(routineId);
        });

        app.MapPost("/api/workouts", async (
            WorkoutDto dto,
            IWhatYouDidApi service) =>
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

        return app;
    }

}
