using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace WhatYouDid.Tests.Infrastructure;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private ServiceProvider _serviceProvider = null!;

    public DbContextOptions<ApplicationDbContext> DbContextOptions { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        DbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        // Run all EF Core migrations against the container
        var tenantService = new TestTenantService();
        await using var db = new ApplicationDbContext(DbContextOptions, tenantService);
        await db.Database.MigrateAsync();

        // Set up DI for Identity (UserManager, RoleManager) used by helper methods
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddScoped<ITenantService, TestTenantService>();
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseSqlServer(_container.GetConnectionString()));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task<ApplicationUser> CreateUserAsync(string email, string password)
        => await UserHelper.CreateUserAsync(_serviceProvider, email, password);

    public IServiceScope CreateScope() => _serviceProvider.CreateScope();

    /// <summary>
    /// Returns a fresh <see cref="ApplicationDbContext"/> scoped to the given userId.
    /// The caller is responsible for disposing it.
    /// </summary>
    public ApplicationDbContext CreateDbContextForTenant(string userId)
    {
        var tenantService = new TestTenantService();
        tenantService.SetTenant(userId);
        return new ApplicationDbContext(DbContextOptions, tenantService);
    }

    /// <summary>
    /// Returns a <see cref="WhatYouDidApiDirectAccess"/> wired to the given tenant service.
    /// Switch tenants between calls by calling <see cref="TestTenantService.SetTenant"/>.
    /// </summary>
    public WhatYouDidApiDirectAccess CreateApiForTenant(TestTenantService tenantService)
    {
        var factory = new TestDbContextFactory(DbContextOptions, tenantService);
        return new WhatYouDidApiDirectAccess(factory, tenantService);
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _container.DisposeAsync();
    }
}
