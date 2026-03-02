namespace WhatYouDid.Shared;

public sealed class DashboardDto
{
    public int? Year { get; set; }
    public List<WorkoutSummaryDto>? TopWorkouts { get; set; }
}

public sealed class WorkoutSummaryDto
{
    public string? RoutineName { get; set; }
    public int Count { get; set; }
}
