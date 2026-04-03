# Progressive Web App (PWA)

## Overview
The app is installable as a PWA on Android and iOS, allowing users to add it to their home screen and launch it fullscreen like a native app.

## Key Files
| File | Purpose |
|---|---|
| `wwwroot/manifest.webmanifest` | App manifest (name, icons, display mode, theme color) |
| `wwwroot/service-worker.js` | Service worker — caching and update notifications |
| `wwwroot/icon192x192.png` | Home screen icon (Android) |
| `wwwroot/icon512x512.png` | Splash screen / high-res icon |
| `Components/App.razor` | Manifest link, theme-color meta, SW registration, update banner |

## Browser Support
| Browser | Install prompt | Service worker |
|---|---|---|
| Chrome / Chromium (desktop & Android) | Yes | Yes |
| Edge (desktop & Android) | Yes | Yes |
| Samsung Internet (Android) | Yes | Yes |
| Safari (iOS) | Manual via Share → Add to Home Screen | Yes |
| Firefox (desktop) | No | Yes |
| DuckDuckGo (Android) | No | Yes |

## Deployment Notes
HTTPS is required for PWA install prompts. This is satisfied automatically by the Cloudflare Tunnel.

## More Info
- Manifest uses `display: standalone` and theme color `#6DC21A` (pterodactyl green)
- Service worker uses network-first caching; `/api/*` and `/_framework/*` are always fetched from the network
- `skipWaiting()` activates new service worker versions immediately; an update banner appears at the bottom of the screen prompting the user to reload
