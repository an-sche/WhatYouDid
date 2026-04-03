namespace WhatYouDid.Shared;

public interface IRoutineService
{
    Task<List<RoutineDto>> GetUserRoutinesAsync();
    Task<List<ExerciseDto>> GetExercisesAsync(int routineId);
    Task<RoutineDetailDto?> GetRoutineAsync(int routineId);
    Task<bool> AddRoutineAsync(CreateRoutineDto routine);
}
