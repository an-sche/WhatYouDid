using WhatYouDid.Shared;

namespace WhatYouDid.EndpointExtensions;

public static class WeightEndpointExtensions
{
    public static IEndpointRouteBuilder MapWeightEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/weight");

        group.MapGet("", async (IWeightService service, int days = 30) =>
        {
            return await service.GetRecentEntriesAsync(days);
        });

        group.MapGet("/history", async (IWeightService service, int days = 90) =>
        {
            return await service.GetDailyAveragesAsync(days);
        });

        group.MapPost("", async (
            CreateWeightEntryDto dto,
            IWeightService service) =>
        {
            var created = await service.AddEntryAsync(dto);
            return created is null
                ? Results.Problem("Failed to log weight")
                : Results.Created($"/api/weight/{created.WeightEntryId}", created);
        });

        group.MapDelete("/{weightEntryId:guid}", async (
            Guid weightEntryId,
            IWeightService service) =>
        {
            var deleted = await service.DeleteEntryAsync(weightEntryId);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        return routes;
    }
}
