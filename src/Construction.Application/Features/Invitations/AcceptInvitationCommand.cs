using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Validation;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Invitations;

/// <summary>Turns an invitation link into a sign-in account for the invited employee.</summary>
public record AcceptInvitationCommand : IRequest<string>
{
    public string Token { get; init; } = null!;

    public string Email { get; init; } = null!;

    public string Password { get; init; } = null!;
}

public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(128);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(256);

        RuleFor(x => x.Password).StrongPassword();
    }
}

/// <summary>Returns the email the account was created with.</summary>
public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcceptInvitationCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<string> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var hash = InvitationTokens.Hash(request.Token);
        var utcNow = _dateTimeProvider.UtcNow;

        // Same answer for every way the link can be unusable.
        var invitation = await _context.EmployeeInvitations
            .Include(i => i.Employee)
            .FirstOrDefaultAsync(
                i => i.TokenHash == hash && i.UsedAt == null && i.ExpiresAt > utcNow,
                cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeInvitation), "link");

        if (await _context.Users.AnyAsync(u => u.EmployeeId == invitation.EmployeeId, cancellationToken))
        {
            throw new ConflictException("This employee already has an account.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new ConflictException($"An account for '{email}' already exists.");
        }

        _context.Users.Add(new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = invitation.Role,
            IsActive = true,
            EmployeeId = invitation.EmployeeId,
        });

        invitation.UsedAt = utcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return email;
    }
}
