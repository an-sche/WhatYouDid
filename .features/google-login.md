# Google Login (OAuth)

## Overview
Users can sign in with their Google account as an alternative to email/password. The login page automatically shows a Google button via `ExternalLoginPicker`, which is driven by whatever external providers are registered in `Program.cs` — no UI changes are needed to add or remove providers.

## Account Linking
If a Google email matches an existing local account, the Google login is automatically linked to that account on first use. The user can then sign in either way. New Google users who have no existing account get an account created automatically with email already confirmed — Google has already verified it.

## Cloudflare Tunnel
The app sits behind a Cloudflare Tunnel which terminates TLS, meaning the app sees plain HTTP internally. Without intervention, OAuth redirect URIs would be built with `http://` and Google would reject them. A small middleware at the top of the pipeline reads `X-Forwarded-Proto` from Cloudflare and rewrites the request scheme so everything downstream sees `https`.

The redirect URI registered in Google Cloud Console must match exactly what the app sends — no `www.` prefix unless the public URL uses one.

## Configuration
| Key | Description |
|-----|-------------|
| `Authentication:Google:ClientId` | OAuth 2.0 Client ID from Google Cloud Console |
| `Authentication:Google:ClientSecret` | OAuth 2.0 Client Secret from Google Cloud Console |

Set via User Secrets in development, `appsettings.json` on the production server.

## Google Cloud Console Setup
- APIs & Services → OAuth consent screen (External)
- APIs & Services → Credentials → OAuth 2.0 Client ID (Web application)
- Authorized redirect URI: `https://what-you-did.com/signin-google`
- Redirect URIs can be added or edited at any time without regenerating credentials
