using Microsoft.AspNetCore.Identity;
using Resend;
using WhatYouDid.Data;
using WhatYouDid.Services;

namespace WhatYouDid.ServiceExtensions;

public static class ResendServiceExtensions
{
    public static IServiceCollection AddResendEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = configuration["Resend:ApiKey"]!;
        });
        services.AddTransient<IResend, ResendClient>();
        services.AddTransient<IEmailSender<ApplicationUser>, ResendEmailSender>();

        return services;
    }
}
