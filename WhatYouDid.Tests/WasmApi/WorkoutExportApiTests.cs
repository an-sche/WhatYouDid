using System.Net;

namespace WhatYouDid.Tests.WasmApi;

[Collection("WasmApi")]
public class WorkoutExportApiTests(ApiWebApplicationFactory factory)
{
    [Fact]
    public async Task ExportCsv_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/workouts/export/csv");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExportCsv_Authenticated_ReturnsCsvContentType()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"csv-hdr-{id}@test.com", "Test1234!");
        var (routineId, exerciseId) = await factory.CreateRoutineWithAllFieldTypesAsync(user.Id, $"Export Routine {id}");
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Export Routine {id}", exerciseId, "Full Body Exercise", reps: 8, weight: 100);
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/workouts/export/csv");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("workouts.csv", response.Content.Headers.ContentDisposition?.FileName ?? "");
    }

    [Fact]
    public async Task ExportCsv_Authenticated_ContainsHeaderRowAndSetData()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"csv-data-{id}@test.com", "Test1234!");
        var (routineId, exerciseId) = await factory.CreateRoutineWithAllFieldTypesAsync(user.Id, $"Set Data Routine {id}");
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Set Data Routine {id}", exerciseId, "Full Body Exercise", reps: 10, weight: 135);
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/workouts/export/csv");
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Contains("StartTime,WorkoutDuration,RoutineName,ExerciseName,SetNumber,Reps,Weight", csv);
        Assert.Contains($"Set Data Routine {id}", csv);
        Assert.Contains("Full Body Exercise", csv);
        Assert.Contains("10", csv);
        Assert.Contains("135", csv);
    }

    [Fact]
    public async Task ExportCsv_DoesNotInclude_OtherUsersData()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await factory.CreateUserAsync($"csv-iso-a-{id}@test.com", "Test1234!");
        var userB = await factory.CreateUserAsync($"csv-iso-b-{id}@test.com", "Test1234!");
        var (routineId, exerciseId) = await factory.CreateRoutineWithAllFieldTypesAsync(userA.Id, $"UserA Private Routine {id}");
        await factory.SaveWorkoutWithSetsAsync(userA.Id, routineId, $"UserA Private Routine {id}", exerciseId, "Full Body Exercise", reps: 5);
        var client = factory.CreateAuthenticatedClient(userB.Id);

        var response = await client.GetAsync("/api/workouts/export/csv");
        var csv = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain($"UserA Private Routine {id}", csv);
    }
}
