using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using WhatYouDid.Data;
using WhatYouDid.Shared;

namespace WhatYouDid.Services;

public class DashboardService(
    ApplicationDbContext db)
    : IDashboardService
{
    private readonly ConcurrentDictionary<int, DashboardDto> cache = [];

    public async Task<DashboardDto> GetDashboardForUserAsync(int? parameterYear = null)
    {
        int year = parameterYear ?? 0; // Use 0 as a key for "all years"

        if (cache.TryGetValue(year, out var hit)) 
            return hit;
        return cache[year] = await ComputeDashboardAsync(year); 
    }

    private async Task<DashboardDto> ComputeDashboardAsync(int year)
    {
        // Compute top 5 most frequent workouts (by RoutineName) optionally filtered by year.
        var workoutsQuery = db.Workouts.AsNoTracking().AsQueryable();
        var dto = new DashboardDto();

        if (year != 0)
        {
            workoutsQuery = workoutsQuery.Where(w => w.StartTime.Year == year);
            dto.Year = year;
        }

        var top3workouts = await workoutsQuery
            .GroupBy(w => w.RoutineName)
            .Select(g => new { RoutineName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .Select(x => new WorkoutSummaryDto { RoutineName = x.RoutineName ?? "", Count = x.Count })
            .ToListAsync();

        dto.TopWorkouts = top3workouts;

        // Total Workout Duration
        dto.TotalWorkoutDuration = await workoutsQuery
            .Where(x => x.EndTime != null)
            .Where(x => x.EndTime > x.StartTime)
            .SumAsync(w => EF.Functions.DateDiffMinute(w.StartTime, w.EndTime));

        // Total Reps Completed (primary + alternate)
        dto.TotalReps = await db.WorkoutExercises
            .Where(we => year == 0 || we.Workout.StartTime.Year == year)
            .SelectMany(we => we.Sets)
            .SumAsync(s => (int?)(s.Reps ?? 0) + (s.AlternateReps ?? 0)) ?? 0;

        dto.TotalWorkouts = await workoutsQuery.CountAsync();

        return dto;
    }

    public async Task<IReadOnlyList<int>> GetActiveYearsAsync()
    {
        return await db.Workouts
            .Select(w => w.StartTime.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();
    }
}
