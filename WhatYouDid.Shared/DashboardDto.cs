namespace WhatYouDid.Shared;

public sealed class DashboardDto
{
    public int? Year { get; set; }
    public List<WorkoutSummaryDto>? TopWorkouts { get; set; }
    public int? TotalWorkoutDuration { get; set; }
    public int? TotalReps { get; set; }
    public DateTime? LongestWorkoutDate { get; set; }
    public string? LongestWorkoutRoutineName { get; set; }
    public int? LongestWorkoutDuration { get; set; }
}

public sealed class WorkoutSummaryDto
{
    public string? RoutineName { get; set; }
    public int Count { get; set; }
}
