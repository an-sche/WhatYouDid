using Microsoft.Playwright;

namespace WhatYouDid.UITests.WorkoutPage;

[Collection("Playwright")]
public class WorkoutPageFlowTests(PlaywrightFixture fixture)
{
    private static ILocator NextButton(AuthenticatedPage page)
        => page.GetByRole(AriaRole.Button, new() { Name = "Next exercise" });

    private static ILocator BackButton(AuthenticatedPage page)
        => page.GetByRole(AriaRole.Button, new() { Name = "Previous exercise" });

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
    public async Task AllFieldTypes_AcceptInput()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithAllFieldTypesAsync(user.Id, "Full Field Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Full Body Exercise")).ToBeVisibleAsync();

        await page.GetByLabel("Reps").First.FillAsync("10");
        await page.GetByLabel("Weight").First.FillAsync("135");
        await page.GetByLabel("Duration").First.FillAsync("30");

        await Expect(page.GetByLabel("Reps").First).ToHaveValueAsync("10");
        await Expect(page.GetByLabel("Weight").First).ToHaveValueAsync("135");
        await Expect(page.GetByLabel("Duration").First).ToHaveValueAsync("30");
    }

    [Fact]
    public async Task AltToggle_ShowsAltSection()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithAllFieldTypesAsync(user.Id, "Full Field Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Full Body Exercise")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Alt" }).First.ClickAsync();

        await Expect(page.GetByLabel("Alt Reps").First).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Alt Weight").First).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Alt Duration").First).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Note").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AltToggle_HidesAltSection_WhenClickedAgain()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, _) = await fixture.Factory.CreateRoutineWithAllFieldTypesAsync(user.Id, "Full Field Routine");

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Full Body Exercise")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Alt" }).First.ClickAsync();
        await Expect(page.GetByLabel("Alt Reps").First).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Alt" }).First.ClickAsync();
        await Expect(page.GetByLabel("Alt Reps").First).ToBeHiddenAsync();
        await Expect(page.GetByLabel("Alt Weight").First).ToBeHiddenAsync();
        await Expect(page.GetByLabel("Alt Duration").First).ToBeHiddenAsync();
        await Expect(page.GetByLabel("Note").First).ToBeHiddenAsync();
    }

    [Fact]
    public async Task AutofillButton_PopulatesFromPreviousWorkout()
    {
        var user = await fixture.Factory.CreateUserAsync($"{Guid.NewGuid()}@test.com", "Test123!");
        var (routineId, exerciseId) = await fixture.Factory.CreateRoutineWithAllFieldTypesAsync(user.Id, "Autofill Test Routine");
        await fixture.Factory.SaveWorkoutWithSetsAsync(
            user.Id, routineId, "Autofill Test Routine",
            exerciseId: exerciseId, exerciseName: "Full Body Exercise",
            reps: 10, weight: 135, duration: 60);

        await using var page = await fixture.CreateAuthenticatedPageAsync(user.Id);
        await PlaywrightFixture.NavigateAndAssertNoErrorsAsync(page,
            $"{fixture.Factory.BaseAddress}workout/{routineId}");

        await Expect(page.GetByText("Full Body Exercise")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Autofill from last workout" }).ClickAsync();
        await Expect(page.GetByLabel("Reps").First).ToHaveValueAsync("10");
        await Expect(page.GetByLabel("Weight").First).ToHaveValueAsync("135");
        await Expect(page.GetByLabel("Duration").First).ToHaveValueAsync("60");
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
        await Expect(page.GetByText("Flow Test Routine")).ToBeVisibleAsync();
    }
}
