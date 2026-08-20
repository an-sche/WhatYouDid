using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Client.Services;

public class WeightHttpService(HttpClient http) : IWeightService
{
    public async Task<List<WeightEntryDto>> GetRecentEntriesAsync(int days = 30)
        => await http.GetFromJsonAsync<List<WeightEntryDto>>($"/api/weight?days={days}") ?? [];

    public async Task<WeightEntryDto?> AddEntryAsync(CreateWeightEntryDto dto)
    {
        var response = await http.PostAsJsonAsync("/api/weight", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<WeightEntryDto>();
    }

    public async Task<bool> DeleteEntryAsync(Guid weightEntryId)
    {
        var response = await http.DeleteAsync($"/api/weight/{weightEntryId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<WeightAveragePointDto>> GetDailyAveragesAsync(int days = 90)
        => await http.GetFromJsonAsync<List<WeightAveragePointDto>>($"/api/weight/history?days={days}") ?? [];
}
