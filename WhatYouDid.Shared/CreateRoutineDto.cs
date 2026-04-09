using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Shared;

public class CreateRoutineDto : IValidatableObject
{
    [Required, MaxLength(50), MinLength(3)]
    public required string Name { get; set; }
    public required List<CreateExerciseDto> Exercises { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var exercise in Exercises)
        {
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(exercise, new ValidationContext(exercise), errors, validateAllProperties: true))
                foreach (var error in errors)
                    yield return error;
        }
    }
}
