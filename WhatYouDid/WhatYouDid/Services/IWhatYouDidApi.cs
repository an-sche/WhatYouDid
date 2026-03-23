using WhatYouDid.Data;
using WhatYouDid.Shared;

namespace WhatYouDid.Services;

public interface IWhatYouDidApi
{
    // All methods assume the current tenant has been resolved in the scoped
    // DbContext (via ITenantService). No caller should pass user id.

    Task<List<Routine>> GetUserRoutinesAsync();
    Task<List<Exercise>> GetExercisesAsync(int routineId);
    Task<int> GetWorkoutsCountAsync(string? search = null);
    Task<List<Workout>> GetWorkoutsAsync(int startIndex, int count, string? search = null);

    Task<List<Routine>> GetRoutinesAsync();
    Task<Routine?> GetRoutineAsync(int routineId);
    Task<bool> AddRoutineAsync(CreateRoutineDto routine);
    Task<Routine> UpdateRoutineAsync(Routine routine);
    Task DeleteRoutineAsync(int routineId);
    Task<WorkoutDto?> GetStartWorkoutDtoAsync(int routineId);
    Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(Guid workoutId);

    Task<bool> SaveWorkoutAsync(WorkoutDto workout);
    Task<bool> UpdateWorkoutExerciseAsync(Guid workoutId, WorkoutExerciseDto exercise);
    Task<bool> DeleteWorkoutAsync(Guid workoutId);
}
