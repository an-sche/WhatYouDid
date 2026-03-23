namespace WhatYouDid.Tests.Infrastructure;

public class TestTenantService : ITenantService
{
    public string Tenant { get; private set; } = string.Empty;
    public void SetTenant(string tenant) => Tenant = tenant;
}
