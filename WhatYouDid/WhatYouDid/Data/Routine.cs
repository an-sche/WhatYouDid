using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Data;

/// <summary>
/// Describes a specific workout routine. Such as "Chest and Back" or "Back and Bicep"
/// </summary>
public class Routine
{
	[Key]
	public int RoutineId { get; set; }

	[Required,
		MaxLength(50, ErrorMessage = "Length must be less than 50 characters"),
		MinLength(3, ErrorMessage = "Length must be at least 3 characters.")]
	public required string Name { get; set; }

	public required List<Exercise> Exercises { get; set; }

	public ApplicationUser? CreateUser { get; set; } = null;
}

