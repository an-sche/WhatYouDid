using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Data;

public class WorkoutExerciseSet
{
    [Key]
    public int WorkoutExerciseSetId { get; set; }

    [Required]
    public int WorkoutExerciseId { get; set; }
    public WorkoutExercise WorkoutExercise { get; set; } = null!;

    public required int SetNumber { get; set; }  // 1-based

    public int? Reps { get; set; }
    public int? Weight { get; set; }
    public int? Duration { get; set; }

    public int? AlternateReps { get; set; }
    public int? AlternateWeight { get; set; }
    public int? AlternateDuration { get; set; }
    [MaxLength(20)]
    public string? Note { get; set; }
}
