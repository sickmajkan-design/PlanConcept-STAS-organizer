using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Invitations;

/// <summary>What the administrator is handed: the token, shown once.</summary>
public record EmployeeInvitationDto(
    Guid EmployeeId,
    string EmployeeName,
    string Token,
    DateTime ExpiresAt,
    string? SuggestedEmail);

/// <summary>Creates a one-time sign-up link for an employee who has no account yet.</summary>
/// <remarks>
/// Replaces any earlier open invitation for the same employee, so there is
/// only ever one working link per person and a link sent to the wrong number
/// can be killed by simply creating a new one.
/// </remarks>
public record CreateEmployeeInvitationCommand : IRequest<EmployeeInvitationDto>
{
    public Guid EmployeeId { get; init; }

    public UserRole Role { get; init; } = UserRole.Worker;
}

public class CreateEmployeeInvitationCommandValidator : AbstractValidator<CreateEmployeeInvitationCommand>
{
    public CreateEmployeeInvitationCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();

        RuleFor(x => x.Role)
            .IsInEnum()
            .NotEqual(UserRole.Customer).WithMessage("A customer cannot be invited as an employee.");
    }
}

public class CreateEmployeeInvitationCommandHandler
    : IRequestHandler<CreateEmployeeInvitationCommand, EmployeeInvitationDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateEmployeeInvitationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EmployeeInvitationDto> Handle(
        CreateEmployeeInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var callerRole = _currentUserService.Role
            ?? throw new UnauthorizedException("User is not authenticated.");

        RoleAdministration.EnsureCanAssign(callerRole, request.Role);

        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        if (await _context.Users.AnyAsync(u => u.EmployeeId == employee.Id, cancellationToken))
        {
            throw new ConflictException($"{employee.FullName} already has an account.");
        }

        var utcNow = _dateTimeProvider.UtcNow;

        var open = await _context.EmployeeInvitations
            .Where(i => i.EmployeeId == employee.Id && i.UsedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var earlier in open)
        {
            earlier.UsedAt = utcNow;
        }

        var token = InvitationTokens.Generate();
        var expiresAt = utcNow.Add(InvitationTokens.Lifetime);

        _context.EmployeeInvitations.Add(new EmployeeInvitation
        {
            EmployeeId = employee.Id,
            TokenHash = InvitationTokens.Hash(token),
            Role = request.Role,
            ExpiresAt = expiresAt,
            CreatedByUserId = _currentUserService.UserId,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new EmployeeInvitationDto(employee.Id, employee.FullName, token, expiresAt, employee.Email);
    }
}
