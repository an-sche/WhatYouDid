using Microsoft.EntityFrameworkCore;
using WhatYouDid.Data;
using WhatYouDid.Shared;

namespace WhatYouDid.Services;

public class WhatYouDidApiDirectAccess(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITenantService tenantService
) : IWhatYouDidApi
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

    public async Task<List<Exercise>> GetExercisesAsync(int routineId)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        return await db.Exercises.Where(x => x.RoutineId == routineId).ToListAsync();
    }

    public async Task<Routine?> GetRoutineAsync(int routineId)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        return await db.Routines
            .Include(x => x.Exercises)
            .FirstOrDefaultAsync(x => x.RoutineId == routineId);
    }

    public async Task<List<Routine>> GetUserRoutinesAsync()
    {
        using var db = await dbFactory.CreateDbContextAsync();
        return await db.Routines.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<int> GetWorkoutsCountAsync(string? search = null)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var query = db.Workouts.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.RoutineName.Contains(search));
        return await query.Where(x => x.EndTime != null).CountAsync();
    }

    public async Task<List<Workout>> GetWorkoutsAsync(int startIndex, int count, string? search = null)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var query = db.Workouts.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.RoutineName.Contains(search));
        return await query
            .Where(x => x.EndTime != null)
            .OrderByDescending(x => x.EndTime)
            .Skip(startIndex)
            .Take(count)
            .ToListAsync();
    }

    public async Task<WorkoutDto?> GetStartWorkoutDtoAsync(int routineId)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var routine = await db.Routines.AsNoTracking()
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(r => r.RoutineId == routineId);

        if (routine is null) return null;

        var lastWorkoutId = await db.Workouts.AsNoTracking()
            .Where(w => w.RoutineId == routineId && w.EndTime != null)
            .OrderByDescending(w => w.EndTime)
            .Select(w => (Guid?)w.WorkoutId)
            .FirstOrDefaultAsync();

        var lastSets = lastWorkoutId is not null
            ? await db.WorkoutExerciseSets.AsNoTracking()
                .Where(s => s.WorkoutExercise.WorkoutId == lastWorkoutId)
                .Select(s => new
                {
                    s.WorkoutExercise.ExerciseId,
                    s.SetNumber,
                    s.Reps,
                    s.Weight,
                    s.Duration,
                    s.AlternateReps,
                    s.AlternateWeight,
                    s.AlternateDuration,
                    s.Note,
                })
                .ToListAsync()
            : [];

        var lastSetsByExerciseId = lastSets
            .GroupBy(s => s.ExerciseId ?? 0)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.SetNumber).ToList());

        return new WorkoutDto
        {
            WorkoutId = Guid.CreateVersion7(),
            RoutineId = routineId,
            RoutineName = routine.Name,
            WorkoutExercises = routine.Exercises.Select(exercise =>
            {
                lastSetsByExerciseId.TryGetValue(exercise.ExerciseId, out var orderedSets);

                return new WorkoutExerciseDto
                {
                    Sequence     = exercise.Sequence,
                    ExerciseId   = exercise.ExerciseId,
                    ExerciseName = exercise.Name,
                    Sets         = exercise.Sets,

                    HasReps      = exercise.HasReps,
                    HasDurations = exercise.HasDuration,
                    HasWeights   = exercise.HasWeight,

                    LastReps               = orderedSets?.Select(s => s.Reps).ToArray(),
                    LastWeights            = orderedSets?.Select(s => s.Weight).ToArray(),
                    LastDurations          = orderedSets?.Select(s => s.Duration).ToArray(),
                    LastAlternateReps      = orderedSets?.Select(s => s.AlternateReps).ToArray(),
                    LastAlternateWeights   = orderedSets?.Select(s => s.AlternateWeight).ToArray(),
                    LastAlternateDurations = orderedSets?.Select(s => s.AlternateDuration).ToArray(),
                    LastNotes              = orderedSets?.Select(s => s.Note).ToArray(),

                    Reps               = new int?[exercise.Sets],
                    Weights            = new int?[exercise.Sets],
                    Durations          = new int?[exercise.Sets],
                    AlternateReps      = new int?[exercise.Sets],
                    AlternateWeights   = new int?[exercise.Sets],
                    AlternateDurations = new int?[exercise.Sets],
                    Notes              = orderedSets?.Select(s => s.Note).ToArray() ?? new string?[exercise.Sets],
                };
            }).ToList()
        };
    }

    public async Task<bool> SaveWorkoutAsync(WorkoutDto workoutDto)
    {
        if (string.IsNullOrEmpty(tenantService.Tenant))
            throw new Exception("Could not resolve tenant");

        using var db = await dbFactory.CreateDbContextAsync();

        var workout = new Workout()
        {
            WorkoutId = workoutDto.WorkoutId,
            ApplicationUserId = tenantService.Tenant,
            RoutineId = workoutDto.RoutineId,
            RoutineName = workoutDto.RoutineName,
            StartTime = workoutDto.StartTime,
            EndTime = workoutDto.EndTime,
        };
        workout.EndTime ??= DateTimeOffset.Now;

        if (workout.EndTime < workout.StartTime)
            throw new InvalidOperationException("EndTime cannot be before StartTime.");

        var exercises = new List<WorkoutExercise>();

        if (workoutDto.WorkoutExercises is not null) {
            foreach (var exerciseDto in workoutDto.WorkoutExercises) {
                var exercise = new WorkoutExercise() {
                    Workout = workout,
                    ExerciseId = exerciseDto.ExerciseId,
                    ExerciseName = exerciseDto.ExerciseName,
                    Sets = Enumerable.Range(0, exerciseDto.Sets).Select(i => new WorkoutExerciseSet {
                        SetNumber         = i + 1,
                        Reps              = exerciseDto.HasReps      ? exerciseDto.Reps.ElementAtOrDefault(i)              : null,
                        Weight            = exerciseDto.HasWeights   ? exerciseDto.Weights.ElementAtOrDefault(i)           : null,
                        Duration          = exerciseDto.HasDurations ? exerciseDto.Durations.ElementAtOrDefault(i)         : null,
                        AlternateReps     = exerciseDto.HasReps      ? exerciseDto.AlternateReps.ElementAtOrDefault(i)     : null,
                        AlternateWeight   = exerciseDto.HasWeights   ? exerciseDto.AlternateWeights.ElementAtOrDefault(i)  : null,
                        AlternateDuration = exerciseDto.HasDurations ? exerciseDto.AlternateDurations.ElementAtOrDefault(i): null,
                        Note              = exerciseDto.Notes.ElementAtOrDefault(i),
                    }).ToList(),
                };
                exercises.Add(exercise);
            }
        }
        workout.WorkoutExercise = exercises;

        await db.Workouts.AddAsync(workout);
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateWorkoutExerciseAsync(Guid workoutId, WorkoutExerciseDto exerciseDto)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.WorkoutExercises
            .Include(we => we.Sets)
            .FirstOrDefaultAsync(x => x.WorkoutId == workoutId && x.ExerciseId == exerciseDto.ExerciseId);

        if (entity is null) return false;

        db.WorkoutExerciseSets.RemoveRange(entity.Sets);
        entity.Sets = Enumerable.Range(0, exerciseDto.Sets).Select(i => new WorkoutExerciseSet {
            SetNumber         = i + 1,
            Reps              = exerciseDto.HasReps      ? exerciseDto.Reps.ElementAtOrDefault(i)              : null,
            Weight            = exerciseDto.HasWeights   ? exerciseDto.Weights.ElementAtOrDefault(i)           : null,
            Duration          = exerciseDto.HasDurations ? exerciseDto.Durations.ElementAtOrDefault(i)         : null,
            AlternateReps     = exerciseDto.HasReps      ? exerciseDto.AlternateReps.ElementAtOrDefault(i)     : null,
            AlternateWeight   = exerciseDto.HasWeights   ? exerciseDto.AlternateWeights.ElementAtOrDefault(i)  : null,
            AlternateDuration = exerciseDto.HasDurations ? exerciseDto.AlternateDurations.ElementAtOrDefault(i): null,
            Note              = exerciseDto.Notes.ElementAtOrDefault(i),
        }).ToList();

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWorkoutAsync(Guid workoutId)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var workout = await db.Workouts.FirstOrDefaultAsync(w => w.WorkoutId == workoutId);
        if (workout is null) return false;

        workout.IsDeleted = true;
        workout.DeletedDt = DateTimeOffset.Now;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(Guid workoutId)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var weEntities = await (
            from we in db.WorkoutExercises.Include(x => x.Sets)
            where we.WorkoutId == workoutId
            join ex in db.Exercises.IgnoreQueryFilters()
                on we.ExerciseId equals ex.ExerciseId into exJoin
            from ex in exJoin.DefaultIfEmpty()
            select new { we, ex })
            .ToListAsync();

        var workoutExercises = weEntities.Select(row => new WorkoutExerciseDto
        {
            Sequence     = row.ex != null ? row.ex.Sequence : 0,
            ExerciseId   = row.we.ExerciseId ?? 0,
            ExerciseName = row.we.ExerciseName,
            Sets         = row.ex != null ? row.ex.Sets : 0,

            HasReps      = row.ex != null && row.ex.HasReps,
            HasWeights   = row.ex != null && row.ex.HasWeight,
            HasDurations = row.ex != null && row.ex.HasDuration,

            Reps               = row.we.Sets.OrderBy(s => s.SetNumber).Select(s => s.Reps).ToArray(),
            Weights            = row.we.Sets.OrderBy(s => s.SetNumber).Select(s => s.Weight).ToArray(),
            Durations          = row.we.Sets.OrderBy(s => s.SetNumber).Select(s => s.Duration).ToArray(),
            AlternateReps      = row.we.Sets.OrderBy(s => s.SetNumber).Select(s => s.AlternateReps).ToArray(),
            AlternateWeights   = row.we.Sets.OrderBy(s => s.SetNumber).Select(s => s.AlternateWeight).ToArray(),
            AlternateDurations = row.we.Sets.OrderBy(s => s.SetNumber).Select(s => s.AlternateDuration).ToArray(),
            Notes              = row.we.Sets.OrderBy(s => s.SetNumber).Select(s => s.Note).ToArray(),
        }).ToList();

        var workout = await db.Workouts
            .Where(x => x.WorkoutId == workoutId)
            .Select(x => new WorkoutDto()
            {
                WorkoutId = x.WorkoutId,
                RoutineName = x.RoutineName,
                RoutineId = x.RoutineId ?? 0,
            })
            .FirstOrDefaultAsync();

        if (workout is null)
            return null;

        workout.WorkoutExercises = workoutExercises;
        return workout;
    }
}
