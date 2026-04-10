using Microsoft.AspNetCore.Components;
using WhatYouDid.ServiceExtensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using MudExtensions.Services;
using WhatYouDid.Client.Pages;
using WhatYouDid.Components;
using WhatYouDid.Components.Account;
using WhatYouDid.Data;
using Scalar.AspNetCore;
using WhatYouDid.EndpointExtensions;
using WhatYouDid.Middleware;
using WhatYouDid.Services;
using WhatYouDid.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMudServices();
builder.Services.AddMudExtensions();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Configure server-side Blazor SignalR hub options to be more tolerant of
// mobile devices that sleep or have intermittent connectivity.
builder.Services
    .AddServerSideBlazor(options =>
    {
        // Retain disconnected circuits for a longer period to allow reconnect from
        // mobile devices that may be suspended for several minutes.
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10);
    })
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(3);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.HandshakeTimeout = TimeSpan.FromSeconds(60);
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

// Tenant resolution service for multi-tenancy
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<CircuitHandler, TenantCircuitHandler>();

string connectionString;
if (builder.Environment.IsDevelopment())
{
	connectionString = builder.Configuration.GetConnectionString("DevelopmentConnection") 
        ?? throw new InvalidOperationException("Connection string 'DevelopmentConnection' not found.");

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}
else
{
	connectionString = builder.Configuration.GetConnectionString("ProductionConnection") 
        ?? throw new InvalidOperationException("Connection string 'ProductionConnection' not found.");
}

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString),
    ServiceLifetime.Scoped);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(1);
});

builder.Services.AddValidation();
builder.Services.AddOpenApi();
builder.Services.AddResendEmail(builder.Configuration);
builder.Services.AddAccountRateLimiting();

builder.Services.AddTransient<IRoutineService, RoutineService>();
builder.Services.AddTransient<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IBrowserStorage, ServerBrowserStorage>();
builder.Services.AddHostedService<AdminRoleSeeder>();
builder.Services.AddHostedService<DevDataSeeder>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

var app = builder.Build();

// Trust the X-Forwarded-Proto header from Cloudflare so OAuth redirect URIs
// are built with https:// even though IIS receives plain HTTP from the tunnel.
// Cloudflare Tunnel terminates TLS and forwards X-Forwarded-Proto: https.
// Rewrite the request scheme so OAuth redirect URIs are built with https://.
app.Use((context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto))
        context.Request.Scheme = proto!;
    return next(context);
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    // Migrate the production server, with a pre-migration backup.
    app.BackupAndMigrateSqlDatabase();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Resolve tenant from authenticated user on each HTTP request so the
// scoped ITenantService has the correct tenant id before EF DbContext
// and other services run.
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapStaticAssets();
app.UseAntiforgery();

// Workaround for .NET 10.0.3 SDK bug: Microsoft.DotNet.HotReload.WebAssembly.Browser
// is listed in the WASM JS module initializer manifest but is absent from publish
// endpoints, causing WASM to fail with a MIME type error in production.
// In development the static assets endpoint (exact match) takes priority over this.
app.MapGet("_content/Microsoft.DotNet.HotReload.WebAssembly.Browser/{**path}",
    () => Results.Content("export {};", "text/javascript"));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StartWorkout).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapApiEndpoints();

app.Run();
