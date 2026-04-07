using Microsoft.Playwright;
using WhatYouDid.Tests.Infrastructure;

namespace WhatYouDid.UITests.Infrastructure;

public class PlaywrightFixture : IAsyncLifetime
{
    public PlaywrightWebApplicationFactory Factory { get; } = new();
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Factory.InitializeAsync();

        // Accessing Server triggers CreateHost, which starts Kestrel and sets BaseAddress.
        // WebApplicationFactory then casts IServer to TestServer (which fails since Kestrel
        // replaced IServer). The server is already running by that point, so we swallow it.
        try { _ = Factory.Server; }
        catch (InvalidCastException) { }

        var playwright = await Playwright.CreateAsync();
        Browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
            await Browser.CloseAsync();
        await Factory.DisposeAsync();
    }

    /// <summary>
    /// Creates a new isolated browser context and page with the X-Test-UserId header
    /// pre-set on every request. Use with <c>await using</c> to ensure the context
    /// is closed after the test.
    /// </summary>
    public async Task<AuthenticatedPage> CreateAuthenticatedPageAsync(string userId)
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            [TestAuthHandler.UserIdHeader] = userId
        });
        return new AuthenticatedPage(page);
    }

    /// <summary>
    /// Attaches a console error listener to <paramref name="page"/> and returns the
    /// collected errors. External resource load failures (e.g. Google Fonts CDN) are
    /// excluded — only errors relevant to the application are captured.
    /// </summary>
    public static List<string> TrackConsoleErrors(IPage page)
    {
        var errors = new List<string>();
        page.Console += (_, e) =>
        {
            if (e.Type != "error") return;
            // Ignore external resource failures — CDNs are unreachable in the test environment
            if (e.Text.Contains("net::ERR_") || e.Text.StartsWith("Access to font"))
                return;
            errors.Add(e.Text);
        };
        return errors;
    }

    public static List<string> TrackConsoleErrors(AuthenticatedPage page)
        => TrackConsoleErrors(page.AsPage());

    /// <summary>
    /// Navigates to <paramref name="url"/>, waits for network idle, and asserts that
    /// no application console errors occurred. On failure, the full console error text
    /// is included in the assertion message.
    /// </summary>
    public static async Task NavigateAndAssertNoErrorsAsync(IPage page, string url)
    {
        var errors = TrackConsoleErrors(page);
        await page.GotoAsync(url);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.True(errors.Count == 0,
            $"Expected no console errors navigating to {url}. Got:\n{string.Join("\n", errors)}");
    }

    public static Task NavigateAndAssertNoErrorsAsync(AuthenticatedPage page, string url)
        => NavigateAndAssertNoErrorsAsync(page.AsPage(), url);
}
