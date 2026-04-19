namespace WhatYouDid.Shared;

public interface IRoutineService
{
    Task<List<RoutineDto>> GetUserRoutinesAsync(bool performedOnly = false);
    Task<List<ExerciseDto>> GetExercisesAsync(int routineId);
    Task<RoutineDetailDto?> GetRoutineAsync(int routineId);
    Task<bool> AddRoutineAsync(CreateRoutineDto routine);
}
