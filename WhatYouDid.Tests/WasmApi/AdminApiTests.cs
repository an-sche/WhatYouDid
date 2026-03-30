using System.Net;
using System.Net.Http.Json;
using WhatYouDid.Services;

namespace WhatYouDid.Tests.WasmApi;

[Collection("WasmApi")]
public class AdminApiTests(ApiWebApplicationFactory factory)
{
    // -------------------------------------------------------------------------
    // GET /api/admin/users
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUsers_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AuthenticatedNonAdmin_Returns403()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"adm-nonadmin-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_Admin_Returns200WithList()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var admin = await factory.CreateUserAsync($"adm-list-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(admin.Id, roles: "Admin");

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<AdminUserDto>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
    }

    // -------------------------------------------------------------------------
    // POST /api/admin/users
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateUser_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/admin/users",
            new { Email = "new@test.com", Password = "Test1234!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_NonAdmin_Returns403()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"adm-create-nonadmin-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.PostAsJsonAsync("/api/admin/users",
            new { Email = $"created-{id}@test.com", Password = "Test1234!" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Admin_ValidCredentials_Returns201()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var admin = await factory.CreateUserAsync($"adm-creator-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(admin.Id, roles: "Admin");

        var response = await client.PostAsJsonAsync("/api/admin/users",
            new { Email = $"newuser-{id}@test.com", Password = "Test1234!" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Admin_CreatedUserAppearsInList()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var admin = await factory.CreateUserAsync($"adm-listcheck-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(admin.Id, roles: "Admin");
        var newEmail = $"listeduser-{id}@test.com";

        await client.PostAsJsonAsync("/api/admin/users",
            new { Email = newEmail, Password = "Test1234!" });

        var listResponse = await client.GetAsync("/api/admin/users");
        var users = await listResponse.Content.ReadFromJsonAsync<List<AdminUserDto>>();
        Assert.Contains(users!, u => u.Email == newEmail);
    }

    [Fact]
    public async Task CreateUser_Admin_WeakPassword_Returns400()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var admin = await factory.CreateUserAsync($"adm-weakpw-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(admin.Id, roles: "Admin");

        var response = await client.PostAsJsonAsync("/api/admin/users",
            new { Email = $"weakpw-{id}@test.com", Password = "weak" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_Admin_DuplicateEmail_Returns400()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var admin = await factory.CreateUserAsync($"adm-dup-{id}@test.com", "Test1234!");
        var dupEmail = $"dupuser-{id}@test.com";
        await factory.CreateUserAsync(dupEmail, "Test1234!");
        var client = factory.CreateAuthenticatedClient(admin.Id, roles: "Admin");

        var response = await client.PostAsJsonAsync("/api/admin/users",
            new { Email = dupEmail, Password = "Test1234!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // POST /api/admin/users/{userId}/reset-password
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ResetPassword_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/reset-password",
            new { NewPassword = "NewPass1234!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_NonAdmin_Returns403()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = await factory.CreateUserAsync($"adm-reset-nonadmin-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{user.Id}/reset-password",
            new { NewPassword = "NewPass1234!" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_Admin_ValidUserId_Returns200()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var admin = await factory.CreateUserAsync($"adm-reset-admin-{id}@test.com", "Test1234!");
        var target = await factory.CreateUserAsync($"adm-reset-target-{id}@test.com", "OldPass1234!");
        var client = factory.CreateAuthenticatedClient(admin.Id, roles: "Admin");

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{target.Id}/reset-password",
            new { NewPassword = "NewPass5678!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_Admin_NonexistentUser_Returns404()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var admin = await factory.CreateUserAsync($"adm-reset-404-{id}@test.com", "Test1234!");
        var client = factory.CreateAuthenticatedClient(admin.Id, roles: "Admin");

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/reset-password",
            new { NewPassword = "NewPass5678!" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_Admin_WeakNewPassword_Returns400()
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var admin = await factory.CreateUserAsync($"adm-reset-weak-{id}@test.com", "Test1234!");
        var target = await factory.CreateUserAsync($"adm-reset-weak-target-{id}@test.com", "OldPass1234!");
        var client = factory.CreateAuthenticatedClient(admin.Id, roles: "Admin");

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{target.Id}/reset-password",
            new { NewPassword = "weak" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
