using System.ComponentModel.DataAnnotations;

namespace WhatYouDid.Data;

/// <summary>
/// A single body weight reading recorded by a user. Multiple readings per day are
/// allowed — weighing in morning and evening keeps both rows.
/// </summary>
public class WeightEntry
{
    [Key]
    public Guid WeightEntryId { get; set; }

    public required string ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

    /// <summary>When the user weighed in, in their local time.</summary>
    public required DateTimeOffset LoggedAt { get; set; }

    public required decimal Pounds { get; set; }
}
