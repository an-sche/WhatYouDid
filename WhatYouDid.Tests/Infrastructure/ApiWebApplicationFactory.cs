using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;

namespace WhatYouDid.Tests.Infrastructure;

/// <summary>
/// WebApplicationFactory that spins up the full ASP.NET Core host against a real SQL Server
/// container. Replaces Identity auth with <see cref="TestAuthHandler"/> so tests can
/// authenticate by passing an X-Test-UserId header.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private string _connectionString = null!;
    private ServiceProvider _userServiceProvider = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Override the connection string before services are registered
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection([
                new KeyValuePair<string, string?>(
                    "ConnectionStrings:DevelopmentConnection", _connectionString)
            ]);
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace DbContextFactory so requests hit the container, not the User Secrets dev DB
            var dbDescriptors = services
                .Where(d => d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>)
                         || d.ServiceType == typeof(ApplicationDbContext))
                .ToList();
            foreach (var d in dbDescriptors) services.Remove(d);
            services.AddDbContextFactory<ApplicationDbContext>(
                opts => opts.UseSqlServer(_connectionString), ServiceLifetime.Scoped);

            // Remove background seeders — internal types, matched by name
            foreach (var name in new[] { "DevDataSeeder", "AdminRoleSeeder" })
            {
                var d = services.FirstOrDefault(s => s.ImplementationType?.Name == name);
                if (d is not null) services.Remove(d);
            }

            // Replace Resend email sender with a fake that captures sent emails in memory
            services.RemoveAll<IEmailSender<ApplicationUser>>();
            services.AddSingleton<FakeEmailSender>();
            services.AddTransient<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<FakeEmailSender>());

            // Replace all Identity auth with a simple header-based test handler
            services.AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                opts.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                opts.DefaultForbidScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        // Run migrations before the host starts so hosted services don't hit a missing schema
        var tenantService = new TestTenantService();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new ApplicationDbContext(options, tenantService);
        await db.Database.MigrateAsync();

        // Build a dedicated ServiceProvider targeting _connectionString directly so
        // CreateUserAsync writes to the test container, not the dev DB from User Secrets.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ITenantService, TestTenantService>();
        services.AddDbContext<ApplicationDbContext>(opts => opts.UseSqlServer(_connectionString));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        _userServiceProvider = services.BuildServiceProvider();
    }

    public new async Task DisposeAsync()
    {
        await _userServiceProvider.DisposeAsync();
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>Creates a user via UserManager against the test container database.</summary>
    public Task<ApplicationUser> CreateUserAsync(string email, string password)
        => UserHelper.CreateUserAsync(_userServiceProvider, email, password);

    /// <summary>
    /// Inserts a routine with one exercise directly into the DB, scoped to <paramref name="userId"/>.
    /// Returns the new RoutineId.
    /// </summary>
    public async Task<int> CreateRoutineAsync(string userId, string routineName)
    {
        var tenantService = new TestTenantService();
        tenantService.SetTenant(userId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new ApplicationDbContext(options, tenantService);

        var routine = new Routine
        {
            Name = routineName,
            CreateUserId = userId,
            Exercises =
            [
                new Exercise
                {
                    Name = "Bench Press",
                    Sequence = 1,
                    Sets = 3,
                    HasReps = true,
                    HasWeight = true,
                }
            ]
        };
        db.Routines.Add(routine);
        await db.SaveChangesAsync();
        return routine.RoutineId;
    }

    /// <summary>
    /// Inserts a completed workout directly into the DB, scoped to <paramref name="userId"/>.
    /// Returns the WorkoutId.
    /// </summary>
    public async Task<Guid> SaveWorkoutAsync(string userId, int routineId, string routineName)
    {
        var tenantService = new TestTenantService();
        tenantService.SetTenant(userId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new ApplicationDbContext(options, tenantService);

        var workoutId = Guid.NewGuid();
        var workout = new Workout
        {
            WorkoutId = workoutId,
            ApplicationUserId = userId,
            RoutineId = routineId,
            RoutineName = routineName,
            StartTime = DateTimeOffset.Now.AddHours(-1),
            EndTime = DateTimeOffset.Now,
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();
        return workoutId;
    }

    /// <summary>
    /// Inserts a routine with <paramref name="exerciseCount"/> exercises and returns
    /// the RoutineId and the generated ExerciseIds. Useful for flow tests that need
    /// multi-exercise navigation or autofill seeding.
    /// </summary>
    public async Task<(int RoutineId, int[] ExerciseIds)> CreateRoutineWithExercisesAsync(
        string userId, string routineName, int exerciseCount = 1)
    {
        var tenantService = new TestTenantService();
        tenantService.SetTenant(userId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new ApplicationDbContext(options, tenantService);

        var exercises = Enumerable.Range(1, exerciseCount).Select(i => new Exercise
        {
            Name = i == 1 ? "Bench Press" : $"Exercise {i}",
            Sequence = i,
            Sets = 3,
            HasReps = true,
            HasWeight = true,
        }).ToList();

        var routine = new Routine
        {
            Name = routineName,
            CreateUserId = userId,
            Exercises = exercises
        };
        db.Routines.Add(routine);
        await db.SaveChangesAsync();

        return (routine.RoutineId, exercises.Select(e => e.ExerciseId).ToArray());
    }

    /// <summary>
    /// Inserts a completed workout with WorkoutExercise and set records so that the
    /// autofill history button appears on the workout page for <paramref name="exerciseId"/>.
    /// Returns the WorkoutId.
    /// </summary>
    public async Task<Guid> SaveWorkoutWithSetsAsync(
        string userId, int routineId, string routineName,
        int exerciseId, string exerciseName, int reps, int weight)
    {
        var tenantService = new TestTenantService();
        tenantService.SetTenant(userId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var db = new ApplicationDbContext(options, tenantService);

        var workoutId = Guid.NewGuid();
        var workout = new Workout
        {
            WorkoutId = workoutId,
            ApplicationUserId = userId,
            RoutineId = routineId,
            RoutineName = routineName,
            StartTime = DateTimeOffset.Now.AddHours(-1),
            EndTime = DateTimeOffset.Now.AddMinutes(-1),
            WorkoutExercise =
            [
                new WorkoutExercise
                {
                    ExerciseId = exerciseId,
                    ExerciseName = exerciseName,
                    Sets =
                    [
                        new WorkoutExerciseSet { SetNumber = 1, Reps = reps, Weight = weight },
                        new WorkoutExerciseSet { SetNumber = 2, Reps = reps, Weight = weight },
                        new WorkoutExerciseSet { SetNumber = 3, Reps = reps, Weight = weight },
                    ]
                }
            ]
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();
        return workoutId;
    }

    /// <summary>
    /// Creates an HttpClient pre-loaded with the X-Test-UserId header so every
    /// request is authenticated as <paramref name="userId"/>.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string userId, string? roles = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        if (roles is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, roles);
        return client;
    }

}
