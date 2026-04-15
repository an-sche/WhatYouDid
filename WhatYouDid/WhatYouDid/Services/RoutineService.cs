using Microsoft.EntityFrameworkCore;
using WhatYouDid.Data;
using WhatYouDid.Shared;

namespace WhatYouDid.Services;

public class RoutineService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITenantService tenantService
) : IRoutineService
{
    public async Task<bool> AddRoutineAsync(CreateRoutineDto dto)
    {
        if (string.IsNullOrEmpty(tenantService.Tenant))
            throw new Exception("Could not resolve tenant");

        using var db = await dbFactory.CreateDbContextAsync();

        var routine = new Routine
        {
            Name = dto.Name,
            CreateUserId = tenantService.Tenant,
            Exercises = dto.Exercises.Select(e => new Exercise
            {
                Name = e.Name,
                Description = e.Description,
                Sequence = e.Sequence,
                Sets = e.Sets,
                HasReps = e.HasReps,
                HasWeight = e.HasWeight,
                HasDuration = e.HasDuration,
            }).ToList()
        };

        await db.Routines.AddAsync(routine);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ExerciseDto>> GetExercisesAsync(int routineId)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        return await db.Exercises
            .Where(x => x.RoutineId == routineId)
            .OrderBy(e => e.Sequence)
            .Select(e => new ExerciseDto
            {
                ExerciseId = e.ExerciseId,
                Name = e.Name,
                Description = e.Description,
                Sequence = e.Sequence,
                Sets = e.Sets,
                HasReps = e.HasReps,
                HasWeight = e.HasWeight,
                HasDuration = e.HasDuration,
            })
            .ToListAsync();
    }

    public async Task<RoutineDetailDto?> GetRoutineAsync(int routineId)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        return await db.Routines
            .Where(x => x.RoutineId == routineId)
            .Select(r => new RoutineDetailDto
            {
                RoutineId = r.RoutineId,
                Name = r.Name,
                Exercises = r.Exercises.OrderBy(e => e.Sequence).Select(e => new ExerciseDto
                {
                    ExerciseId = e.ExerciseId,
                    Name = e.Name,
                    Description = e.Description,
                    Sequence = e.Sequence,
                    Sets = e.Sets,
                    HasReps = e.HasReps,
                    HasWeight = e.HasWeight,
                    HasDuration = e.HasDuration,
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<RoutineDto>> GetUserRoutinesAsync()
    {
        using var db = await dbFactory.CreateDbContextAsync();
        return await db.Routines
            .OrderBy(x => x.Name)
            .Select(r => new RoutineDto { RoutineId = r.RoutineId, Name = r.Name })
            .ToListAsync();
    }
}
