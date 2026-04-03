namespace WhatYouDid.Shared;

public interface IWorkoutService
{
    Task<WorkoutDto?> GetStartWorkoutDtoAsync(int routineId);
    Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(Guid workoutId);
    Task<bool> SaveWorkoutAsync(WorkoutDto workout);
    Task<bool> UpdateWorkoutExerciseAsync(Guid workoutId, WorkoutExerciseDto exercise);
    Task<bool> DeleteWorkoutAsync(Guid workoutId);
    Task<int> GetWorkoutsCountAsync(string? search = null);
    Task<List<WorkoutListItemDto>> GetWorkoutsAsync(int startIndex, int count, string? search = null);
}
