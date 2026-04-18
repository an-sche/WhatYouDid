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

    [Fact]
    public async Task ExportCsv_WithYearFilter_OnlyIncludesMatchingYear()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"csv-year-{id}@test.com", "Test1234!");
        var (routineId, exerciseId) = await factory.CreateRoutineWithAllFieldTypesAsync(user.Id, $"Year Filter Routine {id}");
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Year Filter Routine {id}", exerciseId, "Full Body Exercise",
            reps: 5, startTime: new DateTimeOffset(2022, 6, 1, 10, 0, 0, TimeSpan.Zero));
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Year Filter Routine {id}", exerciseId, "Full Body Exercise",
            reps: 8, startTime: new DateTimeOffset(2023, 6, 1, 10, 0, 0, TimeSpan.Zero));
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/workouts/export/csv?year=2022");
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Contains("2022-06-01", csv);
        Assert.DoesNotContain("2023-06-01", csv);
    }

    [Fact]
    public async Task ExportCsv_WithYearFilter_FilenameIncludesYear()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"csv-fname-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/workouts/export/csv?year=2023");

        Assert.Contains("workouts-2023.csv", response.Content.Headers.ContentDisposition?.FileName ?? "");
    }

    [Fact]
    public async Task ExportCsv_WithNoYearFilter_IncludesAllYears()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"csv-all-{id}@test.com", "Test1234!");
        var (routineId, exerciseId) = await factory.CreateRoutineWithAllFieldTypesAsync(user.Id, $"All Years Routine {id}");
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"All Years Routine {id}", exerciseId, "Full Body Exercise",
            reps: 5, startTime: new DateTimeOffset(2022, 3, 1, 10, 0, 0, TimeSpan.Zero));
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"All Years Routine {id}", exerciseId, "Full Body Exercise",
            reps: 8, startTime: new DateTimeOffset(2023, 3, 1, 10, 0, 0, TimeSpan.Zero));
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/workouts/export/csv");
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Contains("2022-03-01", csv);
        Assert.Contains("2023-03-01", csv);
    }
}
