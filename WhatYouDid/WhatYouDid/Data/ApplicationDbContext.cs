using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhatYouDid.Services;

namespace WhatYouDid.Data;
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantService tenantService
) : IdentityDbContext<ApplicationUser>(options)
{
    private string Tenant => tenantService.Tenant;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Workout>()
            .HasQueryFilter(w => w.ApplicationUserId == Tenant && !w.IsDeleted)
            .HasIndex(w => new { w.ApplicationUserId, w.StartTime })
            .IsDescending(false, true)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Workouts_User_StartTime");

        builder.Entity<Routine>()
            .HasQueryFilter(r => r.CreateUserId == Tenant || r.IsPublic);

        builder.Entity<Exercise>()
            .HasQueryFilter(w => w.Routine!.CreateUserId == Tenant || w.Routine!.IsPublic);

        builder.Entity<WorkoutExercise>()
            .HasQueryFilter(we => we.Workout.ApplicationUserId == Tenant && !we.Workout.IsDeleted);

        builder.Entity<WorkoutExerciseSet>()
            .HasQueryFilter(wes => wes.WorkoutExercise.Workout.ApplicationUserId == Tenant && !wes.WorkoutExercise.Workout.IsDeleted);

        base.OnModelCreating(builder);
    }

    public DbSet<Workout> Workouts { get; set; }

    public DbSet<Routine> Routines { get; set; }

    public DbSet<Exercise> Exercises { get; set; }

    public DbSet<WorkoutExercise> WorkoutExercises { get; set; }

    public DbSet<WorkoutExerciseSet> WorkoutExerciseSets { get; set; }
}
