using Microsoft.AspNetCore.Identity;

namespace WhatYouDid.Data;
// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
	public ICollection<Workout>? Workouts { get; set; } 

	public ICollection<Routine>? Routines { get; set; }
}

