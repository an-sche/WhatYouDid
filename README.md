# WhatYouDid

WhatYouDid is a Blazor + SQL project for logging workouts. The repository contains a Blazor WebAssembly front-end and server-side pieces that communicate with a SQL database to store workout logs.

# Deployment Instructions (Manual)

1. Right click and publish the WhatYouDid (Server) project in Visual Studio
	- Release, net10.0, Self-contained, win-x64
2. Log into the remote server via RDP and copy the published files
3. Shut down the existing IIS website and Application Pool
4. Replace the existing files with the new published files
5. Make sure to preserve the `appsettings.json` file with the correct database connection string, or update it if necessary.
6. Restart the IIS website and Application Pool

# Hosting Information

* Cloudflare Tunnel 
	- Installed on IIS server using winget
	- Running as a windows service
	- Remaining configurations on Cloudflare website
* Can Log into Cloudflare to see tunnel status and metrics

# Configuration


Quick local setup
1. Install prerequisites: .NET 10 SDK
2. Install SQL and create a database. Put that database in `appsettings` under `DevelopmentConnection`
3. Apply migrations to your local database `dotnet ef database update`
4. In UserSecrets: configure the "Admins" section to include your email address for admin access. This is required for accessing admin features in the application.
```
"Admins" : [
  "your@email.here"
]
```
5. Run the application `f5` or `dotnet run`

# Information

Key constraints and priorities
- Target framework: .NET 10. Make sure any changes keep compatibility with .NET 10.
- UI: Blazor (prioritize Blazor-specific patterns over Razor Pages or MVC).
	- Specifically, prefer Radzen Blazor components for UI consistency.
- Platform: WebAssembly client exists in the workspace. Preserve client-server communication patterns.
- Most important feature to preserve: reliable logging of workouts and existing data integrity.

