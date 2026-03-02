using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;
using WhatYouDid.Client.Pages;
using WhatYouDid.Components;
using WhatYouDid.Components.Account;
using WhatYouDid.Data;
using WhatYouDid.EndpointExtensions;
using WhatYouDid.Middleware;
using WhatYouDid.Services;
using WhatYouDid.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRadzenComponents();

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
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddTransient<IWhatYouDidApi, WhatYouDidApiDirectAccess>();
builder.Services.AddScoped<IBrowserStorage, ServerBrowserStorage>();
builder.Services.AddHostedService<AdminRoleSeeder>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    // Migrate the production server.
    app.Services.GetRequiredService<ApplicationDbContext>().Database.Migrate();
}

app.UseHttpsRedirection();

// Resolve tenant from authenticated user on each HTTP request so the
// scoped ITenantService has the correct tenant id before EF DbContext
// and other services run.
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StartWorkout).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Map minimal API endpoints used by the WASM client
app.MapRoutineEndpoints();

app.Run();
