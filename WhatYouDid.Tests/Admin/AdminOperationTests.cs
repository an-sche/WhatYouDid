using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace WhatYouDid.Tests.Admin;

[Collection("Database")]
public class AdminOperationTests(DatabaseFixture fixture)
{
    // -------------------------------------------------------------------------
    // Create user
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateUser_ValidCredentials_Succeeds()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = $"new-{id}@test.com", Email = $"new-{id}@test.com" };
        var result = await userManager.CreateAsync(user, "Test1234!");

        Assert.True(result.Succeeded);
        var found = await userManager.FindByEmailAsync($"new-{id}@test.com");
        Assert.NotNull(found);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Fails()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var email = $"dup-{id}@test.com";
        await fixture.CreateUserAsync(email, "Test1234!");

        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var duplicate = new ApplicationUser { UserName = email, Email = email };
        var result = await userManager.CreateAsync(duplicate, "Test1234!");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code is "DuplicateUserName" or "DuplicateEmail");
    }

    [Fact]
    public async Task CreateUser_WeakPassword_Fails()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = $"weakpw-{id}@test.com", Email = $"weakpw-{id}@test.com" };
        var result = await userManager.CreateAsync(user, "weak");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code.StartsWith("Password"));
    }

    // -------------------------------------------------------------------------
    // Reset password
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ResetPassword_NewPasswordWorks_OldPasswordDoesNot()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await fixture.CreateUserAsync($"reset-{id}@test.com", "OldPass1234!");

        using var scope = fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Re-fetch within this scope so EF tracks the entity in the correct DbContext
        var freshUser = await userManager.FindByIdAsync(user.Id);
        var token = await userManager.GeneratePasswordResetTokenAsync(freshUser!);
        var result = await userManager.ResetPasswordAsync(freshUser!, token, "NewPass5678!");

        Assert.True(result.Succeeded);
        Assert.True(await userManager.CheckPasswordAsync(freshUser!, "NewPass5678!"));
        Assert.False(await userManager.CheckPasswordAsync(freshUser!, "OldPass1234!"));
    }
}
