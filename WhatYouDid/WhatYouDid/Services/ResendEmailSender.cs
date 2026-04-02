using Microsoft.AspNetCore.Identity;
using Resend;
using WhatYouDid.Data;

namespace WhatYouDid.Services;

public class ResendEmailSender(IResend resend, IConfiguration config) : IEmailSender<ApplicationUser>
{
    private string FromAddress => config["Resend:FromAddress"]!;

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        => SendAsync(email, "Reset your WhatYouDid password", BuildResetEmail(resetLink));

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        => SendAsync(email, "Confirm your WhatYouDid email", BuildConfirmationEmail(confirmationLink));

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        => SendAsync(email, "Your WhatYouDid password reset code", BuildResetCodeEmail(resetCode));

    private async Task SendAsync(string toEmail, string subject, string html)
    {
        var message = new EmailMessage
        {
            From = FromAddress,
            To = [toEmail],
            Subject = subject,
            HtmlBody = html
        };

        await resend.EmailSendAsync(message);
    }

    private static string BuildResetEmail(string resetLink) => $"""
        <h2>Reset your WhatYouDid password</h2>
        <p>Click the link below to reset your password. This link expires in 1 hour.</p>
        <p><a href="{resetLink}">Reset Password</a></p>
        <p>If you didn't request this, you can ignore this email.</p>
        """;

    private static string BuildConfirmationEmail(string confirmationLink) => $"""
        <h2>Confirm your WhatYouDid email</h2>
        <p>Click the link below to confirm your email address.</p>
        <p><a href="{confirmationLink}">Confirm Email</a></p>
        <p>If you didn't create an account, you can ignore this email.</p>
        """;

    private static string BuildResetCodeEmail(string resetCode) => $"""
        <h2>Your WhatYouDid password reset code</h2>
        <p>Use the code below to reset your password:</p>
        <h3>{resetCode}</h3>
        <p>If you didn't request this, you can ignore this email.</p>
        """;
}
