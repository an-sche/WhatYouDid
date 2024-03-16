namespace WhatYouDid.Data;

/// <summary>
/// Information about a workout defined by a Routine. 
/// Also holds information about the last time this Routine was performed.
/// </summary>
public class WorkoutDto
{
    public required string ApplicationUserId { get; init; }
    public required int RoutineId { get; init; }
    public required string RoutineName { get; set; }

    public DateTime StartTime { get; set; } = DateTime.Now;

    public List<WorkoutExerciseDto>? WorkoutExercises { get; init; }
    
}

public class WorkoutExerciseDto
{
    public required int Sequence { get; init; }
    public required string ExerciseName { get; init; }
    public string? Descriptions { get; init; }
    public required int Sets { get; init; }

    public List<int>? LastReps { get; init; }
    public List<int>? LastWeights { get; init; }
    public List<int>? LastDurations { get; init; }

    public List<int>? Reps { get; set; }
    public List<int>? Weights { get; set; }
    public List<int>? Durations { get; set; } 
}