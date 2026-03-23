using System.Net;

namespace WhatYouDid.Tests.Admin;

[Collection("WasmApi")]
public class AdminAuthorizationTests(ApiWebApplicationFactory factory)
{
    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/createuser")]
    [InlineData("/admin/resetpassword")]
    public async Task AdminPage_Unauthenticated_Returns401(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/createuser")]
    [InlineData("/admin/resetpassword")]
    public async Task AdminPage_AuthenticatedNonAdmin_Returns403(string path)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"nonadmin-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/createuser")]
    [InlineData("/admin/resetpassword")]
    public async Task AdminPage_AuthenticatedAdmin_Returns200(string path)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"admin-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id, roles: "Admin");

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
