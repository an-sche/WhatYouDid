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

In UserSecrets: configure the "Admins" section to include your email address for admin access. This is required for accessing admin features in the application.
```
"Admins" : [
  "your@email.here"
]
```

# Information

Key constraints and priorities
- Target framework: .NET 10. Make sure any changes keep compatibility with .NET 10.
- UI: Blazor (prioritize Blazor-specific patterns over Razor Pages or MVC).
	- Specifically, prefer Radzen Blazor components for UI consistency.
- Platform: WebAssembly client exists in the workspace. Preserve client-server communication patterns.
- Most important feature to preserve: reliable logging of workouts and existing data integrity.

Quick local setup
1. Install prerequisites: .NET 10 SDK and any required workloads for Blazor WebAssembly (if needed): `dotnet workload install wasm-tools` (optional).
2. Clone the repo and restore packages: `git clone <repo> && cd <repo> && dotnet restore`.
3. Inspect configuration: open `appsettings.json` or environment-specific files to find the database connection string. Do not commit secrets.
4. If the project uses EF Core migrations, apply them from the server / data project: `dotnet ef database update -p <ServerProject> -s <ServerProject>` (only if migrations exist).
5. Run the solution or projects: `dotnet build` then `dotnet run` from the appropriate project directory (usually the server project). For client-only work, run the Blazor WebAssembly client project.

Where to look first (for AI)
- Project layout to check: `Client`, `Server`, `Shared`, `Data`, `Pages`, `Components`, `wwwroot`.
- API endpoints and controllers: check `Server/Controllers` or `Pages` that expose workout APIs.
- Database models and DbContext: look for `*DbContext*` in a `Data` or `Server` project.
- UI components that log workouts: check `Client/Pages`, `Client/Components` and search for words like `Workout`, `Log`, `Exercise`.

Development rules for AI changes
- Make minimal, well-scoped changes that preserve existing behavior unless the user requests a breaking change.
- Run `dotnet build` and `dotnet test` after making changes. Fix compile/test errors before committing.
- If changing the database schema, include EF Core migrations and migration commands, and document any required migration steps.
- Keep secrets and production connection strings out of the repo. Use user secrets or environment variables.
- Add tests for new behavior; prefer small focused tests.

Producing patches and PRs
- Create a branch per feature or fix, include a short description, and reference relevant issue/bug.
- Keep PRs small and focused; include screenshots or curl examples for API changes when applicable.

If you (the AI) are asked to modify or extend behavior, always ask or record:
- Is changing the DB schema allowed?
- Should the UI/UX remain unchanged?
- Any backwards-compatibility requirements?

Contact
For project-specific questions, inspect the repository code and open an issue describing the desired change.
