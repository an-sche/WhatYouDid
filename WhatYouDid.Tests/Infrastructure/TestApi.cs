using WhatYouDid.Services;
using WhatYouDid.Shared;

namespace WhatYouDid.Tests.Infrastructure;

/// <summary>
/// Combines RoutineService and WorkoutService for use in integration tests
/// that need to set up routines and record workouts in the same test.
/// </summary>
public class TestApi(RoutineService routineService, WorkoutService workoutService)
    : IRoutineService, IWorkoutService
{
    public Task<bool> AddRoutineAsync(CreateRoutineDto routine) => routineService.AddRoutineAsync(routine);
    public Task<List<ExerciseDto>> GetExercisesAsync(int routineId) => routineService.GetExercisesAsync(routineId);
    public Task<RoutineDetailDto?> GetRoutineAsync(int routineId) => routineService.GetRoutineAsync(routineId);
    public Task<List<RoutineDto>> GetUserRoutinesAsync() => routineService.GetUserRoutinesAsync();

    public Task<WorkoutDto?> GetStartWorkoutDtoAsync(int routineId) => workoutService.GetStartWorkoutDtoAsync(routineId);
    public Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(Guid workoutId) => workoutService.GetCompletedWorkoutDtoAsync(workoutId);
    public Task<bool> SaveWorkoutAsync(WorkoutDto workout) => workoutService.SaveWorkoutAsync(workout);
    public Task<bool> UpdateWorkoutExerciseAsync(Guid workoutId, WorkoutExerciseDto exercise) => workoutService.UpdateWorkoutExerciseAsync(workoutId, exercise);
    public Task<bool> DeleteWorkoutAsync(Guid workoutId) => workoutService.DeleteWorkoutAsync(workoutId);
    public Task<PagedList<WorkoutListItemDto>> GetWorkoutsAsync(int page, int pageSize, string? search = null) => workoutService.GetWorkoutsAsync(page, pageSize, search);
}
