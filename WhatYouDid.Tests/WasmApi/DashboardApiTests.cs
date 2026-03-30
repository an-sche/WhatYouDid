using System.Net;
using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Tests.WasmApi;

[Collection("WasmApi")]
public class DashboardApiTests(ApiWebApplicationFactory factory)
{
    // -------------------------------------------------------------------------
    // GET /api/dashboard
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetDashboard_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_Authenticated_ReturnsDto()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"dash-basic-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<DashboardDto>();
        Assert.NotNull(dto);
    }

    [Fact]
    public async Task GetDashboard_ReflectsOwnWorkouts()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"dash-count-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Dash Routine {id}");
        await factory.SaveWorkoutAsync(user.Id, routineId, $"Dash Routine {id}");
        await factory.SaveWorkoutAsync(user.Id, routineId, $"Dash Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/dashboard");

        var dto = await response.Content.ReadFromJsonAsync<DashboardDto>();
        Assert.NotNull(dto);
        Assert.True(dto.TotalWorkouts >= 2);
        var entry = dto.TopWorkouts?.FirstOrDefault(w => w.RoutineName == $"Dash Routine {id}");
        Assert.NotNull(entry);
        Assert.Equal(2, entry.Count);
    }

    [Fact]
    public async Task GetDashboard_DoesNotInclude_OtherUsersWorkouts()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await factory.CreateUserAsync($"dash-iso-a-{id}@test.com", "Test1234!");
        var userB = await factory.CreateUserAsync($"dash-iso-b-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(userA.Id, $"UserA Dash Routine {id}");
        await factory.SaveWorkoutAsync(userA.Id, routineId, $"UserA Dash Routine {id}");
        var client = factory.CreateAuthenticatedClient(userB.Id);

        var response = await client.GetAsync("/api/dashboard");

        var dto = await response.Content.ReadFromJsonAsync<DashboardDto>();
        Assert.NotNull(dto);
        Assert.DoesNotContain(dto.TopWorkouts ?? [], w => w.RoutineName == $"UserA Dash Routine {id}");
    }

    [Fact]
    public async Task GetDashboard_YearFilter_ScopesResults()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"dash-year-{id}@test.com", "Test1234!");
        var routineId = await factory.CreateRoutineAsync(user.Id, $"Year Routine {id}");
        // Save workouts in the current year via the factory (uses DateTimeOffset.Now)
        await factory.SaveWorkoutAsync(user.Id, routineId, $"Year Routine {id}");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var currentYear = DateTime.Now.Year;
        var filteredResponse = await client.GetAsync($"/api/dashboard?year={currentYear}");
        var allResponse = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, filteredResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);

        var filtered = await filteredResponse.Content.ReadFromJsonAsync<DashboardDto>();
        var all = await allResponse.Content.ReadFromJsonAsync<DashboardDto>();

        Assert.NotNull(filtered);
        Assert.NotNull(all);
        Assert.Equal(currentYear, filtered.Year);
        // Filtered total should be <= overall total
        Assert.True(filtered.TotalWorkouts <= all.TotalWorkouts);
    }
}
