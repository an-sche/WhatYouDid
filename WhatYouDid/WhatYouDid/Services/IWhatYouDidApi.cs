using WhatYouDid.Data;
using WhatYouDid.Shared;

namespace WhatYouDid.Services;

public interface IWhatYouDidApi
{
    // All methods assume the current tenant has been resolved in the scoped
    // DbContext (via ITenantService). No caller should pass user id.

    IQueryable<Routine> GetUserRoutinesQueryable();
    IQueryable<Workout> GetUserWorkoutsQueryable();
    IQueryable<Exercise> GetExercises(int routineId);

    Task<List<Routine>> GetRoutinesAsync();
    Task<Routine?> GetRoutineAsync(int routineId);
    Task<Routine> AddRoutineAsync(Routine routine);
    Task<Routine> UpdateRoutineAsync(Routine routine);
    void DeleteRoutineAsync(int routineId);
    Task<WorkoutDto?> GetStartWorkoutDtoAsync(int routineId);
    Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(Guid workoutId);

    Task<bool> SaveWorkoutAsync(WorkoutDto workout);
}
