using Microsoft.AspNetCore.Identity;
using WhatYouDid.Data;

namespace WhatYouDid.Services;

internal sealed class DevDataSeeder(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    ILogger<DevDataSeeder> logger
) : IHostedService
{
    private const string AdminEmail = "admin@test.com";
    private const string AdminPassword = "Admin1234!";
    private const string TestEmail = "test@test.com";
    private const string TestPassword = "Test1234!";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return;

        try
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var adminUser = await userManager.FindByEmailAsync(AdminEmail);
            if (adminUser is not null)
            {
                logger.LogInformation("Dev data already seeded, skipping.");
                return;
            }

            // --- Admin user ---
            adminUser = new ApplicationUser { UserName = AdminEmail, Email = AdminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(adminUser, AdminPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
            logger.LogInformation("Created admin user {Email}.", AdminEmail);

            // --- Test user ---
            var testUser = new ApplicationUser { UserName = TestEmail, Email = TestEmail, EmailConfirmed = true };
            result = await userManager.CreateAsync(testUser, TestPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create test user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
            logger.LogInformation("Created test user {Email}.", TestEmail);

            // --- Public routine (owned by admin) ---
            var fullBodyRoutine = new Routine
            {
                Name = "Full Body Starter",
                CreateUserId = adminUser.Id,
                IsPublic = true,
                Exercises =
                [
                    new Exercise { Name = "Squat",       Sequence = 1, Sets = 3, HasReps = true, HasWeight = true },
                    new Exercise { Name = "Bench Press", Sequence = 2, Sets = 3, HasReps = true, HasWeight = true },
                    new Exercise { Name = "Deadlift",    Sequence = 3, Sets = 3, HasReps = true, HasWeight = true },
                    new Exercise { Name = "Pull-Up",     Sequence = 4, Sets = 3, HasReps = true                   },
                ]
            };

            // --- Admin private routines ---
            var adminPush = new Routine
            {
                Name = "Push Day",
                CreateUserId = adminUser.Id,
                IsPublic = false,
                Exercises =
                [
                    new Exercise { Name = "Bench Press",     Sequence = 1, Sets = 4, HasReps = true, HasWeight = true },
                    new Exercise { Name = "Overhead Press",  Sequence = 2, Sets = 3, HasReps = true, HasWeight = true },
                    new Exercise { Name = "Tricep Pushdown", Sequence = 3, Sets = 3, HasReps = true, HasWeight = true },
                    new Exercise { Name = "Lateral Raise",   Sequence = 4, Sets = 3, HasReps = true, HasWeight = true },
                ]
            };

            var adminPull = new Routine
            {
                Name = "Pull Day",
                CreateUserId = adminUser.Id,
                IsPublic = false,
                Exercises =
                [
                    new Exercise { Name = "Deadlift",    Sequence = 1, Sets = 3, HasReps = true, HasWeight = true },
                    new Exercise { Name = "Barbell Row", Sequence = 2, Sets = 4, HasReps = true, HasWeight = true },
                    new Exercise { Name = "Pull-Up",     Sequence = 3, Sets = 3, HasReps = true                   },
                    new Exercise { Name = "Bicep Curl",  Sequence = 4, Sets = 3, HasReps = true, HasWeight = true },
                ]
            };

            // --- Test user private routines ---
            var testLeg = new Routine
            {
                Name = "Leg Day",
                CreateUserId = testUser.Id,
                IsPublic = false,
                Exercises =
                [
                    new Exercise { Name = "Squat",             Sequence = 1, Sets = 4, HasReps = true, HasWeight = true },
                    new Exercise { Name = "Romanian Deadlift", Sequence = 2, Sets = 3, HasReps = true, HasWeight = true },
                    new Exercise { Name = "Leg Press",         Sequence = 3, Sets = 3, HasReps = true, HasWeight = true },
                    new Exercise { Name = "Calf Raise",        Sequence = 4, Sets = 4, HasReps = true, HasWeight = true },
                ]
            };

            var testCardio = new Routine
            {
                Name = "Cardio & Core",
                CreateUserId = testUser.Id,
                IsPublic = false,
                Exercises =
                [
                    new Exercise { Name = "Treadmill Run", Sequence = 1, Sets = 1, HasDuration = true },
                    new Exercise { Name = "Plank",         Sequence = 2, Sets = 3, HasDuration = true },
                    new Exercise { Name = "Sit-Up",        Sequence = 3, Sets = 3, HasReps = true     },
                    new Exercise { Name = "Leg Raise",     Sequence = 4, Sets = 3, HasReps = true     },
                ]
            };

            db.Routines.AddRange(fullBodyRoutine, adminPush, adminPull, testLeg, testCardio);
            await db.SaveChangesAsync(cancellationToken);

            // --- Seed workout history ---
            var now = DateTime.Now;

            SeedWorkoutHistory(db, adminUser.Id, [adminPush, adminPull, fullBodyRoutine], startDaysAgo: 27, now);
            SeedWorkoutHistory(db, testUser.Id, [testLeg, testCardio, fullBodyRoutine], startDaysAgo: 27, now);

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Dev data seeding complete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding dev data.");
            throw;
        }
    }

    private static void SeedWorkoutHistory(ApplicationDbContext db, string userId, Routine[] routines, int startDaysAgo, DateTime now)
    {
        var workoutDay = now.AddDays(-startDaysAgo);
        var routineIndex = 0;

        while (workoutDay < now.AddDays(-1))
        {
            var routine = routines[routineIndex % routines.Length];
            var start = workoutDay.Date.AddHours(18).AddMinutes(Random.Shared.Next(0, 30));
            var durationMinutes = Random.Shared.Next(45, 75);

            var workout = new Workout
            {
                WorkoutId = Guid.NewGuid(),
                ApplicationUserId = userId,
                RoutineId = routine.RoutineId,
                RoutineName = routine.Name,
                StartTime = start,
                EndTime = start.AddMinutes(durationMinutes),
            };

            workout.WorkoutExercise = routine.Exercises.Select(e => new WorkoutExercise
            {
                WorkoutId = workout.WorkoutId,
                ExerciseId = e.ExerciseId,
                ExerciseName = e.Name,
                Sets = Enumerable.Range(1, e.Sets).Select(setNum => new WorkoutExerciseSet {
                    SetNumber = setNum,
                    Reps      = e.HasReps     ? Random.Shared.Next(6, 13)          : null,
                    Weight    = e.HasWeight   ? (Random.Shared.Next(4, 12) * 5)   : null,
                    Duration  = e.HasDuration ? Random.Shared.Next(30, 120)        : null,
                }).ToList(),
            }).ToList();

            db.Workouts.Add(workout);

            routineIndex++;
            workoutDay = workoutDay.AddDays(Random.Shared.Next(1, 3));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
