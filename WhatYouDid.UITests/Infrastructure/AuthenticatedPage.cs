using Microsoft.Playwright;

namespace WhatYouDid.UITests.Infrastructure;

/// <summary>
/// Wraps an <see cref="IPage"/> with its browser context so both are closed when
/// disposed. Use with <c>await using</c> — no try/finally needed in tests.
/// </summary>
public sealed class AuthenticatedPage(IPage page) : IAsyncDisposable
{
    private readonly IPage _page = page;

    public ValueTask DisposeAsync() => new(_page.Context.CloseAsync());

    // Common page interactions used in tests
    public ILocator Locator(string selector, PageLocatorOptions? options = null)
        => _page.Locator(selector, options);

    public ILocator GetByText(string text, PageGetByTextOptions? options = null)
        => _page.GetByText(text, options);

    public ILocator GetByRole(AriaRole role, PageGetByRoleOptions? options = null)
        => _page.GetByRole(role, options);

    public ILocator GetByLabel(string text, PageGetByLabelOptions? options = null)
        => _page.GetByLabel(text, options);

    // Escape hatch for passing to APIs that require IPage directly
    public IPage AsPage() => _page;
}
