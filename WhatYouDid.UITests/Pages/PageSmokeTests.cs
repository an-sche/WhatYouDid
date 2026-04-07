namespace WhatYouDid.UITests.Pages;

[Collection("Playwright")]
public class PageSmokeTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task DashboardPage_Loads_WithoutConsoleErrors()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}dashboard");
    }

    [Fact]
    public async Task RoutinesPage_Loads_WithoutConsoleErrors()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}routines");
    }

    [Fact]
    public async Task WorkoutsPage_Loads_WithoutConsoleErrors()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workouts");
    }
}
