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
        // Compute top 3 most frequent workouts (by RoutineName) optionally filtered by year.
        var query = db.Workouts.AsNoTracking().AsQueryable();
        var dto = new DashboardDto();

        if (year != 0)
        {
            query = query.Where(w => w.StartTime.Year == year);
            dto.Year = year;
        }

        var result = await query
            .GroupBy(w => w.RoutineName)
            .Select(g => new { RoutineName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(3)
            .Select(x => new WorkoutSummaryDto { RoutineName = x.RoutineName ?? "", Count = x.Count })
            .ToListAsync();

        dto.TopWorkouts = result;

        return dto;
    }
}
