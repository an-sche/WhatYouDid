using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace WhatYouDid.ServiceExtensions;

public static class RateLimitingServiceExtensions
{
    public static IServiceCollection AddAccountRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (context.Request.Method != HttpMethods.Post)
                    return RateLimitPartition.GetNoLimiter("");

                // CF-Connecting-IP is set by Cloudflare to the real client IP and cannot be
                // spoofed — Cloudflare strips any client-supplied version before forwarding.
                // Prefer this over X-Forwarded-For (which can be chained/faked) or
                // RemoteIpAddress (which would be the cloudflared daemon's loopback address).
                var ip = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";
                var path = context.Request.Path.Value ?? "";

                return path.ToLowerInvariant() switch
                {
                    // Brute-force login backstop (lockout handles the real protection)
                    "/account/login" =>
                        Window($"login:{ip}", limit: 10, TimeSpan.FromMinutes(1)),

                    // Account creation — rare; main bot signup vector
                    "/account/register" =>
                        Window($"register:{ip}", limit: 3, TimeSpan.FromHours(1)),

                    // Email senders — prevent email bombing
                    "/account/forgotpassword" =>
                        Window($"forgot:{ip}", limit: 5, TimeSpan.FromHours(1)),
                    "/account/resendemailconfirmation" =>
                        Window($"resend:{ip}", limit: 5, TimeSpan.FromHours(1)),

                    // Token-based reset — single-use tokens, but still limit enumeration
                    "/account/resetpassword" =>
                        Window($"reset:{ip}", limit: 10, TimeSpan.FromHours(1)),

                    // 2FA — TOTP brute-force and recovery code protection
                    "/account/loginwith2fa" =>
                        Window($"2fa:{ip}", limit: 10, TimeSpan.FromMinutes(1)),
                    "/account/loginwithrecoverycode" =>
                        Window($"recovery:{ip}", limit: 5, TimeSpan.FromHours(1)),

                    _ => RateLimitPartition.GetNoLimiter("")
                };
            });
        });

        return services;
    }

    private static RateLimitPartition<string> Window(string key, int limit, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit,
            Window = window,
        });
}
