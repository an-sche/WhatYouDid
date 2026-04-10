using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Shared;

public class CreateRoutineDto
{
    [Required, MaxLength(50), MinLength(3)]
    public required string Name { get; set; }
    public required List<CreateExerciseDto> Exercises { get; set; }
}
