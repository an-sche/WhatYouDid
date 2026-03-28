using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Data;

/// <summary>
/// Individual Record of an Exercise performed within a workout.
/// </summary>
public class WorkoutExercise
{
    [Key]
    public int WorkoutExerciseId { get; set; }

    [Required]
    public Guid WorkoutId { get; set; }

    [Required]
    public Workout Workout { get; set; } = null!;

    public int? ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    [MinLength(3), MaxLength(50)]
    public required string ExerciseName { get; set; }

    public ICollection<WorkoutExerciseSet> Sets { get; set; } = [];
}
