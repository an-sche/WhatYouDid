using Microsoft.AspNetCore.Identity;

namespace WhatYouDid.Services;

public record AdminUserDto(string Id, string? Email);

public interface IAdminService
{
    Task<List<AdminUserDto>> GetUsersAsync();
    Task<IdentityResult> CreateUserAsync(string email, string password);
    Task<IdentityResult> ResetPasswordAsync(string userId, string newPassword);
}
