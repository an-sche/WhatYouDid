using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhatYouDid.Services;

namespace WhatYouDid.Data;
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantService tenantService
) : IdentityDbContext<ApplicationUser>(options)
{
    private readonly string _tenant = tenantService.Tenant;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Workout>()
            .HasQueryFilter(w => w.ApplicationUserId == _tenant && !w.IsDeleted)
            .HasIndex(w => new { w.ApplicationUserId, w.StartTime })
            .IsDescending(false, true)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Workouts_User_StartTime");

        builder.Entity<Routine>()
            .HasQueryFilter(r => r.CreateUserId == _tenant || r.IsPublic);

        builder.Entity<Exercise>()
            .HasQueryFilter(w => w.ApplicationUserId == _tenant || w.Routine!.IsPublic);

        builder.Entity<WorkoutExercise>()
            .HasQueryFilter(we => we.Workout.ApplicationUserId == _tenant);

        base.OnModelCreating(builder);
    }

    public DbSet<Workout> Workouts { get; set; }

    public DbSet<Routine> Routines { get; set; }

    public DbSet<Exercise> Exercises { get; set; }

    public DbSet<WorkoutExercise> WorkoutExercises { get; set; }
}
