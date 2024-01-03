using WhatYouDid.Data;

namespace WhatYouDid.Routines;

public interface IRoutineService
{
    IQueryable<Routine> GetUserRoutinesQueryable(ApplicationUser user);
    Task<List<Routine>> GetRoutinesAsync();
    Task<Routine?> GetRoutineAsync(int routineId);
    Task<Routine> AddRoutineAsync(Routine routine);
    Task<Routine> UpdateRoutineAsync(Routine routine);
    void DeleteRoutineAsync(int routineId);
}
