namespace WhatYouDid.Shared;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardForUserAsync(int? year = null);
    Task<IReadOnlyList<int>> GetActiveYearsAsync();
}
