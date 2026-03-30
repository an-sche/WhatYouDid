using WhatYouDid.Data;
using WhatYouDid.Shared;

namespace WhatYouDid.Services;

public interface IWorkoutService
{
    Task<WorkoutDto?> GetStartWorkoutDtoAsync(int routineId);
    Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(Guid workoutId);
    Task<bool> SaveWorkoutAsync(WorkoutDto workout);
    Task<bool> UpdateWorkoutExerciseAsync(Guid workoutId, WorkoutExerciseDto exercise);
    Task<bool> DeleteWorkoutAsync(Guid workoutId);
    Task<int> GetWorkoutsCountAsync(string? search = null);
    Task<List<Workout>> GetWorkoutsAsync(int startIndex, int count, string? search = null);
}
