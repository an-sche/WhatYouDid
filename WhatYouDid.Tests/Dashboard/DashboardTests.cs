using WhatYouDid.Shared;

namespace WhatYouDid.Tests.Dashboard;

[Collection("Database")]
public class DashboardTests(DatabaseFixture fixture)
{
    private static async Task<(int routineId, int exerciseId)> SetupRoutineAsync(
        TestApi api, string name)
    {
        await api.AddRoutineAsync(new CreateRoutineDto
        {
            Name = name,
            Exercises =
            [
                new CreateExerciseDto
                {
                    Name = "Squat",
                    Sequence = 1,
                    Sets = 3,
                    HasReps = true,
                    HasWeight = false,
                    HasDuration = false
                }
            ]
        });
        var routines = await api.GetUserRoutinesAsync();
        var routineId = routines.First(r => r.Name == name).RoutineId;
        var full = await api.GetRoutineAsync(routineId);
        return (routineId, full!.Exercises[0].ExerciseId);
    }

    private static WorkoutDto BuildWorkout(int routineId, string routineName, int exerciseId,
        DateTime startTime, int[] reps, int?[]? alternateReps = null) =>
        new()
        {
            WorkoutId = Guid.NewGuid(),
            RoutineId = routineId,
            RoutineName = routineName,
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            WorkoutExercises =
            [
                new WorkoutExerciseDto
                {
                    Sequence = 1,
                    ExerciseId = exerciseId,
                    ExerciseName = "Squat",
                    Sets = reps.Length,
                    HasReps = true,
                    HasWeights = false,
                    HasDurations = false,
                    Reps = reps.Select(r => (int?)r).ToArray(),
                    Weights = [],
                    Durations = [],
                    AlternateReps = alternateReps ?? [],
                }
            ]
        };

    [Fact]
    public async Task TopWorkouts_RankedByFrequency()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"dash-top-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineIdA, exA) = await SetupRoutineAsync(api, $"Chest {id}");
        var (routineIdB, exB) = await SetupRoutineAsync(api, $"Back {id}");
        var (routineIdC, exC) = await SetupRoutineAsync(api, $"Legs {id}");

        // Chest: 3 times, Back: 2 times, Legs: 1 time
        var now = DateTime.Now;
        for (int i = 0; i < 3; i++)
            await api.SaveWorkoutAsync(BuildWorkout(routineIdA, $"Chest {id}", exA, now.AddHours(-(i + 10)), []));
        for (int i = 0; i < 2; i++)
            await api.SaveWorkoutAsync(BuildWorkout(routineIdB, $"Back {id}", exB, now.AddHours(-(i + 20)), []));
        await api.SaveWorkoutAsync(BuildWorkout(routineIdC, $"Legs {id}", exC, now.AddHours(-30), []));

        using var db = fixture.CreateDbContextForTenant(user.Id);
        var dashboardService = new DashboardService(db);
        var dto = await dashboardService.GetDashboardForUserAsync();

        Assert.NotNull(dto.TopWorkouts);
        var chestEntry = dto.TopWorkouts.FirstOrDefault(w => w.RoutineName == $"Chest {id}");
        var backEntry = dto.TopWorkouts.FirstOrDefault(w => w.RoutineName == $"Back {id}");
        var legsEntry = dto.TopWorkouts.FirstOrDefault(w => w.RoutineName == $"Legs {id}");

        Assert.NotNull(chestEntry);
        Assert.Equal(3, chestEntry.Count);
        Assert.NotNull(backEntry);
        Assert.Equal(2, backEntry.Count);
        Assert.NotNull(legsEntry);
        Assert.Equal(1, legsEntry.Count);

        // Chest should rank above Back
        Assert.True(dto.TopWorkouts.IndexOf(chestEntry) < dto.TopWorkouts.IndexOf(backEntry));
    }

    [Fact]
    public async Task YearFilter_ScopesResults()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"dash-year-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await SetupRoutineAsync(api, $"Yearly Routine {id}");

        // 2 workouts this year, 1 in a prior year
        await api.SaveWorkoutAsync(BuildWorkout(routineId, $"Yearly Routine {id}", exerciseId,
            new DateTime(DateTime.Now.Year, 1, 1, 10, 0, 0), []));
        await api.SaveWorkoutAsync(BuildWorkout(routineId, $"Yearly Routine {id}", exerciseId,
            new DateTime(DateTime.Now.Year, 6, 1, 10, 0, 0), []));
        await api.SaveWorkoutAsync(BuildWorkout(routineId, $"Yearly Routine {id}", exerciseId,
            new DateTime(DateTime.Now.Year - 1, 6, 1, 10, 0, 0), []));

        using var db = fixture.CreateDbContextForTenant(user.Id);
        var dashboardService = new DashboardService(db);
        var thisYear = await dashboardService.GetDashboardForUserAsync(DateTime.Now.Year);

        var entry = thisYear.TopWorkouts?.FirstOrDefault(w => w.RoutineName == $"Yearly Routine {id}");
        Assert.NotNull(entry);
        Assert.Equal(2, entry.Count);
    }

    [Fact]
    public async Task TotalReps_SumsCorrectly()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"dash-reps-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await SetupRoutineAsync(api, $"Reps Routine {id}");

        // Reps: [10, 8, 6] = 24, AlternateReps: [5, null, 3] = 8 — total should be 32
        await api.SaveWorkoutAsync(BuildWorkout(routineId, $"Reps Routine {id}", exerciseId,
            DateTime.Now.AddHours(-1), [10, 8, 6], [5, null, 3]));

        using var db = fixture.CreateDbContextForTenant(user.Id);
        var dashboardService = new DashboardService(db);
        var dto = await dashboardService.GetDashboardForUserAsync();

        Assert.Equal(32, dto.TotalReps);
    }

    [Fact]
    public async Task TotalWorkoutDuration_SumsMinutesCorrectly()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"dash-dur-{id}@test.com", "Test1234!");

        // Insert workouts with controlled start/end times directly — SaveWorkoutAsync
        // always sets EndTime = DateTime.Now so duration is non-deterministic via the API.
        var anchor = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Local);
        using (var setupDb = fixture.CreateDbContextForTenant(user.Id))
        {
            setupDb.Workouts.AddRange(
                new Workout { WorkoutId = Guid.NewGuid(), ApplicationUserId = user.Id, RoutineName = $"Dur A {id}", StartTime = anchor.AddHours(-2),  EndTime = anchor.AddMinutes(-90) }, // 30 min
                new Workout { WorkoutId = Guid.NewGuid(), ApplicationUserId = user.Id, RoutineName = $"Dur B {id}", StartTime = anchor.AddHours(-5),  EndTime = anchor.AddMinutes(-210) } // 90 min
            );
            await setupDb.SaveChangesAsync();
        }

        using var db = fixture.CreateDbContextForTenant(user.Id);
        var dto = await new DashboardService(db).GetDashboardForUserAsync();

        Assert.Equal(120, dto.TotalWorkoutDuration); // 30 + 90
    }

    [Fact]
    public async Task TotalWorkouts_CountsCorrectly()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"dash-count-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await SetupRoutineAsync(api, $"Count Routine {id}");

        await api.SaveWorkoutAsync(BuildWorkout(routineId, $"Count Routine {id}", exerciseId, DateTime.Now.AddHours(-1), []));
        await api.SaveWorkoutAsync(BuildWorkout(routineId, $"Count Routine {id}", exerciseId, DateTime.Now.AddHours(-2), []));
        await api.SaveWorkoutAsync(BuildWorkout(routineId, $"Count Routine {id}", exerciseId, DateTime.Now.AddHours(-3), []));

        using var db = fixture.CreateDbContextForTenant(user.Id);
        var dto = await new DashboardService(db).GetDashboardForUserAsync();

        Assert.Equal(3, dto.TotalWorkouts);
    }

    [Fact]
    public async Task UserA_DashboardMetrics_DoNotInclude_UserB_Workouts()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await fixture.CreateUserAsync($"dash-iso-a-{id}@test.com", "Test1234!");
        var userB = await fixture.CreateUserAsync($"dash-iso-b-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        var api = fixture.CreateApiForTenant(tenantService);

        // User A creates 1 workout with 10 reps
        tenantService.SetTenant(userA.Id);
        var (routineIdA, exA) = await SetupRoutineAsync(api, $"Isolation A {id}");
        await api.SaveWorkoutAsync(BuildWorkout(routineIdA, $"Isolation A {id}", exA,
            DateTime.Now.AddHours(-1), [10, 10, 10]));

        // User B creates 5 workouts with 100 reps each
        tenantService.SetTenant(userB.Id);
        var (routineIdB, exB) = await SetupRoutineAsync(api, $"Isolation B {id}");
        for (int i = 0; i < 5; i++)
            await api.SaveWorkoutAsync(BuildWorkout(routineIdB, $"Isolation B {id}", exB,
                DateTime.Now.AddHours(-(i + 2)), [100, 100, 100]));

        // User A's dashboard should only reflect their own data
        using var db = fixture.CreateDbContextForTenant(userA.Id);
        var dashboardService = new DashboardService(db);
        var dto = await dashboardService.GetDashboardForUserAsync();

        var userAEntry = dto.TopWorkouts?.FirstOrDefault(w => w.RoutineName == $"Isolation A {id}");
        Assert.NotNull(userAEntry);
        Assert.Equal(1, userAEntry.Count);

        Assert.DoesNotContain(dto.TopWorkouts ?? [], w => w.RoutineName == $"Isolation B {id}");
        Assert.Equal(30, dto.TotalReps);
    }
}
