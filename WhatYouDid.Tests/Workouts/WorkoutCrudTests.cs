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
    public async Task GetWorkoutsAsync_OrderedByEndTimeDescending()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-order-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await SetupRoutineAsync(api, $"Order Routine {id}");

        var times = new[]
        {
            DateTimeOffset.Now.AddHours(-3),
            DateTimeOffset.Now.AddHours(-1),
            DateTimeOffset.Now.AddHours(-2)
        };

        foreach (var t in times)
        {
            await api.SaveWorkoutAsync(new WorkoutDto
            {
                WorkoutId = Guid.NewGuid(),
                RoutineId = routineId,
                RoutineName = $"Order Routine {id}",
                StartTime = t,
                EndTime = t.AddMinutes(45),
                WorkoutExercises = []
            });
        }

        var workouts = await api.GetWorkoutsAsync(0, 100);
        var mine = workouts.Where(w => w.RoutineName == $"Order Routine {id}").ToList();

        Assert.Equal(3, mine.Count);
        for (int i = 0; i < mine.Count - 1; i++)
            Assert.True(mine[i].EndTime >= mine[i + 1].EndTime);
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
    public async Task GetWorkoutsCountAsync_ReturnsCorrectCount()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-count-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, _) = await SetupRoutineAsync(api, $"Count Routine {id}");

        for (int i = 0; i < 3; i++)
            await api.SaveWorkoutAsync(new WorkoutDto
            {
                WorkoutId = Guid.NewGuid(),
                RoutineId = routineId,
                RoutineName = $"Count Routine {id}",
                StartTime = DateTime.Now.AddHours(-(i + 1)),
                WorkoutExercises = []
            });

        var count = await api.GetWorkoutsCountAsync();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetWorkoutsAsync_Pagination_RespectsStartIndexAndCount()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-page-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, _) = await SetupRoutineAsync(api, $"Page Routine {id}");

        for (int i = 0; i < 5; i++)
            await api.SaveWorkoutAsync(new WorkoutDto
            {
                WorkoutId = Guid.NewGuid(),
                RoutineId = routineId,
                RoutineName = $"Page Routine {id}",
                StartTime = DateTime.Now.AddHours(-(i + 1)),
                WorkoutExercises = []
            });

        var page1 = await api.GetWorkoutsAsync(0, 2, $"Page Routine {id}");
        var page2 = await api.GetWorkoutsAsync(2, 2, $"Page Routine {id}");
        var page3 = await api.GetWorkoutsAsync(4, 2, $"Page Routine {id}");

        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.Single(page3);
        // Pages should not overlap
        Assert.Empty(page1.Select(w => w.WorkoutId).Intersect(page2.Select(w => w.WorkoutId)));
    }

    [Fact]
    public async Task GetCompletedWorkoutDtoAsync_ReturnsWorkoutWithExercises()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-completed-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await SetupRoutineAsync(api, $"Completed Routine {id}");
        var workoutId = Guid.NewGuid();

        await api.SaveWorkoutAsync(new WorkoutDto
        {
            WorkoutId = workoutId,
            RoutineId = routineId,
            RoutineName = $"Completed Routine {id}",
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

        var dto = await api.GetCompletedWorkoutDtoAsync(workoutId);

        Assert.NotNull(dto);
        Assert.Equal(workoutId, dto.WorkoutId);
        Assert.Equal($"Completed Routine {id}", dto.RoutineName);
        Assert.NotNull(dto.WorkoutExercises);
        Assert.Single(dto.WorkoutExercises);

        var exercise = dto.WorkoutExercises[0];
        Assert.Equal(exerciseId, exercise.ExerciseId);
        Assert.Equal([10, 8, 6], exercise.Reps);
        Assert.Equal([135, 145, 155], exercise.Weights);
    }

    [Fact]
    public async Task UpdateWorkoutExerciseAsync_UpdatesExerciseData()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-update-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, exerciseId) = await SetupRoutineAsync(api, $"Update Routine {id}");
        var workoutId = Guid.NewGuid();

        await api.SaveWorkoutAsync(new WorkoutDto
        {
            WorkoutId = workoutId,
            RoutineId = routineId,
            RoutineName = $"Update Routine {id}",
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
                    Reps = [5, 5, 5],
                    Weights = [100, 100, 100],
                    Durations = []
                }
            ]
        });

        var updated = await api.UpdateWorkoutExerciseAsync(workoutId, new WorkoutExerciseDto
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
        });

        Assert.True(updated);

        var dto = await api.GetCompletedWorkoutDtoAsync(workoutId);
        var exercise = dto!.WorkoutExercises!.First(e => e.ExerciseId == exerciseId);
        Assert.Equal([10, 8, 6], exercise.Reps);
        Assert.Equal([135, 145, 155], exercise.Weights);
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

    [Fact]
    public async Task SaveWorkoutAsync_EndTimeBeforeStartTime_Throws()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"workout-endtime-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var (routineId, _) = await SetupRoutineAsync(api, $"EndTime Routine {id}");

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.SaveWorkoutAsync(new WorkoutDto
        {
            WorkoutId = Guid.NewGuid(),
            RoutineId = routineId,
            RoutineName = $"EndTime Routine {id}",
            StartTime = DateTimeOffset.Now,
            EndTime = DateTimeOffset.Now.AddHours(-1),
            WorkoutExercises = []
        }));
    }
}
