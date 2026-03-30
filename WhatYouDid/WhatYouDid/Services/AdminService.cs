using Microsoft.AspNetCore.Identity;
using WhatYouDid.Data;

namespace WhatYouDid.Services;

public class AdminService(UserManager<ApplicationUser> userManager) : IAdminService
{
    public Task<List<AdminUserDto>> GetUsersAsync()
    {
        var users = userManager.Users
            .Select(u => new AdminUserDto(u.Id, u.Email))
            .ToList();
        return Task.FromResult(users);
    }

    public Task<IdentityResult> CreateUserAsync(string email, string password)
    {
        var user = new ApplicationUser { UserName = email, Email = email };
        return userManager.CreateAsync(user, password);
    }

    public async Task<IdentityResult> ResetPasswordAsync(string userId, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User not found." });
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return await userManager.ResetPasswordAsync(user, token, newPassword);
    }
}
