using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Data;

/// <summary>
/// Instance of a Routine performed by a user. Ex. On x Day, User y, performed z Routine.
/// </summary>
public class Workout
{
	[Key]
	public int WorkoutId { get; set; }

    public required string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
    public ICollection<WorkoutExercise>? WorkoutExercise { get; set; }
    public int? RoutineId { get; set; }
    public Routine? Routine { get; set; }

    [MaxLength(50)]
    public required string RoutineName { get; set; } 

    public required DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime? EndTime { get; set;}
}
