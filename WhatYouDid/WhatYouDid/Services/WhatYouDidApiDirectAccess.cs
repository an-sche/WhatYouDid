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

        return await
                (from routine in db.Routines.AsNoTracking()
                where routine.RoutineId == routineId
                select new WorkoutDto()
                {
                    WorkoutId = Guid.CreateVersion7(),

                    RoutineId = routineId,
                    RoutineName = routine.Name,

                    WorkoutExercises =
                        (from exercise in routine.Exercises
                        join pastExercise in db.WorkoutExercises on exercise.ExerciseId equals pastExercise.ExerciseId into pastExercises
                        from pastExercise in pastExercises
                                                .OrderByDescending(x => x.Workout.StartTime).Take(1).DefaultIfEmpty()
                        select new WorkoutExerciseDto() {

                            Sequence = exercise.Sequence,
                            ExerciseId = exercise.ExerciseId,
                            ExerciseName = exercise.Name,
                            Sets = exercise.Sets,

                            LastDurations = (pastExercise != null && pastExercise.Durations != null) ? pastExercise.Durations.ToArray() : null,
                            LastReps = (pastExercise != null && pastExercise.Reps != null) ? pastExercise.Reps.ToArray() : null,
                            LastWeights = (pastExercise != null && pastExercise.Weights != null) ? pastExercise.Weights.ToArray() : null,

                            HasReps = exercise.HasReps,
                            HasDurations = exercise.HasDuration,
                            HasWeights = exercise.HasWeight,

                            Reps = new int?[exercise.Sets],
                            Durations = new int?[exercise.Sets],
                            Weights = new int?[exercise.Sets],
                        }).ToList()
                }).FirstOrDefaultAsync();
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
            EndTime = DateTimeOffset.Now,
        };

        var exercises = new List<WorkoutExercise>();

        if (workoutDto.WorkoutExercises is not null) {
            foreach (var exerciseDto in workoutDto.WorkoutExercises) {
                var exercise = new WorkoutExercise() {
                    Workout = workout,
                    ExerciseId = exerciseDto.ExerciseId,
                    ExerciseName = exerciseDto.ExerciseName,
                };
                if (exerciseDto.HasReps) {
                    exercise.Reps = exerciseDto.Reps.ToList();
                }
                if (exerciseDto.HasWeights) {
                    exercise.Weights = exerciseDto.Weights.ToList();
                }
                if (exerciseDto.HasDurations) {
                    exercise.Durations = exerciseDto.Durations.ToList();
                }
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
            .FirstOrDefaultAsync(x => x.WorkoutId == workoutId && x.ExerciseId == exerciseDto.ExerciseId);

        if (entity is null) return false;

        if (exerciseDto.HasReps)
            entity.Reps = exerciseDto.Reps.ToList();
        if (exerciseDto.HasWeights)
            entity.Weights = exerciseDto.Weights.ToList();
        if (exerciseDto.HasDurations)
            entity.Durations = exerciseDto.Durations.ToList();

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

        var workoutExercises = await (
            from we in db.WorkoutExercises
            where we.WorkoutId == workoutId
            join ex in db.Exercises.IgnoreQueryFilters()
                on we.ExerciseId equals ex.ExerciseId into exJoin
            from ex in exJoin.DefaultIfEmpty()
            select new WorkoutExerciseDto
            {
                Sequence = ex != null ? ex.Sequence : 0,
                ExerciseId = we.ExerciseId ?? 0,
                ExerciseName = we.ExerciseName,
                Sets = ex != null ? ex.Sets : 0,

                HasReps = ex != null && ex.HasReps,
                HasWeights = ex != null && ex.HasWeight,
                HasDurations = ex != null && ex.HasDuration,

                Reps = we.Reps.ToArray(),
                Weights = we.Weights.ToArray(),
                Durations = we.Durations.ToArray(),
            })
            .ToListAsync();

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
