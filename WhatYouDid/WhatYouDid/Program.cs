using Microsoft.AspNetCore.Components;
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
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
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
    var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var hasPendingMigrations = db.Database.GetPendingMigrations().Any();
    if (hasPendingMigrations)
    {
        var backupPath = app.Configuration["DatabaseBackupPath"]
            ?? throw new InvalidOperationException(
                "DatabaseBackupPath is not configured. Add it to appsettings.json.");

        if (!Directory.Exists(backupPath))
            throw new InvalidOperationException($"Backup directory does not exist: {backupPath}");

        var connection = db.Database.GetDbConnection();
        var databaseName = connection.Database;
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        static string MigName(string? migrationId) =>
            migrationId is null ? "init"
            : migrationId.Contains('_') ? migrationId[(migrationId.IndexOf('_') + 1)..] : migrationId;

        var currMig = MigName(db.Database.GetAppliedMigrations().LastOrDefault());
        var tgtMig = MigName(db.Database.GetPendingMigrations().Last());
        var backupFile = Path.Combine(backupPath, $"{databaseName}_{timestamp}_curr-{currMig}_to-{tgtMig}.bak");

        connection.Open();
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"BACKUP DATABASE [{databaseName}] TO DISK = @backupFile WITH FORMAT, INIT"; //, COMPRESSION (when using full sql)
            cmd.CommandTimeout = 300; // 5 minutes
            var param = cmd.CreateParameter();
            param.ParameterName = "@backupFile";
            param.Value = backupFile;
            cmd.Parameters.Add(param);
            cmd.ExecuteNonQuery();
        }
        finally
        {
            connection.Close();
        }

        Console.WriteLine($"[Backup] Completed before migration: {backupFile}");
        
        db.Database.Migrate();
    }

}

app.UseHttpsRedirection();
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
