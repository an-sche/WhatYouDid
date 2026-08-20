using System.Net;
using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Tests.WasmApi;

[Collection("WasmApi")]
public class WeightApiTests(ApiWebApplicationFactory factory)
{
    // -------------------------------------------------------------------------
    // Auth
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetWeight_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/weight");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostWeight_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/weight",
            new CreateWeightEntryDto { Pounds = 180m });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // POST /api/weight
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostWeight_ThenGet_RoundTrips()
    {
        var user = await CreateUserAsync("weight-roundtrip");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var response = await client.PostAsJsonAsync("/api/weight",
            new CreateWeightEntryDto { Pounds = 182.5m, LoggedAt = DateTimeOffset.Now });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<WeightEntryDto>();
        Assert.NotNull(created);
        Assert.Equal(182.5m, created.Pounds);
        Assert.NotEqual(Guid.Empty, created.WeightEntryId);

        var entries = await client.GetFromJsonAsync<List<WeightEntryDto>>("/api/weight");
        Assert.NotNull(entries);
        Assert.Contains(entries, e => e.WeightEntryId == created.WeightEntryId);
    }

    /// <summary>
    /// The defining behaviour of the multiple-per-day model: a second weigh-in on the
    /// same day is an additional row, not a replacement.
    /// </summary>
    [Fact]
    public async Task PostWeight_TwiceSameDay_KeepsBothEntries()
    {
        var user = await CreateUserAsync("weight-sameday");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var today = DateTimeOffset.Now.Date;
        var morning = new DateTimeOffset(today.AddHours(7), DateTimeOffset.Now.Offset);
        var evening = new DateTimeOffset(today.AddHours(21), DateTimeOffset.Now.Offset);

        await client.PostAsJsonAsync("/api/weight",
            new CreateWeightEntryDto { Pounds = 182.5m, LoggedAt = morning });
        await client.PostAsJsonAsync("/api/weight",
            new CreateWeightEntryDto { Pounds = 185.1m, LoggedAt = evening });

        var entries = await client.GetFromJsonAsync<List<WeightEntryDto>>("/api/weight");

        Assert.NotNull(entries);
        var sameDay = entries.Where(e => e.LoggedAt.LocalDateTime.Date == today).ToList();
        Assert.Equal(2, sameDay.Count);
        Assert.Contains(sameDay, e => e.Pounds == 182.5m);
        Assert.Contains(sameDay, e => e.Pounds == 185.1m);
    }

    [Fact]
    public async Task PostWeight_OutOfRange_Returns400()
    {
        var user = await CreateUserAsync("weight-range");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var tooHeavy = await client.PostAsJsonAsync("/api/weight",
            new CreateWeightEntryDto { Pounds = 5000m });
        var tooLight = await client.PostAsJsonAsync("/api/weight",
            new CreateWeightEntryDto { Pounds = 0m });

        Assert.Equal(HttpStatusCode.BadRequest, tooHeavy.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLight.StatusCode);
    }

    [Fact]
    public async Task PostWeight_WithoutTimestamp_DefaultsToNow()
    {
        var user = await CreateUserAsync("weight-defaultnow");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var before = DateTimeOffset.Now.AddMinutes(-1);
        var response = await client.PostAsJsonAsync("/api/weight",
            new CreateWeightEntryDto { Pounds = 175m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<WeightEntryDto>();
        Assert.NotNull(created);
        Assert.InRange(created.LoggedAt, before, DateTimeOffset.Now.AddMinutes(1));
    }

    // -------------------------------------------------------------------------
    // GET /api/weight/history — daily averages
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetHistory_AveragesMultipleEntriesOnSameDay()
    {
        var user = await CreateUserAsync("weight-average");
        var client = factory.CreateAuthenticatedClient(user.Id);

        // Two weigh-ins on the same day, three days back to avoid boundary flakiness.
        var day = DateTimeOffset.Now.AddDays(-3);
        await factory.AddWeightEntryAsync(user.Id, day.Date.AddHours(7), 180m);
        await factory.AddWeightEntryAsync(user.Id, day.Date.AddHours(21), 190m);

        var points = await client.GetFromJsonAsync<List<WeightAveragePointDto>>("/api/weight/history");

        Assert.NotNull(points);
        var target = DateOnly.FromDateTime(day.Date);
        var point = points.FirstOrDefault(p => p.Date == target);
        Assert.NotNull(point);
        Assert.Equal(185m, point.AveragePounds);
    }

    [Fact]
    public async Task GetHistory_ExcludesEntriesOutsideRange()
    {
        var user = await CreateUserAsync("weight-range-filter");
        var client = factory.CreateAuthenticatedClient(user.Id);

        var old = DateTimeOffset.Now.AddDays(-60);
        await factory.AddWeightEntryAsync(user.Id, old, 200m);

        var points = await client.GetFromJsonAsync<List<WeightAveragePointDto>>("/api/weight/history?days=7");

        Assert.NotNull(points);
        Assert.DoesNotContain(points, p => p.Date == DateOnly.FromDateTime(old.Date));
    }

    // -------------------------------------------------------------------------
    // DELETE /api/weight/{id} — including tenant isolation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteWeight_OwnEntry_RemovesIt()
    {
        var user = await CreateUserAsync("weight-delete");
        var client = factory.CreateAuthenticatedClient(user.Id);
        var entryId = await factory.AddWeightEntryAsync(user.Id, DateTimeOffset.Now, 181m);

        var response = await client.DeleteAsync($"/api/weight/{entryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var entries = await client.GetFromJsonAsync<List<WeightEntryDto>>("/api/weight");
        Assert.NotNull(entries);
        Assert.DoesNotContain(entries, e => e.WeightEntryId == entryId);
    }

    [Fact]
    public async Task DeleteWeight_OtherUsersEntry_Returns404_AndLeavesItIntact()
    {
        var userA = await CreateUserAsync("weight-iso-a");
        var userB = await CreateUserAsync("weight-iso-b");
        var entryId = await factory.AddWeightEntryAsync(userA.Id, DateTimeOffset.Now, 199m);

        var clientB = factory.CreateAuthenticatedClient(userB.Id);
        var response = await clientB.DeleteAsync($"/api/weight/{entryId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Still there for its owner.
        var clientA = factory.CreateAuthenticatedClient(userA.Id);
        var entries = await clientA.GetFromJsonAsync<List<WeightEntryDto>>("/api/weight");
        Assert.NotNull(entries);
        Assert.Contains(entries, e => e.WeightEntryId == entryId);
    }

    [Fact]
    public async Task GetWeight_DoesNotIncludeOtherUsersEntries()
    {
        var userA = await CreateUserAsync("weight-list-iso-a");
        var userB = await CreateUserAsync("weight-list-iso-b");
        var entryId = await factory.AddWeightEntryAsync(userA.Id, DateTimeOffset.Now, 165m);

        var clientB = factory.CreateAuthenticatedClient(userB.Id);
        var entries = await clientB.GetFromJsonAsync<List<WeightEntryDto>>("/api/weight");

        Assert.NotNull(entries);
        Assert.DoesNotContain(entries, e => e.WeightEntryId == entryId);
    }

    private Task<ApplicationUser> CreateUserAsync(string prefix)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        return factory.CreateUserAsync($"{prefix}-{id}@test.com", "Test1234!");
    }
}
