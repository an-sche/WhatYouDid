using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace WhatYouDid.Shared;

[ValidatableType]
public class CreateWeightEntryDto
{
    [Range(1, 1500, ErrorMessage = "Weight must be between 1 and 1500 lb.")]
    public required decimal Pounds { get; set; }

    /// <summary>
    /// When the weigh-in happened, supplied by the client so it reflects the user's
    /// local clock. The server falls back to its own clock when omitted.
    /// </summary>
    public DateTimeOffset? LoggedAt { get; set; }
}
