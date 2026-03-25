using WhatYouDid.Shared;

namespace WhatYouDid.Tests.Routines;

[Collection("Database")]
public class RoutineCrudTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task AddRoutineAsync_CreatesRoutine_WithCorrectCreateUserId()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"routine-crud-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        await api.AddRoutineAsync(new CreateRoutineDto
        {
            Name = $"My Routine {id}",
            Exercises =
            [
                new CreateExerciseDto
                {
                    Name = "Push Up",
                    Sequence = 1,
                    Sets = 3,
                    HasReps = true,
                    HasWeight = false,
                    HasDuration = false
                }
            ]
        });

        var routines = await api.GetUserRoutinesAsync();
        var created = routines.FirstOrDefault(r => r.Name == $"My Routine {id}");

        Assert.NotNull(created);
        Assert.Equal(user.Id, created.CreateUserId);
    }

    [Fact]
    public async Task GetRoutineAsync_ReturnsRoutineWithExercises()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"routine-get-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        await api.AddRoutineAsync(new CreateRoutineDto
        {
            Name = $"Full Body {id}",
            Exercises =
            [
                new CreateExerciseDto
                {
                    Name = "Squat",
                    Sequence = 1,
                    Sets = 4,
                    HasReps = true,
                    HasWeight = true,
                    HasDuration = false
                },
                new CreateExerciseDto
                {
                    Name = "Bench Press",
                    Sequence = 2,
                    Sets = 3,
                    HasReps = true,
                    HasWeight = true,
                    HasDuration = false
                }
            ]
        });

        var routines = await api.GetUserRoutinesAsync();
        var routine = routines.First(r => r.Name == $"Full Body {id}");
        var fetched = await api.GetRoutineAsync(routine.RoutineId);

        Assert.NotNull(fetched);
        Assert.Equal($"Full Body {id}", fetched.Name);
        Assert.Equal(2, fetched.Exercises.Count);
        Assert.Contains(fetched.Exercises, e => e.Name == "Squat");
        Assert.Contains(fetched.Exercises, e => e.Name == "Bench Press");
    }

    [Fact]
    public async Task GetExercisesAsync_ReturnsExercisesForRoutine()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"routine-exs-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(user.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        await api.AddRoutineAsync(new CreateRoutineDto
        {
            Name = $"Two Ex Routine {id}",
            Exercises =
            [
                new CreateExerciseDto { Name = "Squat",      Sequence = 1, Sets = 4, HasReps = true,  HasWeight = true,  HasDuration = false },
                new CreateExerciseDto { Name = "Deadlift",   Sequence = 2, Sets = 3, HasReps = true,  HasWeight = true,  HasDuration = false },
            ]
        });

        var routines = await api.GetUserRoutinesAsync();
        var routineId = routines.First(r => r.Name == $"Two Ex Routine {id}").RoutineId;

        var exercises = await api.GetExercisesAsync(routineId);

        Assert.Equal(2, exercises.Count);
        Assert.Contains(exercises, e => e.Name == "Squat");
        Assert.Contains(exercises, e => e.Name == "Deadlift");
    }

}
