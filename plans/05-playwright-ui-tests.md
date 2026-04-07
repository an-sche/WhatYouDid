# Plan: Playwright UI Tests

## Goal
Add end-to-end browser tests using Playwright to catch runtime WASM errors and UI regressions that the existing integration tests miss. The motivating example: a missing `<MudPopoverProvider />` on the workout page caused a crash that only manifested in a real browser — the API-layer integration tests had no way to detect it.

## Context
- Existing tests in `WhatYouDid.Tests` cover the API layer via `WebApplicationFactory` + `TestAuthHandler` + Testcontainers SQL Server. They never boot a real browser or WASM runtime.
- Playwright opens a real Chromium browser against a running server. If the WASM runtime throws on page load, the test fails.
- The existing `ApiWebApplicationFactory` and `TestAuthHandler` infrastructure will be reused — no need to duplicate database setup or auth logic.

## New Project: `WhatYouDid.UITests`

A separate project keeps Playwright's browser binaries and dependencies isolated from the API test project.

**`WhatYouDid.UITests.csproj`:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Playwright" Version="1.*" />
    <PackageReference Include="Testcontainers.MsSql" Version="4.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../WhatYouDid/WhatYouDid/WhatYouDid.csproj" />
    <ProjectReference Include="../WhatYouDid.Tests/WhatYouDid.Tests.csproj" />
  </ItemGroup>
</Project>
```

Reference `WhatYouDid.Tests` to reuse `ApiWebApplicationFactory`, `TestAuthHandler`, `UserHelper`, and other infrastructure directly rather than duplicating them.

After adding the project, build once and then install Playwright browsers:
```bash
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

## Key Challenge: Real HTTP Listener

`WebApplicationFactory`'s default `TestServer` does not bind to a real TCP port — Playwright can't navigate to it. The solution is to override `CreateHost` so the factory spins up a second Kestrel host on a random port alongside the in-process test host.

**`Infrastructure/PlaywrightWebApplicationFactory.cs`:**
```csharp
/// <summary>
/// Extends ApiWebApplicationFactory to also start a real Kestrel listener so
/// Playwright (which needs a real HTTP URL) can reach the server.
/// </summary>
public class PlaywrightWebApplicationFactory : ApiWebApplicationFactory
{
    private IHost _kestrelHost = null!;
    public Uri BaseAddress { get; private set; } = null!;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Let the base class build its in-process TestServer (required for factory internals)
        var testHost = base.CreateHost(builder);

        // Clone the builder configuration onto a real Kestrel host on port 0 (OS picks port)
        builder.ConfigureWebHost(b => b.UseKestrel(opts => opts.ListenLocalhost(0)));
        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var addresses = _kestrelHost.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses;
        BaseAddress = new Uri(addresses.First());

        return testHost;
    }

    public new async Task DisposeAsync()
    {
        await _kestrelHost.StopAsync();
        _kestrelHost.Dispose();
        await base.DisposeAsync();
    }
}
```

## Auth Approach

`TestAuthHandler` authenticates any request that carries an `X-Test-UserId` header. Playwright's `SetExtraHTTPHeadersAsync` injects that header on every request the browser makes — including the initial page navigation, WASM bootstrap fetches, and all `/api/*` calls from WASM JavaScript. No special login endpoint or cookie minting is needed.

```csharp
await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
{
    [TestAuthHandler.UserIdHeader] = userId
});
await page.GotoAsync($"{factory.BaseAddress}workout/{routineId}");
```

## Infrastructure

**`Infrastructure/PlaywrightCollection.cs`** — xUnit collection so tests share one factory + browser instance:
```csharp
[CollectionDefinition("Playwright")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture> { }
```

**`Infrastructure/PlaywrightFixture.cs`** — manages the factory and a single Playwright browser process:
```csharp
public class PlaywrightFixture : IAsyncLifetime
{
    public PlaywrightWebApplicationFactory Factory { get; } = new();
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Factory.InitializeAsync();
        var playwright = await Playwright.CreateAsync();
        Browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();
        await Factory.DisposeAsync();
    }

    /// <summary>
    /// Creates a new browser context + page with X-Test-UserId pre-set.
    /// Each test should call this to get an isolated context.
    /// </summary>
    public async Task<IPage> CreateAuthenticatedPageAsync(string userId)
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            [TestAuthHandler.UserIdHeader] = userId
        });
        return page;
    }
}
```

## Test Structure

```
WhatYouDid.UITests/
  Infrastructure/
    PlaywrightWebApplicationFactory.cs
    PlaywrightFixture.cs
    PlaywrightCollection.cs
  WorkoutPage/
    WorkoutPageSmokeTests.cs
    WorkoutPageFlowTests.cs
```

## What to Test

### Smoke tests (`WorkoutPageSmokeTests.cs`)
These are the highest-value tests — they catch the class of error that motivated this plan.

- `WorkoutPage_Loads_WithoutErrors` — navigate to `/workout/{id}`, assert no error UI (`#blazor-error-ui` is hidden, no `MudAlert` with "error" text), assert the routine name heading is visible.
- `WorkoutPage_WithNoExistingWorkout_ShowsFirstExercise` — asserts the first exercise card is rendered and the progress bar is visible.
- `WorkoutPage_AfterPageLoad_WasmIsActive` — asserts WASM is running (e.g. the Next button is interactive/clickable, not just pre-rendered HTML).

### Flow tests (`WorkoutPageFlowTests.cs`)
These cover the core user journey through a workout.

- `NextButton_AdvancesToNextExercise` — click Next, assert exercise index display changes from "1 of N" to "2 of N".
- `BackButton_IsDisabledOnFirstExercise` — assert the Back button has the MudBlazor disabled attribute on exercise 0.
- `BackButton_AfterNext_ReturnsToFirstExercise` — click Next then Back, assert "1 of N" is shown again.
- `AutofillButton_PopulatesFieldsFromLastWorkout` — requires a saved prior workout with known values; click the history icon button, assert the rep/weight fields are filled.
- `AltToggle_ShowsAndHidesAltSection` — click the [+Alt] button, assert the alt fields appear; click again, assert they disappear.
- `CompleteFlow_SubmitsWorkout` — navigate through all exercises clicking Next each time, reach the Review screen, click "Finish Workout", assert redirect to `/workouts`.

## Locating Elements

Prefer Playwright's `GetByRole` and `GetByText` selectors over CSS selectors — they're more resilient to MudBlazor's generated class names:

```csharp
// Good
await page.GetByRole(AriaRole.Button, new() { Name = "arrow_forward" }).ClickAsync();
await page.GetByText("1 of 3").WaitForAsync();

// Fragile — avoid
await page.Locator(".mud-progress-linear").WaitForAsync();
```

For MudBlazor inputs (which render as `<input>` inside shadow-like wrappers), use:
```csharp
await page.GetByLabel("Reps").FillAsync("10");
```

## Console Error Detection

Add a helper that attaches a console listener before navigation and fails the test if any `console.error` fires. This catches WASM runtime errors that don't necessarily show the `#blazor-error-ui` element:

```csharp
var errors = new List<string>();
page.Console += (_, e) => { if (e.Type == "error") errors.Add(e.Text); };

await page.GotoAsync(url);
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

Assert.Empty(errors);
```

## Running Tests

```bash
# From solution root
dotnet test WhatYouDid.UITests/WhatYouDid.UITests.csproj
```

UI tests are slower than API tests (~5–15s per test due to browser startup and WASM load time). Keep them in a separate project so `dotnet test WhatYouDid.Tests` remains fast.

## CI Integration

UI tests can run on GitHub Actions `ubuntu-latest` (Docker is available for Testcontainers). Run them in their own job to avoid resource contention with the API tests — both spin up SQL Server containers and running them together is heavy.

`playwright.ps1` requires PowerShell (`pwsh`), which is not pre-installed on `ubuntu-latest`. Add an install step before running it.

```yaml
ui-tests:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.x'
    - name: Install PowerShell
      run: |
        sudo apt-get update
        sudo apt-get install -y powershell
    - name: Build
      run: dotnet build WhatYouDid.UITests/WhatYouDid.UITests.csproj
    - name: Install Playwright browsers
      run: pwsh WhatYouDid.UITests/bin/Debug/net10.0/playwright.ps1 install chromium --with-deps
    - name: Run UI tests
      run: dotnet test WhatYouDid.UITests/WhatYouDid.UITests.csproj --logger trx
```

If tests are flaky under CI load, replace `WaitForLoadStateAsync(NetworkIdle)` with `WaitForSelectorAsync` on a specific element — it's more reliable than timing-based waits.

## Out of Scope
- Testing server-rendered pages (Routines, Workouts, Dashboard) — covered by existing integration tests; browser tests add cost without much benefit until those pages go WASM (see plan 03).
- Visual regression / screenshot comparison.
- Mobile viewport / PWA offline testing.
