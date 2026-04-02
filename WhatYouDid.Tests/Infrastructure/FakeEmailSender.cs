using Microsoft.AspNetCore.Identity;

namespace WhatYouDid.Tests.Infrastructure;

public class FakeEmailSender : IEmailSender<ApplicationUser>
{
    public List<(string Email, string Subject, string Body)> SentEmails { get; } = [];

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        SentEmails.Add((email, "Reset your WhatYouDid password", resetLink));
        return Task.CompletedTask;
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        SentEmails.Add((email, "Confirm your WhatYouDid email", confirmationLink));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        SentEmails.Add((email, "Your WhatYouDid password reset code", resetCode));
        return Task.CompletedTask;
    }
}
