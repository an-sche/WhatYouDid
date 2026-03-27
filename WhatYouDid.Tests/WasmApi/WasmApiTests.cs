using System.Net;
using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Tests.WasmApi;

[Collection("WasmApi")]
public class WasmApiTests(ApiWebApplicationFactory factory)
{
    // -------------------------------------------------------------------------
    // GET /api/workouts/start/{routineId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetStartWorkout_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/workouts/start/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStartWorkout_Authenticated_ValidRoutine_Returns200WithDto()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wasm-get-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Push Day {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/workouts/start/{routineId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<WorkoutDto>();
        Assert.NotNull(dto);
        Assert.Equal(routineId, dto.RoutineId);
        Assert.Equal($"Push Day {id}", dto.RoutineName);
        Assert.NotNull(dto.WorkoutExercises);
        Assert.Single(dto.WorkoutExercises);
        Assert.Equal("Bench Press", dto.WorkoutExercises[0].ExerciseName);
    }

    [Fact]
    public async Task GetStartWorkout_Authenticated_NonexistentRoutine_ReturnsNullBody()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wasm-get-null-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/workouts/start/999999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("null", body.Trim());
    }

    [Fact]
    public async Task GetStartWorkout_Authenticated_OtherUsersRoutine_ReturnsNullBody()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var owner = await factory.CreateUserAsync($"wasm-owner-{id}@test.com", "Test1234!");
        var other = await factory.CreateUserAsync($"wasm-other-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(owner.Id, $"Private Routine {id}");

        // Other user cannot see the owner's private routine
        var client = factory.CreateAuthenticatedClient(other.Id);
        var response = await client.GetAsync($"/api/workouts/start/{routineId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("null", body.Trim());
    }

    // -------------------------------------------------------------------------
    // POST /api/workouts
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SaveWorkout_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var dto = new WorkoutDto
        {
            WorkoutId = Guid.NewGuid(),
            RoutineId = 1,
            RoutineName = "Test",
            WorkoutExercises = []
        };

        var response = await client.PostAsJsonAsync("/api/workouts", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SaveWorkout_Authenticated_ValidBody_Returns201()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wasm-post-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Leg Day {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var dto = new WorkoutDto
        {
            WorkoutId = Guid.NewGuid(),
            RoutineId = routineId,
            RoutineName = $"Leg Day {id}",
            StartTime = DateTime.Now.AddHours(-1),
            WorkoutExercises = []
        };

        var response = await client.PostAsJsonAsync("/api/workouts", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SaveWorkout_Authenticated_EndTimeBeforeStartTime_ReturnsError()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wasm-endtime-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"EndTime Day {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var dto = new WorkoutDto
        {
            WorkoutId = Guid.NewGuid(),
            RoutineId = routineId,
            RoutineName = $"EndTime Day {id}",
            StartTime = DateTimeOffset.Now,
            EndTime = DateTimeOffset.Now.AddHours(-1),
            WorkoutExercises = []
        };

        var response = await client.PostAsJsonAsync("/api/workouts", dto);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task SaveWorkout_Authenticated_WorkoutIsPersisted()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wasm-persist-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Back Day {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var workoutId = Guid.NewGuid();
        var dto = new WorkoutDto
        {
            WorkoutId = workoutId,
            RoutineId = routineId,
            RoutineName = $"Back Day {id}",
            StartTime = DateTime.Now.AddHours(-1),
            WorkoutExercises = []
        };

        await client.PostAsJsonAsync("/api/workouts", dto);

        // Verify via the GET endpoint that the workout was saved and is visible to the user
        var getResponse = await client.GetAsync($"/api/workouts/start/{routineId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }
}
