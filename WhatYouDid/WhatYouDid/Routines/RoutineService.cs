using Microsoft.EntityFrameworkCore;
using WhatYouDid.Data;

namespace WhatYouDid.Routines;

public class RoutineService : IRoutineService
{
    private readonly ApplicationDbContext _db;
    public RoutineService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Routine> AddRoutineAsync(Routine routine)
    {
        var result = await _db.Routines.AddAsync(routine);
        await _db.SaveChangesAsync();
        return result.Entity;
    }

    public async void DeleteRoutineAsync(int routineId)
    {
        var result = await _db.Routines.FirstOrDefaultAsync(x => x.RoutineId == routineId);
        if (result is null)
            return;

        _db.Routines.Remove(result);
        await _db.SaveChangesAsync();
    }

	public IQueryable<Exercise> GetExercises(int routineId)
	{
        return _db.Exercises.Where(x => x.RoutineId == routineId);
	}

	public async Task<Routine?> GetRoutineAsync(int routineId)
    {
        var result = await _db.Routines
            .Include(x => x.Exercises)
            .FirstOrDefaultAsync(x => x.RoutineId == routineId);
        return result;
    }

    public async Task<List<Routine>> GetRoutinesAsync()
    {
        return await _db.Routines.ToListAsync();
    }

	public IQueryable<Routine> GetUserRoutinesQueryable(ApplicationUser user)
	{
        return _db.Routines.OrderBy(x => x.Name).Where(x => x.CreateUser == user);
	}

    public IQueryable<Workout> GetUserWorkoutsQueryable(ApplicationUser user)
    {
        return _db.Workouts.OrderByDescending(x => x.StartTime).Where(x => x.ApplicationUser == user);
    }

    public async Task<WorkoutDto?> GetStartWorkoutDtoAsync(string applicationUserId, int routineId)
	{
        return await _db.Routines
            .Include(x => x.Exercises)
            .Where(x => x.CreateUserId == applicationUserId && x.RoutineId == routineId)
            .Select(x => new WorkoutDto()
            {
                RoutineId = routineId,
                RoutineName = x.Name,
                ApplicationUserId = applicationUserId,
                WorkoutExercises = x.Exercises.Select(e => new WorkoutExerciseDto()
                {
                    Sequence = e.Sequence,
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.Name,
                    Sets = e.Sets,

                    HasReps = e.HasReps,
                    HasWeights = e.HasWeight,
                    HasDurations = e.HasDuration,

					// TODO: Get the "Last" workouts. (for the user to reference) (what you did!)

                    // For the user to fill in
					Reps = new int?[e.Sets],
                    Weights = new int?[e.Sets],
                    Durations = new int?[e.Sets],
                }).ToList()
            })
            .FirstOrDefaultAsync();
	}

    public async Task<bool> SaveWorkoutAsync(WorkoutDto workoutDto) {

        // convert that to my db object,
        var workout = new Workout() {
            ApplicationUserId = workoutDto.ApplicationUserId,
            RoutineId = workoutDto.RoutineId,
            RoutineName = workoutDto.RoutineName,
            StartTime = workoutDto.StartTime,
            EndTime = DateTime.Now,
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
        
        await _db.Workouts.AddAsync(workout);
        await _db.SaveChangesAsync();

        // add that to my db table (s) 
        return true;
    }

	public Task<Routine> UpdateRoutineAsync(Routine routine)
    {
        throw new NotImplementedException();
    }

	public async Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(string user, int workoutId)
	{
        return await _db.Workouts
            .Where(x => x.ApplicationUserId == user && x.WorkoutId == workoutId)
            .Select(x => new WorkoutDto() 
            {
                RoutineName = x.RoutineName,
                RoutineId = x.RoutineId ?? 0,
                ApplicationUserId = user,
			    WorkoutExercises = x.WorkoutExercise.Select(e => new WorkoutExerciseDto()
			    {
				    Sequence = e.Exercise.Sequence,
				    ExerciseId = e.ExerciseId ?? 0,
				    ExerciseName = e.ExerciseName,
				    Sets = e.Exercise.Sets,

				    HasReps = e.Exercise.HasReps,
				    HasWeights = e.Exercise.HasWeight,
				    HasDurations = e.Exercise.HasDuration,

				    // For the user to fill in
				    Reps = e.Reps.ToArray(),
				    Weights = e.Weights.ToArray(),
				    Durations = e.Durations.ToArray(),
			    }).ToList()
		    })
            .FirstOrDefaultAsync();
	}
}
