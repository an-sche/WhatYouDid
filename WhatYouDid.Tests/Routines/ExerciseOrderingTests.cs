using WhatYouDid.Shared;

namespace WhatYouDid.Tests.Routines;

/// <summary>
/// Tests that exercise ordering by Sequence is guaranteed.
/// These tests insert exercises in reverse Sequence order to expose missing OrderBy clauses.
/// </summary>
[Collection("Database")]
public class ExerciseOrderingTests(DatabaseFixture fixture)
{
    /// <summary>
    /// Creates a routine with exercises inserted in reverse Sequence order (3, 2, 1)
    /// so that any query without an explicit ORDER BY will expose the bug.
    /// </summary>
    private static async Task<int> CreateRoutineWithReversedSequencesAsync(TestApi api, string name)
    {
        await api.AddRoutineAsync(new CreateRoutineDto
        {
            Name = name,
            Exercises =
            [
                new CreateExerciseDto { Name = "Exercise C", Sequence = 3, Sets = 3, HasReps = true, HasWeight = false, HasDuration = false },
                new CreateExerciseDto { Name = "Exercise B", Sequence = 2, Sets = 3, HasReps = true, HasWeight = false, HasDuration = false },
                new CreateExerciseDto { Name = "Exercise A", Sequence = 1, Sets = 3, HasReps = true, HasWeight = false, HasDuration = false },
            ]
        });
        var routines = await api.GetUserRoutinesAsync();
        return routines.First(r => r.Name == name).RoutineId;
    }

    [Fact]
    public async Task GetExercisesAsync_ReturnsExercisesOrderedBySequence()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"ex-order-1-{id}@test.com", "Test1234!");
        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var routineId = await CreateRoutineWithReversedSequencesAsync(api, $"Ordering Test {id}");

        var exercises = await api.GetExercisesAsync(routineId);

        Assert.Equal(3, exercises.Count);
        Assert.Equal(1, exercises[0].Sequence);
        Assert.Equal(2, exercises[1].Sequence);
        Assert.Equal(3, exercises[2].Sequence);
        Assert.Equal("Exercise A", exercises[0].Name);
        Assert.Equal("Exercise B", exercises[1].Name);
        Assert.Equal("Exercise C", exercises[2].Name);
    }

    [Fact]
    public async Task GetRoutineAsync_ReturnsExercisesOrderedBySequence()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"ex-order-2-{id}@test.com", "Test1234!");
        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var routineId = await CreateRoutineWithReversedSequencesAsync(api, $"Ordering Test {id}");

        var routine = await api.GetRoutineAsync(routineId);

        Assert.NotNull(routine);
        Assert.Equal(3, routine.Exercises.Count);
        Assert.Equal(1, routine.Exercises[0].Sequence);
        Assert.Equal(2, routine.Exercises[1].Sequence);
        Assert.Equal(3, routine.Exercises[2].Sequence);
        Assert.Equal("Exercise A", routine.Exercises[0].Name);
        Assert.Equal("Exercise B", routine.Exercises[1].Name);
        Assert.Equal("Exercise C", routine.Exercises[2].Name);
    }

    [Fact]
    public async Task GetStartWorkoutDtoAsync_ReturnsExercisesOrderedBySequence()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"ex-order-3-{id}@test.com", "Test1234!");
        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var routineId = await CreateRoutineWithReversedSequencesAsync(api, $"Ordering Test {id}");

        var dto = await api.GetStartWorkoutDtoAsync(routineId);

        Assert.NotNull(dto);
        Assert.NotNull(dto.WorkoutExercises);
        Assert.Equal(3, dto.WorkoutExercises.Count);
        Assert.Equal(1, dto.WorkoutExercises[0].Sequence);
        Assert.Equal(2, dto.WorkoutExercises[1].Sequence);
        Assert.Equal(3, dto.WorkoutExercises[2].Sequence);
        Assert.Equal("Exercise A", dto.WorkoutExercises[0].ExerciseName);
        Assert.Equal("Exercise B", dto.WorkoutExercises[1].ExerciseName);
        Assert.Equal("Exercise C", dto.WorkoutExercises[2].ExerciseName);
    }
}
