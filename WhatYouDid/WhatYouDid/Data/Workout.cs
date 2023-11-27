using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Data;

public class Workout
{
    [Key]
    public int Id { get; set; }

    [Required,
        MaxLength(50, ErrorMessage = "Name must be less than 50 characters"),
        MinLength(3, ErrorMessage = "Name must be at least 3 characters.")]
    public string Name { get; set; }
}
