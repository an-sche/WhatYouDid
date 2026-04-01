using WhatYouDid.Shared;

namespace WhatYouDid.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardForUserAsync(int? year = null);
    Task<IReadOnlyList<int>> GetActiveYearsAsync();
}
