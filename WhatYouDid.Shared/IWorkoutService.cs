namespace WhatYouDid.Shared;

public interface IWorkoutService
{
    Task<WorkoutDto?> GetStartWorkoutDtoAsync(int routineId);
    Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(Guid workoutId);
    Task<bool> SaveWorkoutAsync(WorkoutDto workout);
    Task<bool> UpdateWorkoutExerciseAsync(Guid workoutId, WorkoutExerciseDto exercise);
    Task<bool> DeleteWorkoutAsync(Guid workoutId);
    Task<PagedList<WorkoutListItemDto>> GetWorkoutsAsync(int page, int pageSize, string? search = null);
    Task<IEnumerable<WorkoutExportRowDto>> GetAllWorkoutsForExportAsync(int? year = null);
    Task<ExerciseHistoryDto?> GetExerciseHistoryAsync(int exerciseId);
}
