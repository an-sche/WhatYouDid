
using WhatYouDid.Data;
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
            return await service.SaveWorkoutAsync(dto);
        });

        return app;
    }

}
