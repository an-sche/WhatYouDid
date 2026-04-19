using System.Net;
using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Tests.WasmApi;

[Collection("WasmApi")]
public class ProgressApiTests(ApiWebApplicationFactory factory)
{
    // -------------------------------------------------------------------------
    // GET /api/workouts/history/{exerciseId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetExerciseHistory_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/workouts/history/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetExerciseHistory_NoHistory_Returns404()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"prg-404-{id}@test.com", "Test1234!");
        var (_, exerciseIds) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        // exerciseId exists in the routine but has no workout history
        var response = await client.GetAsync($"/api/workouts/history/{exerciseIds[0]}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetExerciseHistory_ReturnsCorrectSessions()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"prg-sessions-{id}@test.com", "Test1234!");
        var (routineId, exerciseIds) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Routine {id}");
        var t0 = DateTimeOffset.UtcNow.AddDays(-2);
        var t1 = DateTimeOffset.UtcNow.AddDays(-1);
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Routine {id}", exerciseIds[0], "Bench Press", reps: 8, startTime: t0);
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Routine {id}", exerciseIds[0], "Bench Press", reps: 10, startTime: t1);
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/workouts/history/{exerciseIds[0]}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ExerciseHistoryDto>();
        Assert.NotNull(dto);
        Assert.Equal("Bench Press", dto.ExerciseName);
        Assert.Equal(2, dto.Sessions.Count);
        Assert.True(dto.Sessions[0].Date <= dto.Sessions[1].Date);
    }

    [Fact]
    public async Task GetExerciseHistory_SetsHasRepsAndWeightFlags()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"prg-flags-{id}@test.com", "Test1234!");
        var (routineId, exerciseIds) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Routine {id}");
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Routine {id}", exerciseIds[0], "Bench Press", reps: 8, weight: 135);
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/workouts/history/{exerciseIds[0]}");
        var dto = await response.Content.ReadFromJsonAsync<ExerciseHistoryDto>();

        Assert.NotNull(dto);
        Assert.True(dto.HasReps);
        Assert.True(dto.HasWeight);
        Assert.False(dto.HasDuration);
    }

    [Fact]
    public async Task GetExerciseHistory_SameExerciseName_DifferentIds_TrackedSeparately()
    {
        // Two "Push-Ups" in the same routine at different positions get separate histories
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"prg-dup-{id}@test.com", "Test1234!");
        var (routineId, exerciseIds) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Routine {id}", exerciseCount: 2);
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Routine {id}", exerciseIds[0], "Push-Ups", reps: 10);
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Routine {id}", exerciseIds[1], "Push-Ups", reps: 20);
        var client = factory.CreateAuthenticatedClient(user.Id);

        var responseA = await client.GetAsync($"/api/workouts/history/{exerciseIds[0]}");
        var responseB = await client.GetAsync($"/api/workouts/history/{exerciseIds[1]}");
        var dtoA = await responseA.Content.ReadFromJsonAsync<ExerciseHistoryDto>();
        var dtoB = await responseB.Content.ReadFromJsonAsync<ExerciseHistoryDto>();

        Assert.NotNull(dtoA);
        Assert.NotNull(dtoB);
        Assert.Equal(10, dtoA.Sessions[0].Sets[0].Reps);
        Assert.Equal(20, dtoB.Sessions[0].Sets[0].Reps);
    }

    [Fact]
    public async Task GetExerciseHistory_DoesNotInclude_OtherUsersHistory()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await factory.CreateUserAsync($"prg-iso-a-{id}@test.com", "Test1234!");
        var userB = await factory.CreateUserAsync($"prg-iso-b-{id}@test.com", "Test1234!");
        var (routineId, exerciseIds) = await factory.CreateRoutineWithExercisesAsync(userA.Id, $"Routine {id}");
        await factory.SaveWorkoutWithSetsAsync(userA.Id, routineId, $"Routine {id}", exerciseIds[0], "Bench Press", reps: 5);
        var client = factory.CreateAuthenticatedClient(userB.Id);

        var response = await client.GetAsync($"/api/workouts/history/{exerciseIds[0]}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
