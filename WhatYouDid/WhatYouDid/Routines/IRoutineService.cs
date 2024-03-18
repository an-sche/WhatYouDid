using WhatYouDid.Data;

namespace WhatYouDid.Routines;

public interface IRoutineService
{
    IQueryable<Routine> GetUserRoutinesQueryable(ApplicationUser user);
    IQueryable<Workout> GetUserWorkoutsQueryable(ApplicationUser user);
    Task<List<Routine>> GetRoutinesAsync();
    Task<Routine?> GetRoutineAsync(int routineId);
    Task<Routine> AddRoutineAsync(Routine routine);
    Task<Routine> UpdateRoutineAsync(Routine routine);
    void DeleteRoutineAsync(int routineId);
	IQueryable<Exercise> GetExercises(int routineId);
    Task<WorkoutDto?> GetStartWorkoutDtoAsync(string user, int routineId);
	Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(string user, int workoutId);

	Task<bool> SaveWorkoutAsync(WorkoutDto workout);
}
