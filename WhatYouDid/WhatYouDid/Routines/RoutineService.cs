using Microsoft.EntityFrameworkCore;
using WhatYouDid.Data;

namespace WhatYouDid.Routines;

public class RoutineService : IRoutineService
{
    private readonly ApplicationDbContext _db;
    public RoutineService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Routine> AddRoutineAsync(Routine routine)
    {
        var result = await _db.Routines.AddAsync(routine);
        await _db.SaveChangesAsync();
        return result.Entity;
    }

    public async void DeleteRoutineAsync(int routineId)
    {
        var result = await _db.Routines.FirstOrDefaultAsync(x => x.RoutineId == routineId);
        if (result is null)
            return;

        _db.Routines.Remove(result);
        await _db.SaveChangesAsync();
    }

	public IQueryable<Exercise> GetExercises(int routineId)
	{
        return _db.Exercises.Where(x => x.RoutineId == routineId);
	}

	public async Task<Routine?> GetRoutineAsync(int routineId)
    {
        var result = await _db.Routines
            .Include(x => x.Exercises)
            .FirstOrDefaultAsync(x => x.RoutineId == routineId);
        return result;
    }

    public async Task<List<Routine>> GetRoutinesAsync()
    {
        return await _db.Routines.ToListAsync();
    }

	public IQueryable<Routine> GetUserRoutinesQueryable(ApplicationUser user)
	{
        return _db.Routines.OrderBy(x => x.Name).Where(x => x.CreateUser == user);
	}

	public async Task<WorkoutDto?> GetWorkoutRoutineDtoAsync(string user, int routineId)
	{
        return await _db.Routines
            .Include(x => x.Exercises)
            .Where(x => x.CreateUserId == user && x.RoutineId == routineId)
			.Select(x => new WorkoutDto()
            {
                RoutineId = routineId,
                RoutineName = x.Name,
                ApplicationUserId = user,
                // TODO: Other fields here.
                WorkoutExercises = x.Exercises.Select(e => new WorkoutExerciseDto()
                {
                    Sequence = e.Sequence,
                    ExerciseName = e.Name,
                    Sets = e.Sets
                }).ToList()
            })
            .FirstOrDefaultAsync();
	}

	public Task<Routine> UpdateRoutineAsync(Routine routine)
    {
        throw new NotImplementedException();
    }
}
