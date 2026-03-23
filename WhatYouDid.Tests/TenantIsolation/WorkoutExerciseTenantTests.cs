using WhatYouDid.Shared;

namespace WhatYouDid.Tests.TenantIsolation;

[Collection("Database")]
public class WorkoutExerciseTenantTests(DatabaseFixture fixture)
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
                    Name = "Deadlift",
                    Sequence = 1,
                    Sets = 3,
                    HasReps = true,
                    HasWeight = true,
                    HasDuration = false
                }
            ]
        });
        var routines = await api.GetUserRoutinesAsync();
        var routineId = routines.First(r => r.Name == routineName).RoutineId;
        var full = await api.GetRoutineAsync(routineId);
        return (routineId, full!.Exercises[0].ExerciseId);
    }

    [Fact]
    public async Task GetStartWorkoutDtoAsync_LastWorkoutData_OnlyFromCurrentUser()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await fixture.CreateUserAsync($"we-start-a-{id}@test.com", "Test1234!");
        var userB = await fixture.CreateUserAsync($"we-start-b-{id}@test.com", "Test1234!");

        // Use a public routine so both users can see it
        int routineId;
        int exerciseId;
        using (var db = fixture.CreateDbContextForTenant(userA.Id))
        {
            var routine = new WhatYouDid.Data.Routine
            {
                Name = $"Shared Routine {id}",
                CreateUserId = userA.Id,
                IsPublic = true,
                Exercises =
                [
                    new WhatYouDid.Data.Exercise
                    {
                        Name = "Deadlift",
                        Sequence = 1,
                        Sets = 3,
                        HasReps = true,
                        ApplicationUserId = userA.Id
                    }
                ]
            };
            db.Routines.Add(routine);
            await db.SaveChangesAsync();
            routineId = routine.RoutineId;
            exerciseId = routine.Exercises[0].ExerciseId;
        }

        // User A completes the routine and records reps
        var tenantService = new TestTenantService();
        var api = fixture.CreateApiForTenant(tenantService);

        tenantService.SetTenant(userA.Id);
        await api.SaveWorkoutAsync(new WorkoutDto
        {
            WorkoutId = Guid.NewGuid(),
            RoutineId = routineId,
            RoutineName = $"Shared Routine {id}",
            StartTime = DateTime.Now.AddHours(-1),
            WorkoutExercises =
            [
                new WorkoutExerciseDto
                {
                    Sequence = 1,
                    ExerciseId = exerciseId,
                    ExerciseName = "Deadlift",
                    Sets = 3,
                    HasReps = true,
                    HasWeights = false,
                    HasDurations = false,
                    Reps = [100, 90, 80],
                    Weights = [],
                    Durations = []
                }
            ]
        });

        // User B calls GetStartWorkoutDtoAsync — should NOT see User A's reps
        tenantService.SetTenant(userB.Id);
        var dto = await api.GetStartWorkoutDtoAsync(routineId);

        Assert.NotNull(dto);
        var exerciseDto = dto.WorkoutExercises?.FirstOrDefault(e => e.ExerciseId == exerciseId);
        Assert.NotNull(exerciseDto);

        // LastReps should be null or empty — user B has no workout history
        Assert.True(exerciseDto.LastReps is null || exerciseDto.LastReps.Length == 0);
    }

    [Fact]
    public async Task UpdateWorkoutExerciseAsync_CannotUpdate_OtherUsersExercise()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await fixture.CreateUserAsync($"we-update-a-{id}@test.com", "Test1234!");
        var userB = await fixture.CreateUserAsync($"we-update-b-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        var api = fixture.CreateApiForTenant(tenantService);

        tenantService.SetTenant(userA.Id);
        var (routineId, exerciseId) = await CreateRoutineAsync(api, $"Pull Day {id}");

        var workoutId = Guid.NewGuid();
        await api.SaveWorkoutAsync(new WorkoutDto
        {
            WorkoutId = workoutId,
            RoutineId = routineId,
            RoutineName = $"Pull Day {id}",
            StartTime = DateTime.Now.AddHours(-1),
            WorkoutExercises =
            [
                new WorkoutExerciseDto
                {
                    Sequence = 1,
                    ExerciseId = exerciseId,
                    ExerciseName = "Deadlift",
                    Sets = 3,
                    HasReps = true,
                    HasWeights = false,
                    HasDurations = false,
                    Reps = [5, 5, 5],
                    Weights = [],
                    Durations = []
                }
            ]
        });

        // User B tries to update User A's workout exercise
        tenantService.SetTenant(userB.Id);
        var updated = await api.UpdateWorkoutExerciseAsync(workoutId, new WorkoutExerciseDto
        {
            Sequence = 1,
            ExerciseId = exerciseId,
            ExerciseName = "Deadlift",
            Sets = 3,
            HasReps = true,
            HasWeights = false,
            HasDurations = false,
            Reps = [999, 999, 999],
            Weights = [],
            Durations = []
        });

        Assert.False(updated);
    }
}
