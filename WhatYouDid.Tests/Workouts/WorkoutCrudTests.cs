using WhatYouDid.Shared;

namespace WhatYouDid.Tests.Workouts;

[Collection("Database")]
public class WorkoutCrudTests(DatabaseFixture fixture)
{
    private async Task<(int routineId, int exerciseId)> SetupRoutineAsync(
        WhatYouDidApiDirectAccess api, string name)
    {
        await api.AddRoutineAsync(new CreateRoutineDto
        {
            Name = name,
            Exercises =
            [
                new CreateExerciseDto
                {
                    Name = "Bench Press",
                    Sequence = 1,
                    Sets = 3,
                    HasReps = true,
                    HasWeight = true,
                    HasDuration = false
                }
            ]
        });
        var routines = await api.GetUserRoutinesAsync();
        var routineId = routines.First(r => r.Name == name).RoutineId;
        var full = await api.GetRoutineAsync(routineId);
        return (routineId, full!.Exercises[0].ExerciseId);
    }

    [Fact]
    public async Task SaveWorkoutAsync_PersistsWithCorrectApplicationUserId()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-save-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await SetupRoutineAsync(api, $"Push Day Save {id}");
        var workoutId = Guid.NewGuid();
        await api.SaveWorkoutAsync(new WorkoutDto
        {
            WorkoutId = workoutId,
            RoutineId = routineId,
            RoutineName = $"Push Day Save {id}",
            StartTime = DateTime.Now.AddHours(-1),
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
                    Reps = [10, 8, 6],
                    Weights = [135, 145, 155],
                    Durations = []
                }
            ]
        });

        var workouts = await api.GetWorkoutsAsync(0, 100);
        var saved = workouts.FirstOrDefault(w => w.WorkoutId == workoutId);

        Assert.NotNull(saved);
        Assert.Equal(user.Id, saved.ApplicationUserId);
    }

    [Fact]
    public async Task GetWorkoutsAsync_OrderedByStartTimeDescending()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-order-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await SetupRoutineAsync(api, $"Order Routine {id}");

        var times = new[]
        {
            DateTime.Now.AddHours(-3),
            DateTime.Now.AddHours(-1),
            DateTime.Now.AddHours(-2)
        };

        foreach (var t in times)
        {
            await api.SaveWorkoutAsync(new WorkoutDto
            {
                WorkoutId = Guid.NewGuid(),
                RoutineId = routineId,
                RoutineName = $"Order Routine {id}",
                StartTime = t,
                WorkoutExercises = []
            });
        }

        var workouts = await api.GetWorkoutsAsync(0, 100);
        var mine = workouts.Where(w => w.RoutineName == $"Order Routine {id}").ToList();

        Assert.Equal(3, mine.Count);
        for (int i = 0; i < mine.Count - 1; i++)
            Assert.True(mine[i].StartTime >= mine[i + 1].StartTime);
    }

    [Fact]
    public async Task GetWorkoutsAsync_SearchFilters_ByRoutineName()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-search-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        // Create two routines with distinct names
        var (routineIdA, _) = await SetupRoutineAsync(api, $"Chest Day {id}");
        var (routineIdB, _) = await SetupRoutineAsync(api, $"Back Day {id}");

        await api.SaveWorkoutAsync(new WorkoutDto
        {
            WorkoutId = Guid.NewGuid(),
            RoutineId = routineIdA,
            RoutineName = $"Chest Day {id}",
            StartTime = DateTime.Now.AddHours(-2),
            WorkoutExercises = []
        });
        await api.SaveWorkoutAsync(new WorkoutDto
        {
            WorkoutId = Guid.NewGuid(),
            RoutineId = routineIdB,
            RoutineName = $"Back Day {id}",
            StartTime = DateTime.Now.AddHours(-1),
            WorkoutExercises = []
        });

        var results = await api.GetWorkoutsAsync(0, 100, $"Chest Day {id}");
        var count = await api.GetWorkoutsCountAsync($"Chest Day {id}");

        Assert.All(results, w => Assert.Contains($"Chest Day {id}", w.RoutineName));
        Assert.Equal(results.Count, count);
    }

    [Fact]
    public async Task GetStartWorkoutDtoAsync_ReturnsLastWorkoutsRepsForEachExercise()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-lastset-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await SetupRoutineAsync(api, $"Last Set Routine {id}");

        // First (older) workout
        await api.SaveWorkoutAsync(new WorkoutDto
        {
            WorkoutId = Guid.NewGuid(),
            RoutineId = routineId,
            RoutineName = $"Last Set Routine {id}",
            StartTime = DateTime.Now.AddDays(-2),
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
                    Durations = []
                }
            ]
        });

        // Second (more recent) workout
        await api.SaveWorkoutAsync(new WorkoutDto
        {
            WorkoutId = Guid.NewGuid(),
            RoutineId = routineId,
            RoutineName = $"Last Set Routine {id}",
            StartTime = DateTime.Now.AddDays(-1),
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
                    Reps = [10, 8, 6],
                    Weights = [135, 145, 155],
                    Durations = []
                }
            ]
        });

        var dto = await api.GetStartWorkoutDtoAsync(routineId);

        Assert.NotNull(dto);
        var exercise = dto.WorkoutExercises?.FirstOrDefault(e => e.ExerciseId == exerciseId);
        Assert.NotNull(exercise);

        // LastReps should come from the most recent workout
        Assert.NotNull(exercise.LastReps);
        Assert.Equal([10, 8, 6], exercise.LastReps);
        Assert.Equal([135, 145, 155], exercise.LastWeights);
    }
}
