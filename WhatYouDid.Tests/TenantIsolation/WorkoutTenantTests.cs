using WhatYouDid.Shared;

namespace WhatYouDid.Tests.TenantIsolation;

[Collection("Database")]
public class WorkoutTenantTests(DatabaseFixture fixture)
{
    private async Task<(int routineId, int exerciseId)> CreateRoutineAsync(
        WhatYouDidApiDirectAccess api, string routineName)
    {
        await api.AddRoutineAsync(new CreateRoutineDto
        {
            Name = routineName,
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
        var routineId = routines.First(r => r.Name == routineName).RoutineId;
        var full = await api.GetRoutineAsync(routineId);
        return (routineId, full!.Exercises[0].ExerciseId);
    }

    private WorkoutDto BuildWorkoutDto(int routineId, string routineName, int exerciseId,
        DateTime? startTime = null) =>
        new()
        {
            WorkoutId = Guid.NewGuid(),
            RoutineId = routineId,
            RoutineName = routineName,
            StartTime = startTime ?? DateTime.Now.AddHours(-1),
            WorkoutExercises =
            [
                new WorkoutExerciseDto
                {
                    Sequence = 1,
                    ExerciseId = exerciseId,
                    ExerciseName = "Squat",
                    Sets = 3,
                    HasReps = true,
                    HasWeights = false,
                    HasDurations = false,
                    Reps = [10, 8, 6],
                    Weights = [],
                    Durations = []
                }
            ]
        };

    [Fact]
    public async Task UserA_Workouts_NotVisibleTo_UserB()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await fixture.CreateUserAsync($"workout-a-{id}@test.com", "Test1234!");
        var userB = await fixture.CreateUserAsync($"workout-b-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        var api = fixture.CreateApiForTenant(tenantService);

        tenantService.SetTenant(userA.Id);
        var (routineId, exerciseId) = await CreateRoutineAsync(api, $"Push Day {id}");
        await api.SaveWorkoutAsync(BuildWorkoutDto(routineId, $"Push Day {id}", exerciseId));

        tenantService.SetTenant(userB.Id);
        var workouts = await api.GetWorkoutsAsync(0, 100);
        var count = await api.GetWorkoutsCountAsync();

        Assert.Empty(workouts);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task UserA_Workouts_VisibleTo_UserA()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await fixture.CreateUserAsync($"workout-own-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(userA.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await CreateRoutineAsync(api, $"Push Day Own {id}");
        await api.SaveWorkoutAsync(BuildWorkoutDto(routineId, $"Push Day Own {id}", exerciseId));

        var workouts = await api.GetWorkoutsAsync(0, 100);
        var count = await api.GetWorkoutsCountAsync();

        Assert.Single(workouts);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DeletedWorkout_ExcludedFromUsersList()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-del-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await CreateRoutineAsync(api, $"Leg Day {id}");
        var workoutDto = BuildWorkoutDto(routineId, $"Leg Day {id}", exerciseId);
        await api.SaveWorkoutAsync(workoutDto);

        await api.DeleteWorkoutAsync(workoutDto.WorkoutId);

        var workouts = await api.GetWorkoutsAsync(0, 100);
        Assert.Empty(workouts);
    }

    [Fact]
    public async Task DeleteWorkoutAsync_SetsIsDeletedAndDeletedDt()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-softdel-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await CreateRoutineAsync(api, $"Back Day {id}");
        var workoutDto = BuildWorkoutDto(routineId, $"Back Day {id}", exerciseId);
        await api.SaveWorkoutAsync(workoutDto);

        var result = await api.DeleteWorkoutAsync(workoutDto.WorkoutId);
        Assert.True(result);

        // Read the raw row, bypassing query filters
        using var db = fixture.CreateDbContextForTenant(user.Id);
        var workout = await db.Workouts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.WorkoutId == workoutDto.WorkoutId);

        Assert.NotNull(workout);
        Assert.True(workout.IsDeleted);
        Assert.NotNull(workout.DeletedDt);
    }

    [Fact]
    public async Task GetCompletedWorkoutDtoAsync_ReturnsNull_ForWrongUser()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await fixture.CreateUserAsync($"workout-completed-a-{id}@test.com", "Test1234!");
        var userB = await fixture.CreateUserAsync($"workout-completed-b-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        var api = fixture.CreateApiForTenant(tenantService);

        tenantService.SetTenant(userA.Id);
        var (routineId, exerciseId) = await CreateRoutineAsync(api, $"Chest Day {id}");
        var workoutDto = BuildWorkoutDto(routineId, $"Chest Day {id}", exerciseId);
        await api.SaveWorkoutAsync(workoutDto);

        tenantService.SetTenant(userB.Id);
        var result = await api.GetCompletedWorkoutDtoAsync(workoutDto.WorkoutId);

        Assert.Null(result);
    }
}
