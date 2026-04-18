using System.Net;
using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Tests.WasmApi;

[Collection("WasmApi")]
public class ProgressApiTests(ApiWebApplicationFactory factory)
{
    // -------------------------------------------------------------------------
    // GET /api/workouts/exercises
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetExerciseNames_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/workouts/exercises");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetExerciseNames_ReturnsNamesFromUsersWorkouts()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"prg-names-{id}@test.com", "Test1234!");
        var (routineId, exerciseIds) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Routine {id}");
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Routine {id}", exerciseIds[0], $"Push-Up {id}", reps: 10);
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/workouts/exercises");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var names = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(names);
        Assert.Contains($"Push-Up {id}", names);
    }

    [Fact]
    public async Task GetExerciseNames_DoesNotInclude_OtherUsersExercises()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await factory.CreateUserAsync($"prg-names-a-{id}@test.com", "Test1234!");
        var userB = await factory.CreateUserAsync($"prg-names-b-{id}@test.com", "Test1234!");
        var (routineId, exerciseIds) = await factory.CreateRoutineWithExercisesAsync(userA.Id, $"Routine {id}");
        await factory.SaveWorkoutWithSetsAsync(userA.Id, routineId, $"Routine {id}", exerciseIds[0], $"Secret Lift {id}", reps: 5);
        var client = factory.CreateAuthenticatedClient(userB.Id);

        var response = await client.GetAsync("/api/workouts/exercises");

        var names = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(names);
        Assert.DoesNotContain($"Secret Lift {id}", names);
    }

    [Fact]
    public async Task GetExerciseNames_ReturnsAlphabeticallySorted()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"prg-sort-{id}@test.com", "Test1234!");
        var (routineIdA, exIdsA) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Routine A {id}");
        var (routineIdB, exIdsB) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Routine B {id}");
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineIdA, $"Routine A {id}", exIdsA[0], $"Zzz Exercise {id}", reps: 5);
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineIdB, $"Routine B {id}", exIdsB[0], $"Aaa Exercise {id}", reps: 5);
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/workouts/exercises");

        var names = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(names);
        var relevant = names.Where(n => n.EndsWith(id)).ToList();
        Assert.Equal(2, relevant.Count);
        Assert.True(string.Compare(relevant[0], relevant[1], StringComparison.Ordinal) < 0);
    }

    // -------------------------------------------------------------------------
    // GET /api/workouts/history/{exerciseName}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetExerciseHistory_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/workouts/history/PushUps");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetExerciseHistory_NoHistory_Returns404()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"prg-404-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/workouts/history/NonExistent{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetExerciseHistory_ReturnsCorrectSessions()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"prg-sessions-{id}@test.com", "Test1234!");
        var (routineId, exerciseIds) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Routine {id}");
        var exerciseName = $"Push-Up {id}";
        var t0 = DateTimeOffset.UtcNow.AddDays(-2);
        var t1 = DateTimeOffset.UtcNow.AddDays(-1);
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Routine {id}", exerciseIds[0], exerciseName, reps: 8, startTime: t0);
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Routine {id}", exerciseIds[0], exerciseName, reps: 10, startTime: t1);
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/workouts/history/{Uri.EscapeDataString(exerciseName)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ExerciseHistoryDto>();
        Assert.NotNull(dto);
        Assert.Equal(exerciseName, dto.ExerciseName);
        Assert.Equal(2, dto.Sessions.Count);
        // sessions ordered oldest first
        Assert.True(dto.Sessions[0].Date <= dto.Sessions[1].Date);
    }

    [Fact]
    public async Task GetExerciseHistory_SetsHasRepsFlag_WhenRepsPresent()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"prg-flags-{id}@test.com", "Test1234!");
        var (routineId, exerciseIds) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Routine {id}");
        var exerciseName = $"Curl {id}";
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineId, $"Routine {id}", exerciseIds[0], exerciseName, reps: 12, weight: 35);
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/workouts/history/{Uri.EscapeDataString(exerciseName)}");
        var dto = await response.Content.ReadFromJsonAsync<ExerciseHistoryDto>();

        Assert.NotNull(dto);
        Assert.True(dto.HasReps);
        Assert.True(dto.HasWeight);
        Assert.False(dto.HasDuration);
    }

    [Fact]
    public async Task GetExerciseHistory_PopulatesRoutinesList()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"prg-routines-{id}@test.com", "Test1234!");
        var exerciseName = $"Push-Up {id}";
        var (routineAId, exAIds) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Chest Day {id}");
        var (routineBId, exBIds) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Push Day {id}");
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineAId, $"Chest Day {id}", exAIds[0], exerciseName, reps: 10);
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineBId, $"Push Day {id}", exBIds[0], exerciseName, reps: 15);
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/workouts/history/{Uri.EscapeDataString(exerciseName)}");
        var dto = await response.Content.ReadFromJsonAsync<ExerciseHistoryDto>();

        Assert.NotNull(dto);
        Assert.Equal(2, dto.Sessions.Count);
        Assert.Contains($"Chest Day {id}", dto.Routines);
        Assert.Contains($"Push Day {id}", dto.Routines);
    }

    [Fact]
    public async Task GetExerciseHistory_RoutineNameFilter_ReturnsOnlyMatchingSessions()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"prg-filter-{id}@test.com", "Test1234!");
        var exerciseName = $"Push-Up {id}";
        var (routineAId, exAIds) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Chest Day {id}");
        var (routineBId, exBIds) = await factory.CreateRoutineWithExercisesAsync(user.Id, $"Push Day {id}");
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineAId, $"Chest Day {id}", exAIds[0], exerciseName, reps: 10);
        await factory.SaveWorkoutWithSetsAsync(user.Id, routineBId, $"Push Day {id}", exBIds[0], exerciseName, reps: 15);
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync(
            $"/api/workouts/history/{Uri.EscapeDataString(exerciseName)}?routineName={Uri.EscapeDataString($"Chest Day {id}")}");
        var dto = await response.Content.ReadFromJsonAsync<ExerciseHistoryDto>();

        Assert.NotNull(dto);
        Assert.Single(dto.Sessions);
        Assert.Equal($"Chest Day {id}", dto.Sessions[0].RoutineName);
    }

    [Fact]
    public async Task GetExerciseHistory_DoesNotInclude_OtherUsersHistory()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await factory.CreateUserAsync($"prg-iso-a-{id}@test.com", "Test1234!");
        var userB = await factory.CreateUserAsync($"prg-iso-b-{id}@test.com", "Test1234!");
        var exerciseName = $"Shared Name {id}";
        var (routineId, exerciseIds) = await factory.CreateRoutineWithExercisesAsync(userA.Id, $"Routine {id}");
        await factory.SaveWorkoutWithSetsAsync(userA.Id, routineId, $"Routine {id}", exerciseIds[0], exerciseName, reps: 5);
        var client = factory.CreateAuthenticatedClient(userB.Id);

        var response = await client.GetAsync($"/api/workouts/history/{Uri.EscapeDataString(exerciseName)}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
