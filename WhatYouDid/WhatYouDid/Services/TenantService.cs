namespace WhatYouDid.Services;

public class TenantService : ITenantService
{
    private string _tenant = string.Empty;

    public string Tenant => _tenant ?? string.Empty;

    public void SetTenant(string tenant)
    {
        _tenant = tenant ?? string.Empty;
    }
}
