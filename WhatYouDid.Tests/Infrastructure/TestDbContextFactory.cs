namespace WhatYouDid.Tests.Infrastructure;

public class TestDbContextFactory(
    DbContextOptions<ApplicationDbContext> options,
    ITenantService tenantService) : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext()
        => new ApplicationDbContext(options, tenantService);
}
