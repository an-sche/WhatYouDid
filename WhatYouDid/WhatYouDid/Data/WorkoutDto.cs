using System.Text;

namespace WhatYouDid.Data;

/// <summary>
/// Information about a workout defined by a Routine. 
/// Also holds information about the last time this Routine was performed.
/// </summary>
public class WorkoutDto
{
    public string GetBrowserStorageId { get { return "Workout_" + RoutineName + "_" + RoutineId; } }

    public required string ApplicationUserId { get; init; }
    public required int RoutineId { get; init; }
    public required string RoutineName { get; set; }

    public DateTime StartTime { get; set; } = DateTime.Now;

    public List<WorkoutExerciseDto>? WorkoutExercises { get; init; }
    public int ExerciseIndex { get; set; } = 0;
}

public class WorkoutExerciseDto
{
    public string Review {
        get {
            StringBuilder result = new();
            for (int i = 0; i < Sets; i++) {
                StringBuilder line = new();
                if (HasReps) { 
                    line.Append("R: ");
                    if (Reps.Length > i) line.Append((Reps[i]?.ToString() ?? "--") + "  ");
                }
                if (HasWeights) {
                    line.Append("W: ");
                    if (Weights.Length > i) line.Append((Weights[i]?.ToString() ?? "--") + "  ");
                } 
                if (HasDurations) {
                    line.Append("D: ");
                    if (Durations.Length > i) line.Append((Durations[i]?.ToString() ?? "--") + "  ");
                } 
                line.Append(Environment.NewLine);
                result.Append(line);
            }
            return result.ToString();
        }
    }
    
    public required int Sequence { get; init; }
    public required int ExerciseId { get; init; }
    public required string ExerciseName { get; init; }
    public string? Descriptions { get; init; }
    public required int Sets { get; init; }

    public required bool HasReps { get; init; }
    public required bool HasWeights { get; init; }
    public required bool HasDurations { get; init; }

    public int?[]? LastReps { get; set; } = [];
    public int?[]? LastWeights { get; set; } = [];
    public int?[]? LastDurations { get; set; } = [];

    public int?[] Reps { get; init; } = [];
    public int?[] Weights { get; init; } = [];
    public int?[] Durations { get; init; } = [];
}