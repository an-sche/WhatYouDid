using WhatYouDid.Data;
using WhatYouDid.Shared;

namespace WhatYouDid.Services;

public interface IRoutineService
{
    Task<List<Routine>> GetUserRoutinesAsync();
    Task<List<Exercise>> GetExercisesAsync(int routineId);
    Task<Routine?> GetRoutineAsync(int routineId);
    Task<bool> AddRoutineAsync(CreateRoutineDto routine);
}
