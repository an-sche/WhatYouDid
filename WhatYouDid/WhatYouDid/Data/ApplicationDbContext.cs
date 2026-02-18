using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhatYouDid.Services;

namespace WhatYouDid.Data;
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ITenantService tenantService
) : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Workout>()
            .HasQueryFilter(w => w.ApplicationUserId == tenantService.Tenant);

        builder.Entity<Routine>()
            .HasQueryFilter(r => r.CreateUserId == tenantService.Tenant || r.IsPublic);

        base.OnModelCreating(builder);
    }

    public DbSet<Workout> Workouts { get; set; }

    public DbSet<Routine> Routines { get; set; }

    public DbSet<Exercise> Exercises { get; set; }

    public DbSet<WorkoutExercise> WorkoutExercises { get; set; }
}
