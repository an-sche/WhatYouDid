using Microsoft.AspNetCore.Identity;
using WhatYouDid.Data;

namespace WhatYouDid.Services;

internal sealed class AdminRoleSeeder(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<AdminRoleSeeder> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var admins = configuration.GetSection("Admins").Get<string[]>() ?? [];

            var adminExists = await roleManager.RoleExistsAsync("Admin");
            if (!adminExists)
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
                if (!roleResult.Succeeded)
                {
                    logger.LogError("Failed to create Admin role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            foreach (var admin in admins)
            {
                if (string.IsNullOrWhiteSpace(admin))
                {
                    continue;
                }

                var existing = await userManager.FindByEmailAsync(admin);
                if (existing is null)
                {
                    logger.LogWarning("Configured admin user '{Email}' was not found.", admin);
                    continue;
                }

                var alreadyInRole = await userManager.IsInRoleAsync(existing, "Admin");
                if (alreadyInRole)
                {
                    continue;
                }

                var result = await userManager.AddToRoleAsync(existing, "Admin");
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to assign Admin role to {Email}: {Errors}", admin, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding admin role/users.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
