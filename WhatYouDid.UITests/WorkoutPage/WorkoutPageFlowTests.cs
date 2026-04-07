using Microsoft.Playwright;

namespace WhatYouDid.UITests.WorkoutPage;

[Collection("Playwright")]
public class WorkoutPageFlowTests(PlaywrightFixture fixture)
{
    private static ILocator NextButton(AuthenticatedPage page)
        => page.GetByRole(AriaRole.Button, new() { Name = "Next exercise" });

    private static ILocator BackButton(AuthenticatedPage page)
        => page.GetByRole(AriaRole.Button, new() { Name = "Previous exercise" });

    // MudTextFieldExtended (CodeBeam.MudBlazor.Extensions) doesn't associate labels via <label for="">,
    // so GetByLabel doesn't work. All workout form fields use InputMode.numeric — use that instead.
    private static ILocator NumericInputs(AuthenticatedPage page)
        => page.Locator("input[inputmode='numeric']");

    [Fact]
    public async Task BackButton_IsDisabledOnFirstExercise()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Flow Test Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Bench Press")).ToBeVisibleAsync();
        await Expect(BackButton(page)).ToBeDisabledAsync();
    }

    [Fact]
    public async Task NextButton_AdvancesToNextExercise()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Flow Test Routine", exerciseCount: 2);

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("1 of 2")).ToBeVisibleAsync();
        await NextButton(page).ClickAsync();
        await Expect(page.GetByText("2 of 2")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task BackButton_AfterNext_ReturnsToFirstExercise()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Flow Test Routine", exerciseCount: 2);

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("1 of 2")).ToBeVisibleAsync();
        await NextButton(page).ClickAsync();
        await Expect(page.GetByText("2 of 2")).ToBeVisibleAsync();

        await BackButton(page).ClickAsync();
        await Expect(page.GetByText("1 of 2")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task LastExercise_ShowsReviewScreen()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Flow Test Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Bench Press")).ToBeVisibleAsync();
        await NextButton(page).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Review" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Finish Workout" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task RepsField_AcceptsInput()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Flow Test Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Bench Press")).ToBeVisibleAsync();
        await NumericInputs(page).First.FillAsync("10");
        await Expect(NumericInputs(page).First).ToHaveValueAsync("10");
    }

    [Fact]
    public async Task WeightField_AcceptsInput()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Flow Test Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Bench Press")).ToBeVisibleAsync();
        // Nth(1) = Set 1 Weight (Nth(0) = Set 1 Reps)
        await NumericInputs(page).Nth(1).FillAsync("135");
        await Expect(NumericInputs(page).Nth(1)).ToHaveValueAsync("135");
    }

    [Fact]
    public async Task AltToggle_ShowsAltSection()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Flow Test Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Bench Press")).ToBeVisibleAsync();
        // 3 sets × 2 fields (Reps + Weight) = 6 inputs before expanding
        await Expect(NumericInputs(page)).ToHaveCountAsync(6);

        await page.GetByRole(AriaRole.Button, new() { Name = "Alt" }).First.ClickAsync();
        // After expanding Set 1 alt: +2 inputs (Alt Reps + Alt Weight)
        await Expect(NumericInputs(page)).ToHaveCountAsync(8);
    }

    [Fact]
    public async Task AltToggle_HidesAltSection_WhenClickedAgain()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Flow Test Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Bench Press")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Alt" }).First.ClickAsync();
        await Expect(NumericInputs(page)).ToHaveCountAsync(8);

        await page.GetByRole(AriaRole.Button, new() { Name = "Alt" }).First.ClickAsync();
        await Expect(NumericInputs(page)).ToHaveCountAsync(6);
    }

    [Fact]
    public async Task AutofillButton_PopulatesFromPreviousWorkout()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, exerciseIds) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Autofill Test Routine");
        await fixture.Factory.SaveWorkoutWithSetsAsync(
            user.Id, routineId, "Autofill Test Routine",
            exerciseId: exerciseIds[0], exerciseName: "Bench Press",
            reps: 10, weight: 135);

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Bench Press")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Autofill from last workout" }).ClickAsync();
        await Expect(NumericInputs(page).First).ToHaveValueAsync("10");
        await Expect(NumericInputs(page).Nth(1)).ToHaveValueAsync("135");
    }

    [Fact]
    public async Task FinishWorkout_RedirectsToWorkoutsPage()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithExercisesAsync(user.Id, "Flow Test Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Bench Press")).ToBeVisibleAsync();
        await NextButton(page).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Review" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Finish Workout" }).ClickAsync();
        await page.AsPage().WaitForURLAsync("**/workouts");
    }
}
