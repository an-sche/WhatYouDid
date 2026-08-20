namespace WhatYouDid.Shared;

public interface IWeightService
{
    Task<List<WeightEntryDto>> GetRecentEntriesAsync(int days = 30);
    Task<WeightEntryDto?> AddEntryAsync(CreateWeightEntryDto dto);
    Task<bool> DeleteEntryAsync(Guid weightEntryId);
    Task<List<WeightAveragePointDto>> GetDailyAveragesAsync(int days = 90);
}
