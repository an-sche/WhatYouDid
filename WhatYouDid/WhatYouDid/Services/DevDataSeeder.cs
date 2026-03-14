using Microsoft.AspNetCore.Identity;
using WhatYouDid.Data;

namespace WhatYouDid.Services;

internal sealed class DevDataSeeder(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    ILogger<DevDataSeeder> logger
) : IHostedService
{
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

            var user = await userManager.FindByEmailAsync(TestEmail);
            if (user is not null)
            {
                logger.LogInformation("Dev data already seeded, skipping.");
                return;
            }

            user = new ApplicationUser { UserName = TestEmail, Email = TestEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, TestPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create test user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Created test user {Email}.", TestEmail);

            var pushDay = new Routine
            {
                Name = "Push Day",
                CreateUserId = user.Id,
                IsPublic = false,
                Exercises =
                [
                    new Exercise { Name = "Bench Press",    Sequence = 1, Sets = 4, HasReps = true, HasWeight = true, ApplicationUserId = user.Id },
                    new Exercise { Name = "Overhead Press", Sequence = 2, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = user.Id },
                    new Exercise { Name = "Tricep Pushdown",Sequence = 3, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = user.Id },
                    new Exercise { Name = "Lateral Raise",  Sequence = 4, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = user.Id },
                ]
            };

            var pullDay = new Routine
            {
                Name = "Pull Day",
                CreateUserId = user.Id,
                IsPublic = false,
                Exercises =
                [
                    new Exercise { Name = "Deadlift",      Sequence = 1, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = user.Id },
                    new Exercise { Name = "Barbell Row",   Sequence = 2, Sets = 4, HasReps = true, HasWeight = true, ApplicationUserId = user.Id },
                    new Exercise { Name = "Pull-Up",       Sequence = 3, Sets = 3, HasReps = true,                   ApplicationUserId = user.Id },
                    new Exercise { Name = "Bicep Curl",    Sequence = 4, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = user.Id },
                ]
            };

            var legDay = new Routine
            {
                Name = "Leg Day",
                CreateUserId = user.Id,
                IsPublic = false,
                Exercises =
                [
                    new Exercise { Name = "Squat",          Sequence = 1, Sets = 4, HasReps = true, HasWeight = true, ApplicationUserId = user.Id },
                    new Exercise { Name = "Romanian Deadlift", Sequence = 2, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = user.Id },
                    new Exercise { Name = "Leg Press",      Sequence = 3, Sets = 3, HasReps = true, HasWeight = true, ApplicationUserId = user.Id },
                    new Exercise { Name = "Calf Raise",     Sequence = 4, Sets = 4, HasReps = true, HasWeight = true, ApplicationUserId = user.Id },
                ]
            };

            db.Routines.AddRange(pushDay, pullDay, legDay);
            await db.SaveChangesAsync(cancellationToken);

            // Seed 4 weeks of past workouts (push/pull/leg rotating)
            var routines = new[] { pushDay, pullDay, legDay };
            var now = DateTime.Now;
            var workoutDay = now.AddDays(-27);
            var routineIndex = 0;

            while (workoutDay < now.AddDays(-1))
            {
                var routine = routines[routineIndex % routines.Length];
                var start = workoutDay.Date.AddHours(18).AddMinutes(Random.Shared.Next(0, 30));
                var durationMinutes = Random.Shared.Next(45, 75);

                var workout = new Workout
                {
                    WorkoutId = Guid.NewGuid(),
                    ApplicationUserId = user.Id,
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
                    ApplicationUserId = user.Id,
                    Reps = e.HasReps ? Enumerable.Range(0, e.Sets).Select(_ => (int?)Random.Shared.Next(6, 13)).ToList() : null,
                    Weights = e.HasWeight ? Enumerable.Range(0, e.Sets).Select(_ => (int?)(Random.Shared.Next(4, 12) * 5)).ToList() : null,
                    Durations = null,
                }).ToList();

                db.Workouts.Add(workout);
                db.WorkoutExercises.AddRange(workout.WorkoutExercise);

                routineIndex++;
                workoutDay = workoutDay.AddDays(Random.Shared.Next(1, 3));
            }

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Dev data seeding complete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding dev data.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
