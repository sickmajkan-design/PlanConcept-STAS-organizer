using System.Security.Cryptography;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Starts the password-reset flow. Always succeeds from the caller's point of
/// view so the endpoint cannot be used to enumerate registered email addresses.
/// </summary>
public record ForgotPasswordCommand : IRequest
{
    public string Email { get; init; } = null!;
}

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.");
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IResetLinkBuilder _resetLinkBuilder;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailSender emailSender,
        IDateTimeProvider dateTimeProvider,
        IResetLinkBuilder resetLinkBuilder,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _dateTimeProvider = dateTimeProvider;
        _resetLinkBuilder = resetLinkBuilder;
        _logger = logger;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive, cancellationToken);

        if (user is null)
        {
            // Deliberately do nothing: the endpoint must not reveal whether the address exists.
            return;
        }

        var utcNow = _dateTimeProvider.UtcNow;

        // Invalidate previous outstanding tokens so only the latest email works.
        var outstandingTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var outstanding in outstandingTokens)
        {
            outstanding.UsedAt = utcNow;
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256(rawToken),
            ExpiresAt = utcNow.Add(TokenLifetime)
        });

        await _context.SaveChangesAsync(cancellationToken);

        var resetLink = _resetLinkBuilder.Build(user.Email, rawToken);

        try
        {
            await _emailSender.SendAsync(
                user.Email,
                "Password reset request",
                BuildEmailBody(resetLink),
                cancellationToken);
        }
        catch (Exception ex)
        {
            // The token is stored, so support can still resend; never surface
            // SMTP problems to an unauthenticated caller.
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
        }
    }

    private static string BuildEmailBody(string resetLink) =>
        $"""
         <p>We received a request to reset your password.</p>
         <p><a href="{resetLink}">Click here to choose a new password</a>. The link is valid for 1 hour.</p>
         <p>If you did not request a password reset, you can safely ignore this email.</p>
         """;
}

/// <summary>Builds the client-facing password reset link from configuration.</summary>
public interface IResetLinkBuilder
{
    string Build(string email, string rawToken);
}
