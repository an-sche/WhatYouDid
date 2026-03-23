using WhatYouDid.Shared;

namespace WhatYouDid.Tests.TenantIsolation;

[Collection("Database")]
public class RoutineTenantTests(DatabaseFixture fixture)
{
    private CreateRoutineDto BuildPrivateRoutine(string name) => new()
    {
        Name = name,
        Exercises =
        [
            new CreateExerciseDto
            {
                Name = "Curl",
                Sequence = 1,
                Sets = 3,
                HasReps = true,
                HasWeight = true,
                HasDuration = false
            }
        ]
    };

    [Fact]
    public async Task UserA_PrivateRoutine_NotVisibleTo_UserB()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await fixture.CreateUserAsync($"routine-a-{id}@test.com", "Test1234!");
        var userB = await fixture.CreateUserAsync($"routine-b-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        var api = fixture.CreateApiForTenant(tenantService);

        tenantService.SetTenant(userA.Id);
        await api.AddRoutineAsync(BuildPrivateRoutine($"Private Routine {id}"));

        tenantService.SetTenant(userB.Id);
        var routines = await api.GetUserRoutinesAsync();

        Assert.DoesNotContain(routines, r => r.Name == $"Private Routine {id}");
    }

    [Fact]
    public async Task UserA_PrivateRoutine_VisibleTo_UserA()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await fixture.CreateUserAsync($"routine-own-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        tenantService.SetTenant(userA.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        await api.AddRoutineAsync(BuildPrivateRoutine($"My Routine {id}"));

        var routines = await api.GetUserRoutinesAsync();

        Assert.Contains(routines, r => r.Name == $"My Routine {id}");
    }

    [Fact]
    public async Task UserA_PublicRoutine_VisibleTo_UserB()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await fixture.CreateUserAsync($"routine-pub-a-{id}@test.com", "Test1234!");
        var userB = await fixture.CreateUserAsync($"routine-pub-b-{id}@test.com", "Test1234!");

        // Insert a public routine directly via DbContext (API doesn't expose IsPublic flag)
        using (var db = fixture.CreateDbContextForTenant(userA.Id))
        {
            db.Routines.Add(new WhatYouDid.Data.Routine
            {
                Name = $"Public Routine {id}",
                CreateUserId = userA.Id,
                IsPublic = true,
                Exercises =
                [
                    new WhatYouDid.Data.Exercise
                    {
                        Name = "Pull Up",
                        Sequence = 1,
                        Sets = 3,
                        ApplicationUserId = userA.Id
                    }
                ]
            });
            await db.SaveChangesAsync();
        }

        var tenantService = new TestTenantService();
        tenantService.SetTenant(userB.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var routines = await api.GetUserRoutinesAsync();

        Assert.Contains(routines, r => r.Name == $"Public Routine {id}");
    }

    [Fact]
    public async Task UserA_PrivateRoutineExercises_NotVisibleTo_UserB()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await fixture.CreateUserAsync($"exercise-a-{id}@test.com", "Test1234!");
        var userB = await fixture.CreateUserAsync($"exercise-b-{id}@test.com", "Test1234!");

        var tenantService = new TestTenantService();
        var api = fixture.CreateApiForTenant(tenantService);

        tenantService.SetTenant(userA.Id);
        await api.AddRoutineAsync(BuildPrivateRoutine($"Arm Day {id}"));
        var userARoutines = await api.GetUserRoutinesAsync();
        var routine = userARoutines.First(r => r.Name == $"Arm Day {id}");

        tenantService.SetTenant(userB.Id);
        var exercises = await api.GetExercisesAsync(routine.RoutineId);

        Assert.Empty(exercises);
    }

    [Fact]
    public async Task UserA_PublicRoutineExercises_VisibleTo_UserB()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var userA = await fixture.CreateUserAsync($"pub-exercise-a-{id}@test.com", "Test1234!");
        var userB = await fixture.CreateUserAsync($"pub-exercise-b-{id}@test.com", "Test1234!");

        int routineId;
        using (var db = fixture.CreateDbContextForTenant(userA.Id))
        {
            var routine = new WhatYouDid.Data.Routine
            {
                Name = $"Public Arm Day {id}",
                CreateUserId = userA.Id,
                IsPublic = true,
                Exercises =
                [
                    new WhatYouDid.Data.Exercise
                    {
                        Name = "Bicep Curl",
                        Sequence = 1,
                        Sets = 3,
                        ApplicationUserId = userA.Id
                    }
                ]
            };
            db.Routines.Add(routine);
            await db.SaveChangesAsync();
            routineId = routine.RoutineId;
        }

        var tenantService = new TestTenantService();
        tenantService.SetTenant(userB.Id);
        var api = fixture.CreateApiForTenant(tenantService);

        var exercises = await api.GetExercisesAsync(routineId);

        Assert.NotEmpty(exercises);
        Assert.Contains(exercises, e => e.Name == "Bicep Curl");
    }
}
