using Microsoft.Playwright;

namespace WhatYouDid.UITests.WorkoutPage;

[Collection("Playwright")]
public class WorkoutPageSmokeTests(PlaywrightFixture fixture)
{
    /// <summary>
    /// The highest-value test: navigates to the workout page and asserts that no
    /// console errors fire. Catches WASM runtime errors (missing providers, etc.)
    /// that API-layer tests cannot detect.
    /// </summary>
    [Fact]
    public async Task WorkoutPage_Loads_WithoutConsoleErrors()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var routineId = await fixture.Factory.CreateRoutineAsync(user.Id, "Smoke Test Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");
    }

    [Fact]
    public async Task WorkoutPage_BlazorErrorUi_IsHidden()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var routineId = await fixture.Factory.CreateRoutineAsync(user.Id, "Smoke Test Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");
        await Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    [Fact]
    public async Task WorkoutPage_ShowsFirstExercise()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var routineId = await fixture.Factory.CreateRoutineAsync(user.Id, "Smoke Test Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        // CreateRoutineAsync seeds one exercise named "Bench Press"
        await Expect(page.GetByText("Bench Press")).ToBeVisibleAsync();
    }
}
