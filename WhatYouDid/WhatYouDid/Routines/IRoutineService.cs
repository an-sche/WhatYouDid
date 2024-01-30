using WhatYouDid.Data;

namespace WhatYouDid.Routines;

public interface IRoutineService
{
    Task<List<Routine>> GetRoutinesAsync();
    IQueryable<Routine> GetRoutinesByUserAsync(ApplicationUser user);
    Task<Routine?> GetRoutineAsync(int routineId);
    Task<Routine> AddRoutineAsync(Routine routine);
    Task<Routine> UpdateRoutineAsync(Routine routine);
    void DeleteRoutineAsync(int routineId);
}
