using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Data;

/// <summary>
/// Instance of a Routine performed by a user. Ex. On x Day, User y, performed z Routine.
/// </summary>
public class Workout
{
	[Key]
	public int WorkoutId { get; set; }

    /// <summary>
    /// Aka (Routine Name)
    /// </summary>
    [Required,
        MaxLength(50, ErrorMessage = "Name must be less than 50 characters"),
        MinLength(3, ErrorMessage = "Name must be at least 3 characters.")]
    public required string RoutineName { get; set; } 

    public required DateTime StartTime { get; set; } = DateTime.Now;
    public required DateTime EndTime { get; set;} = DateTime.Now;
}
