using WhatYouDid.Data;

namespace WhatYouDid.Services;

public interface IWhatYouDidApi
{
    // All methods assume the current tenant has been resolved in the scoped
    // DbContext (via ITenantService). No caller should pass user id.
    IQueryable<Routine> GetUserRoutinesQueryable();
    IQueryable<Workout> GetUserWorkoutsQueryable();
    Task<List<Routine>> GetRoutinesAsync();
    Task<Routine?> GetRoutineAsync(int routineId);
    Task<Routine> AddRoutineAsync(Routine routine);
    Task<Routine> UpdateRoutineAsync(Routine routine);
    void DeleteRoutineAsync(int routineId);
    IQueryable<Exercise> GetExercises(int routineId);
    Task<WorkoutDto?> GetStartWorkoutDtoAsync(int routineId);
    Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(int workoutId);

    Task<bool> SaveWorkoutAsync(WorkoutDto workout);
}
