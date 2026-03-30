using System.Net;
using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Tests.WasmApi;

[Collection("WasmApi")]
public class RoutineApiTests(ApiWebApplicationFactory factory)
{
    // -------------------------------------------------------------------------
    // GET /api/routines
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetRoutines_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/routines");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRoutines_Authenticated_ReturnsOwnRoutines()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"rt-list-{id}@test.com", "Test1234!");
        await factory.CreateRoutineAsync(user.Id, $"My Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/routines");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var routines = await response.Content.ReadFromJsonAsync<List<RoutineDto>>();
        Assert.NotNull(routines);
        Assert.Contains(routines, r => r.Name == $"My Routine {id}");
    }

    [Fact]
    public async Task GetRoutines_DoesNotInclude_OtherUsersPrivateRoutines()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var owner = await factory.CreateUserAsync($"rt-owner-{id}@test.com", "Test1234!");
        var other = await factory.CreateUserAsync($"rt-other-{id}@test.com", "Test1234!");
        await factory.CreateRoutineAsync(owner.Id, $"Private Routine {id}");
        var client = factory.CreateAuthenticatedClient(other.Id);

        var response = await client.GetAsync("/api/routines");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var routines = await response.Content.ReadFromJsonAsync<List<RoutineDto>>();
        Assert.NotNull(routines);
        Assert.DoesNotContain(routines, r => r.Name == $"Private Routine {id}");
    }

    // -------------------------------------------------------------------------
    // GET /api/routines/{routineId}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetRoutine_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/routines/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRoutine_Authenticated_ValidId_ReturnsRoutineWithExercises()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"rt-get-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Get Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/routines/{routineId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var routine = await response.Content.ReadFromJsonAsync<RoutineDetailDto>();
        Assert.NotNull(routine);
        Assert.Equal($"Get Routine {id}", routine.Name);
        Assert.NotNull(routine.Exercises);
        Assert.Single(routine.Exercises);
    }

    [Fact]
    public async Task GetRoutine_Nonexistent_Returns404()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"rt-404-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/routines/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRoutine_OtherUsersPrivateRoutine_Returns404()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var owner = await factory.CreateUserAsync($"rt-own2-{id}@test.com", "Test1234!");
        var other = await factory.CreateUserAsync($"rt-oth2-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(owner.Id, $"Private Get {id}");
        var client = factory.CreateAuthenticatedClient(other.Id);

        var response = await client.GetAsync($"/api/routines/{routineId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // GET /api/routines/{routineId}/exercises
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetExercises_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/routines/1/exercises");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetExercises_Authenticated_ReturnsExercises()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"rt-ex-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Exercises Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/routines/{routineId}/exercises");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseDto>>();
        Assert.NotNull(exercises);
        Assert.Single(exercises);
        Assert.Equal("Bench Press", exercises[0].Name);
    }

    [Fact]
    public async Task GetExercises_NonexistentRoutine_ReturnsEmptyList()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"rt-ex-empty-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/routines/999999/exercises");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseDto>>();
        Assert.NotNull(exercises);
        Assert.Empty(exercises);
    }

    // -------------------------------------------------------------------------
    // POST /api/routines
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateRoutine_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var dto = new CreateRoutineDto { Name = "Test", Exercises = [] };

        var response = await client.PostAsJsonAsync("/api/routines", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoutine_Authenticated_Returns201()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"rt-create-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);
        var dto = new CreateRoutineDto
        {
            Name = $"New Routine {id}",
            Exercises =
            [
                new CreateExerciseDto
                {
                    Name = "Squat",
                    Sequence = 1,
                    Sets = 3,
                    HasReps = true,
                    HasWeight = true,
                }
            ]
        };

        var response = await client.PostAsJsonAsync("/api/routines", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoutine_Authenticated_RoutineIsRetrievable()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"rt-persist-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);
        var dto = new CreateRoutineDto
        {
            Name = $"Persist Routine {id}",
            Exercises = []
        };

        await client.PostAsJsonAsync("/api/routines", dto);

        var response = await client.GetAsync("/api/routines");
        var routines = await response.Content.ReadFromJsonAsync<List<RoutineDto>>();
        Assert.Contains(routines!, r => r.Name == $"Persist Routine {id}");
    }
}
