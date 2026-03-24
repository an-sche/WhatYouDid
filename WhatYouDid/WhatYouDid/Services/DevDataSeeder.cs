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
                    new Exercise { Name = "Squat",       Sequence = 1, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = adminUser.Id },
                    new Exercise { Name = "Bench Press", Sequence = 2, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = adminUser.Id },
                    new Exercise { Name = "Deadlift",    Sequence = 3, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = adminUser.Id },
                    new Exercise { Name = "Pull-Up",     Sequence = 4, Sets = 3, HasReps = true,                   ApplicationUserId = adminUser.Id },
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
                    new Exercise { Name = "Bench Press",     Sequence = 1, Sets = 4, HasReps = true, HasWeight = true, ApplicationUserId = adminUser.Id },
                    new Exercise { Name = "Overhead Press",  Sequence = 2, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = adminUser.Id },
                    new Exercise { Name = "Tricep Pushdown", Sequence = 3, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = adminUser.Id },
                    new Exercise { Name = "Lateral Raise",   Sequence = 4, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = adminUser.Id },
                ]
            };

            var adminPull = new Routine
            {
                Name = "Pull Day",
                CreateUserId = adminUser.Id,
                IsPublic = false,
                Exercises =
                [
                    new Exercise { Name = "Deadlift",    Sequence = 1, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = adminUser.Id },
                    new Exercise { Name = "Barbell Row", Sequence = 2, Sets = 4, HasReps = true, HasWeight = true, ApplicationUserId = adminUser.Id },
                    new Exercise { Name = "Pull-Up",     Sequence = 3, Sets = 3, HasReps = true,                   ApplicationUserId = adminUser.Id },
                    new Exercise { Name = "Bicep Curl",  Sequence = 4, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = adminUser.Id },
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
                    new Exercise { Name = "Squat",             Sequence = 1, Sets = 4, HasReps = true, HasWeight = true, ApplicationUserId = testUser.Id },
                    new Exercise { Name = "Romanian Deadlift", Sequence = 2, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = testUser.Id },
                    new Exercise { Name = "Leg Press",         Sequence = 3, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = testUser.Id },
                    new Exercise { Name = "Calf Raise",        Sequence = 4, Sets = 4, HasReps = true, HasWeight = true, ApplicationUserId = testUser.Id },
                ]
            };

            var testCardio = new Routine
            {
                Name = "Cardio & Core",
                CreateUserId = testUser.Id,
                IsPublic = false,
                Exercises =
                [
                    new Exercise { Name = "Treadmill Run", Sequence = 1, Sets = 1, HasDuration = true,                   ApplicationUserId = testUser.Id },
                    new Exercise { Name = "Plank",         Sequence = 2, Sets = 3, HasDuration = true,                   ApplicationUserId = testUser.Id },
                    new Exercise { Name = "Sit-Up",        Sequence = 3, Sets = 3, HasReps = true,                       ApplicationUserId = testUser.Id },
                    new Exercise { Name = "Leg Raise",     Sequence = 4, Sets = 3, HasReps = true,                       ApplicationUserId = testUser.Id },
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
                Reps = e.HasReps ? Enumerable.Range(0, e.Sets).Select(_ => (int?)Random.Shared.Next(6, 13)).ToList() : null,
                Weights = e.HasWeight ? Enumerable.Range(0, e.Sets).Select(_ => (int?)(Random.Shared.Next(4, 12) * 5)).ToList() : null,
                Durations = e.HasDuration ? Enumerable.Range(0, e.Sets).Select(_ => (int?)Random.Shared.Next(30, 120)).ToList() : null,
            }).ToList();

            db.Workouts.Add(workout);
            db.WorkoutExercises.AddRange(workout.WorkoutExercise);

            routineIndex++;
            workoutDay = workoutDay.AddDays(Random.Shared.Next(1, 3));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
