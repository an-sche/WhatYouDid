using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Data;

public class Exercise
{
    [Required,
        MaxLength(50, ErrorMessage="Length must be less than 50 characters"),
        MinLength(3, ErrorMessage="Length must be at least 3 characters.")]
    public string Name { get; set; }
}
