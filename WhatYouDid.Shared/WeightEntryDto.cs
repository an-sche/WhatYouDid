namespace WhatYouDid.Shared;

public record WeightEntryDto(Guid WeightEntryId, DateTimeOffset LoggedAt, decimal Pounds);

/// <summary>
/// One day's worth of weigh-ins collapsed to a single average, used by the trend chart.
/// </summary>
public record WeightAveragePointDto(DateOnly Date, decimal AveragePounds);
