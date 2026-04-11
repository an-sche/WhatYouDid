using Microsoft.EntityFrameworkCore;
using WhatYouDid.Shared;

namespace WhatYouDid;

public static class PagedListExtensions
{
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 0) throw new ArgumentOutOfRangeException(nameof(page), "Page must be >= 0.");
        if (pageSize < 1 || pageSize > 1000)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 1000.");

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedList<T>(items, page, pageSize, totalCount);
    }
}
