using System.Net;
using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Tests.WasmApi;

[Collection("WasmApi")]
public class WorkoutApiTests(ApiWebApplicationFactory factory)
{
    // -------------------------------------------------------------------------
    // GET /api/workouts
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetWorkouts_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/workouts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkouts_Authenticated_ReturnsOwnWorkouts()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wh-list-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"List Routine {id}");
        await factory.SaveWorkoutAsync(user.Id, routineId, $"List Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/workouts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutDto>>();
        Assert.NotNull(workouts);
        Assert.Contains(workouts, w => w.RoutineName == $"List Routine {id}");
    }

    [Fact]
    public async Task GetWorkouts_DoesNotInclude_OtherUsersWorkouts()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await factory.CreateUserAsync($"wh-iso-a-{id}@test.com", "Test1234!");
        var userB = await factory.CreateUserAsync($"wh-iso-b-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(userA.Id, $"Isolated Routine {id}");
        await factory.SaveWorkoutAsync(userA.Id, routineId, $"Isolated Routine {id}");
        var client = factory.CreateAuthenticatedClient(userB.Id);

        var response = await client.GetAsync("/api/workouts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutDto>>();
        Assert.NotNull(workouts);
        Assert.DoesNotContain(workouts, w => w.RoutineName == $"Isolated Routine {id}");
    }

    [Fact]
    public async Task GetWorkouts_SearchFilters_ByRoutineName()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wh-search-{id}@test.com", "Test1234!");
        var routineAId = await factory.CreateRoutineAsync(user.Id, $"Chest Day {id}");
        var routineBId = await factory.CreateRoutineAsync(user.Id, $"Back Day {id}");
        await factory.SaveWorkoutAsync(user.Id, routineAId, $"Chest Day {id}");
        await factory.SaveWorkoutAsync(user.Id, routineBId, $"Back Day {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/workouts?search=Chest+Day+{id}");

        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutDto>>();
        Assert.NotNull(workouts);
        Assert.All(workouts, w => Assert.Contains($"Chest Day {id}", w.RoutineName));
    }

    [Fact]
    public async Task GetWorkouts_PaginationParams_Work()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wh-page-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Page Routine {id}");
        for (int i = 0; i < 3; i++)
            await factory.SaveWorkoutAsync(user.Id, routineId, $"Page Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var page1 = await client.GetAsync($"/api/workouts?startIndex=0&count=2&search=Page+Routine+{id}");
        var page2 = await client.GetAsync($"/api/workouts?startIndex=2&count=2&search=Page+Routine+{id}");

        var p1 = await page1.Content.ReadFromJsonAsync<List<WorkoutDto>>();
        var p2 = await page2.Content.ReadFromJsonAsync<List<WorkoutDto>>();
        Assert.NotNull(p1);
        Assert.NotNull(p2);
        Assert.Equal(2, p1.Count);
        Assert.Single(p2);
        Assert.Empty(p1.Select(w => w.WorkoutId).Intersect(p2.Select(w => w.WorkoutId)));
    }

    // -------------------------------------------------------------------------
    // GET /api/workouts/count
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetWorkoutsCount_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/workouts/count");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkoutsCount_Authenticated_ReturnsCorrectCount()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wh-count-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Count Routine {id}");
        await factory.SaveWorkoutAsync(user.Id, routineId, $"Count Routine {id}");
        await factory.SaveWorkoutAsync(user.Id, routineId, $"Count Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/workouts/count?search=Count+Routine+{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var count = await response.Content.ReadFromJsonAsync<int>();
        Assert.Equal(2, count);
    }

    // -------------------------------------------------------------------------
    // GET /api/workouts/{workoutId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetWorkout_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/workouts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkout_Authenticated_ValidId_ReturnsDto()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wh-get-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Get Workout Routine {id}");
        var workoutId = await factory.SaveWorkoutAsync(user.Id, routineId, $"Get Workout Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/workouts/{workoutId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<WorkoutDto>();
        Assert.NotNull(dto);
        Assert.Equal(workoutId, dto.WorkoutId);
        Assert.Equal($"Get Workout Routine {id}", dto.RoutineName);
    }

    [Fact]
    public async Task GetWorkout_Nonexistent_Returns404()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wh-404-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/workouts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkout_OtherUsersWorkout_Returns404()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await factory.CreateUserAsync($"wh-sec-a-{id}@test.com", "Test1234!");
        var userB = await factory.CreateUserAsync($"wh-sec-b-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(userA.Id, $"Secure Routine {id}");
        var workoutId = await factory.SaveWorkoutAsync(userA.Id, routineId, $"Secure Routine {id}");
        var client = factory.CreateAuthenticatedClient(userB.Id);

        var response = await client.GetAsync($"/api/workouts/{workoutId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // PATCH /api/workouts/{workoutId}/exercises/{exerciseId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateWorkoutExercise_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var dto = new WorkoutExerciseDto
        {
            Sequence = 1,
            ExerciseId = 1,
            ExerciseName = "Test",
            Sets = 1,
            HasReps = false,
            HasWeights = false,
            HasDurations = false,
        };

        var response = await client.PatchAsJsonAsync($"/api/workouts/{Guid.NewGuid()}/exercises/1", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkoutExercise_Authenticated_ValidIds_Returns200()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wh-patch-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Patch Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        // Save a workout with an exercise via the WASM endpoint
        var exerciseId = await GetFirstExerciseIdAsync(client, routineId);
        var workoutId = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/workouts", new WorkoutDto
        {
            WorkoutId = workoutId,
            RoutineId = routineId,
            RoutineName = $"Patch Routine {id}",
            StartTime = DateTimeOffset.Now.AddHours(-1),
            WorkoutExercises =
            [
                new WorkoutExerciseDto
                {
                    Sequence = 1,
                    ExerciseId = exerciseId,
                    ExerciseName = "Bench Press",
                    Sets = 3,
                    HasReps = true,
                    HasWeights = true,
                    HasDurations = false,
                    Reps = [5, 5, 5],
                    Weights = [100, 100, 100],
                }
            ]
        });

        var patch = new WorkoutExerciseDto
        {
            Sequence = 1,
            ExerciseId = exerciseId,
            ExerciseName = "Bench Press",
            Sets = 3,
            HasReps = true,
            HasWeights = true,
            HasDurations = false,
            Reps = [10, 8, 6],
            Weights = [135, 145, 155],
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/workouts/{workoutId}/exercises/{exerciseId}", patch);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkoutExercise_NonexistentWorkout_Returns404()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wh-patch-404-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);
        var dto = new WorkoutExerciseDto
        {
            Sequence = 1,
            ExerciseId = 1,
            ExerciseName = "Test",
            Sets = 1,
            HasReps = false,
            HasWeights = false,
            HasDurations = false,
        };

        var response = await client.PatchAsJsonAsync(
            $"/api/workouts/{Guid.NewGuid()}/exercises/1", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/workouts/{workoutId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteWorkout_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/workouts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkout_Authenticated_ValidId_Returns204()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wh-del-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Delete Routine {id}");
        var workoutId = await factory.SaveWorkoutAsync(user.Id, routineId, $"Delete Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.DeleteAsync($"/api/workouts/{workoutId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkout_Authenticated_WorkoutRemovedFromList()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wh-del-gone-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Del Gone Routine {id}");
        var workoutId = await factory.SaveWorkoutAsync(user.Id, routineId, $"Del Gone Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        await client.DeleteAsync($"/api/workouts/{workoutId}");

        var response = await client.GetAsync("/api/workouts");
        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutDto>>();
        Assert.DoesNotContain(workouts!, w => w.WorkoutId == workoutId);
    }

    [Fact]
    public async Task DeleteWorkout_Nonexistent_Returns404()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"wh-del-404-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.DeleteAsync($"/api/workouts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static async Task<int> GetFirstExerciseIdAsync(HttpClient client, int routineId)
    {
        var response = await client.GetAsync($"/api/routines/{routineId}/exercises");
        var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseDto>>();
        return exercises![0].ExerciseId;
    }
}
