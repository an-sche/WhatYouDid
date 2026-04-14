using Microsoft.Playwright;

namespace WhatYouDid.UITests.WorkoutPage;

[Collection("Playwright")]
public class RoutineCardResumeTests(PlaywrightFixture fixture)
{
    private static ILocator NextButton(AuthenticatedPage page)
        => page.GetByRole(AriaRole.Button, new() { Name = "Next exercise" });

    [Fact]
    public async Task RoutineCard_ShowsContinueButton_AfterPartialWorkout()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Resume Test Routine", exerciseCount: 2);

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);

        // Start the workout and navigate to exercise 2, which saves progress to storage
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page, $"{fixture.Factory.BaseAddress}workout/{routineId}");
        await Expect(page.GetByText("1 of 2")).ToBeVisibleAsync();
        await NextButton(page).ClickAsync();
        await Expect(page.GetByText("2 of 2")).ToBeVisibleAsync();

        // Navigate to the routines page
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page, $"{fixture.Factory.BaseAddress}routines");
        await Expect(page.GetByText("Resume Test Routine")).ToBeVisibleAsync();

        // Continue button should now be visible
        await Expect(page.Locator("[title='Continue workout']")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task RoutineCard_DiscardWorkout_ShowsStartButton()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Discard Test Routine", exerciseCount: 2);

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);

        // Start the workout and navigate to exercise 2, which saves progress to storage
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page, $"{fixture.Factory.BaseAddress}workout/{routineId}");
        await Expect(page.GetByText("1 of 2")).ToBeVisibleAsync();
        await NextButton(page).ClickAsync();
        await Expect(page.GetByText("2 of 2")).ToBeVisibleAsync();

        // Navigate to the routines page and verify Continue is showing
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page, $"{fixture.Factory.BaseAddress}routines");
        await Expect(page.Locator("[title='Discard in-progress workout']")).ToBeVisibleAsync();

        // Discard the workout
        await page.Locator("[title='Discard in-progress workout']").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Discard", Exact = true }).ClickAsync();

        // Continue should be gone and Start should be visible instead
        await Expect(page.Locator("[title='Continue workout']")).ToBeHiddenAsync();
        await Expect(page.Locator("[title='Start workout']")).ToBeVisibleAsync();
    }
}
