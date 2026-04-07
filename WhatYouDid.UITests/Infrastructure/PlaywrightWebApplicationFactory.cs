using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using WhatYouDid.Tests.Infrastructure;

namespace WhatYouDid.UITests.Infrastructure;

/// <summary>
/// Extends ApiWebApplicationFactory to also start a real Kestrel listener so
/// Playwright (which needs a real HTTP URL) can reach the server.
///
/// UseKestrel is added after UseTestServer, so Kestrel becomes the active IServer
/// and binds to a real port. TestServer remains registered in DI so the base
/// WebApplicationFactory internals continue to work.
/// </summary>
public class PlaywrightWebApplicationFactory : ApiWebApplicationFactory
{
    public Uri BaseAddress { get; private set; } = null!;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureWebHost(b => b.UseKestrel(opts => opts.Listen(IPAddress.Loopback, 0)));

        var host = base.CreateHost(builder);

        var server = host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()!.Addresses;
        BaseAddress = new Uri(addresses.First());

        return host;
    }
}
