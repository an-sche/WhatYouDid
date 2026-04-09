using System.Text;
using System.Text.Json.Serialization;

namespace WhatYouDid.Shared;

public class WorkoutListItemDto
{
    public Guid WorkoutId { get; set; }
    public string? RoutineName { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}

/// <summary>
/// Information about a workout defined by a Routine. 
/// Also holds information about the last time this Routine was performed.
/// </summary>
public class WorkoutDto
{
    public string GetBrowserStorageId { get { return "Workout_" + RoutineName + "_" + RoutineId; } }

    public required Guid WorkoutId { get; init; }
    public required int RoutineId { get; init; }
    public required string RoutineName { get; set; }

    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? EndTime { get; set; }

    public List<WorkoutExerciseDto>? WorkoutExercises { get; set; }
    public int ExerciseIndex { get; set; } = 0;
}

public class WorkoutExerciseDto
{
    [JsonIgnore]
    public string Review {
        get {
            StringBuilder line = new();
            for (int i = 0; i < Sets; i++) {
                if (HasReps) {
                    string val = Reps.Length > i ? (Reps[i]?.ToString() ?? "--") : "--";
                    if (AlternateReps.Length > i && AlternateReps[i] != null) val += "+" + AlternateReps[i];
                    line.Append("R: " + val + "  ");
                }
                if (HasWeights) {
                    string val = Weights.Length > i ? (Weights[i]?.ToString() ?? "--") : "--";
                    if (AlternateWeights.Length > i && AlternateWeights[i] != null) val += "+" + AlternateWeights[i];
                    line.Append("W: " + val + "  ");
                }
                if (HasDurations) {
                    string val = Durations.Length > i ? (Durations[i]?.ToString() ?? "--") : "--";
                    if (AlternateDurations.Length > i && AlternateDurations[i] != null) val += "+" + AlternateDurations[i];
                    line.Append("D: " + val + "  ");
                }
                if (Notes.Length > i && !string.IsNullOrEmpty(Notes[i])) line.Append($"({Notes[i]})  ");
                line.Append(Environment.NewLine);
            }
            return line.ToString();
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
    public int?[]? LastAlternateReps { get; set; } = [];
    public int?[]? LastAlternateWeights { get; set; } = [];
    public int?[]? LastAlternateDurations { get; set; } = [];
    public string?[]? LastNotes { get; set; } = [];

    public int?[] Reps { get; init; } = [];
    public int?[] Weights { get; init; } = [];
    public int?[] Durations { get; init; } = [];
    public int?[] AlternateReps { get; init; } = [];
    public int?[] AlternateWeights { get; init; } = [];
    public int?[] AlternateDurations { get; init; } = [];
    public string?[] Notes { get; init; } = [];

    public bool HasAnyAlternateData(int setIndex) =>
        (AlternateReps.Length > setIndex && AlternateReps[setIndex] != null)
        || (AlternateWeights.Length > setIndex && AlternateWeights[setIndex] != null)
        || (AlternateDurations.Length > setIndex && AlternateDurations[setIndex] != null)
        || (Notes.Length > setIndex && !string.IsNullOrEmpty(Notes[setIndex]));

}