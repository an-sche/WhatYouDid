using Microsoft.EntityFrameworkCore;
using WhatYouDid.Data;
using WhatYouDid.Shared;

namespace WhatYouDid.Services;

public class WeightService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ITenantService tenantService
) : IWeightService
{
    public async Task<List<WeightEntryDto>> GetRecentEntriesAsync(int days = 30)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var cutoff = DateTimeOffset.Now.AddDays(-days);

        return await db.WeightEntries
            .AsNoTracking()
            .Where(w => w.LoggedAt >= cutoff)
            .OrderByDescending(w => w.LoggedAt)
            .Select(w => new WeightEntryDto(w.WeightEntryId, w.LoggedAt, w.Pounds))
            .ToListAsync();
    }

    public async Task<WeightEntryDto?> AddEntryAsync(CreateWeightEntryDto dto)
    {
        if (string.IsNullOrEmpty(tenantService.Tenant))
            throw new Exception("Could not resolve tenant");

        using var db = await dbFactory.CreateDbContextAsync();

        var entry = new WeightEntry
        {
            WeightEntryId = Guid.CreateVersion7(),
            ApplicationUserId = tenantService.Tenant,
            LoggedAt = dto.LoggedAt ?? DateTimeOffset.Now,
            Pounds = dto.Pounds,
        };

        await db.WeightEntries.AddAsync(entry);
        await db.SaveChangesAsync();

        return new WeightEntryDto(entry.WeightEntryId, entry.LoggedAt, entry.Pounds);
    }

    public async Task<bool> DeleteEntryAsync(Guid weightEntryId)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        // The global query filter scopes this to the current tenant, so another
        // user's entry simply isn't found.
        var entry = await db.WeightEntries
            .FirstOrDefaultAsync(w => w.WeightEntryId == weightEntryId);

        if (entry is null) return false;

        db.WeightEntries.Remove(entry);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<WeightAveragePointDto>> GetDailyAveragesAsync(int days = 90)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var cutoff = DateTimeOffset.Now.AddDays(-days);

        // Grouping on LoggedAt.Date translates to CONVERT(date, ...) so both the
        // day bucketing and the average happen in SQL. WeightSqlTranslationTests
        // pins that shape.
        var buckets = await db.WeightEntries
            .AsNoTracking()
            .Where(w => w.LoggedAt >= cutoff)
            .GroupBy(w => w.LoggedAt.Date)
            .Select(g => new
            {
                Day = g.Key,
                Average = g.Average(w => w.Pounds)
            })
            .ToListAsync();

        return [.. buckets
            .Select(b => new WeightAveragePointDto(
                DateOnly.FromDateTime(b.Day),
                Math.Round(b.Average, 2)))
            .OrderBy(p => p.Date)];
    }
}
