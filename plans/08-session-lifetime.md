# Plan: Session Lifetime / Remember Me

## Goal
Keep users logged in longer, and make the "Remember me" checkbox on the login page meaningful.

## Context
- The login page has a "Remember me" checkbox, but it currently has no effect — there is no `ConfigureApplicationCookie` call, so ASP.NET uses its default cookie settings regardless of whether `isPersistent` is true or false.
- `DataProtectionTokenProviderOptions.TokenLifespan` (currently 1 hour) controls password reset and email confirmation token expiry only — it has nothing to do with session lifetime.
- `LoginUser()` in `Login.razor` already passes `Input.RememberMe` to `PasswordSignInAsync`. The value just has nowhere to go.

## Decision Required
Two reasonable approaches:

**Option A — Single long lifetime for everyone**
Set a long `ExpireTimeSpan` (e.g. 30 days) with `SlidingExpiration = true`. Everyone stays logged in as long as they're active. "Remember me" checkbox becomes irrelevant and could be removed.

**Option B — Short default, long when "Remember me" is checked**
Set a short default (e.g. 1 day) and override to a longer lifetime (e.g. 30 days) when the user checks "Remember me". This is the standard pattern users expect.

## Implementation

### Both options: add to Program.cs after `AddIdentity`
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});
```

### Option B only: honour the checkbox
The cookie lifetime can't be set per-request via `ConfigureApplicationCookie`. Instead, after a successful login in `Login.razor`, sign the user out and back in with an explicit `AuthenticationProperties`:

```csharp
var authProperties = new AuthenticationProperties
{
    IsPersistent = Input.RememberMe,
    ExpiresUtc = Input.RememberMe
        ? DateTimeOffset.UtcNow.AddDays(30)
        : DateTimeOffset.UtcNow.AddHours(8)
};
await SignInManager.SignInWithClaimsAsync(user, authProperties, claims);
```

This requires a small refactor of `LoginUser()` in `Login.razor`.

## Files Touched
- `Program.cs` — `ConfigureApplicationCookie`
- `Components/Account/Pages/Login.razor` — only if implementing Option B
