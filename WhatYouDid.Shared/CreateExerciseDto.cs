using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Shared;

public class CreateExerciseDto
{
    [Required, MaxLength(50), MinLength(3)]
    public required string Name { get; set; }
    [MaxLength(1000)]
    public string? Description { get; set; }
    public required int Sequence { get; set; }
    [Range(1, 10)]
    public required int Sets { get; set; }
    public bool HasReps { get; set; }
    public bool HasWeight { get; set; }
    public bool HasDuration { get; set; }
}
