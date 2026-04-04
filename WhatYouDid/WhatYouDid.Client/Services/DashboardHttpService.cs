using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Client.Services;

public class DashboardHttpService(HttpClient http) : IDashboardService
{
    public async Task<DashboardDto> GetDashboardForUserAsync(int? year = null)
    {
        var url = year.HasValue ? $"/api/dashboard?year={year}" : "/api/dashboard";
        return await http.GetFromJsonAsync<DashboardDto>(url) ?? new DashboardDto();
    }

    public async Task<IReadOnlyList<int>> GetActiveYearsAsync()
    {
        return await http.GetFromJsonAsync<List<int>>("/api/dashboard/years") ?? [];
    }
}
