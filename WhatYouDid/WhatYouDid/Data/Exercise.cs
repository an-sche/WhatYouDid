using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Data;

public class Exercise
{
    [Key]
    public int ExerciseId { get; set; }

    [Required,
        MaxLength(50, ErrorMessage="Length must be less than 50 characters"),
        MinLength(3, ErrorMessage="Length must be at least 3 characters.")]
    public required string Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// The Order in the Main Routine
    /// </summary>
    public required int Sequence { get; set; }

    /// <summary>
    /// The number of sets to do (IE on strip set = 3)
    /// </summary>
    public required int Sets { get; set; } = 1;


    public bool HasReps { get; set; } = false;
    public bool HasWeight { get; set; } = false;
    public bool HasDuration { get; set; } = false;
}
