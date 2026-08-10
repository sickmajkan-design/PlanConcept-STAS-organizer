using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Application.Features.Users.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Changes an account's email, role or employee link.
///
/// Active/inactive is deliberately not here — offboarding goes through its own
/// endpoint so the intent is explicit in the audit trail and so it can carry
/// the session revocation that a field update would not.
/// </summary>
public record UpdateUserCommand : IRequest<UserDto>
{
    public Guid Id { get; init; }

    public string Email { get; init; } = null!;

    public UserRole Role { get; init; }

    public Guid? EmployeeId { get; init; }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(256);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role is not a known role.");
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var callerRole = _currentUserService.Role
            ?? throw new UnauthorizedException("User is not authenticated.");

        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        // Both sides are checked: the role the account holds now, and the role
        // it would hold afterwards. Checking only one would let an Admin
        // promote a Worker to Super Admin, or edit an account above them.
        RoleAdministration.EnsureCanManage(callerRole, user.Role);
        RoleAdministration.EnsureCanAssign(callerRole, request.Role);

        var roleChanged = user.Role != request.Role;

        if (roleChanged && user.Id == _currentUserService.UserId)
        {
            throw new ConflictException("You cannot change your own role.");
        }

        if (roleChanged && user.Role == UserRole.SuperAdmin)
        {
            await EnsureAnotherSuperAdminRemainsAsync(user.Id, cancellationToken);
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (email != user.Email &&
            await _context.Users.AnyAsync(u => u.Email == email && u.Id != user.Id, cancellationToken))
        {
            throw new ConflictException($"An account for '{email}' already exists.");
        }

        user.Email = email;
        user.Role = request.Role;
        user.Employee = await ResolveEmployeeAsync(user, request.EmployeeId, cancellationToken);
        user.EmployeeId = user.Employee?.Id;

        if (roleChanged)
        {
            // The role travels inside the access token, so a demotion would
            // otherwise keep its old permissions until that token expired and
            // the refresh handed out a new one. Revoking forces a fresh sign-in.
            await RevokeSessionsAsync(user.Id, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return UserMapping.ToDto(user);
    }

    private async Task<Employee?> ResolveEmployeeAsync(
        User user,
        Guid? employeeId,
        CancellationToken cancellationToken)
    {
        if (employeeId is not { } id)
        {
            return null;
        }

        if (user.EmployeeId == id)
        {
            return user.Employee;
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), id);

        if (await _context.Users.AnyAsync(u => u.EmployeeId == id && u.Id != user.Id, cancellationToken))
        {
            throw new ConflictException(
                $"{employee.FirstName} {employee.LastName} already has an account.");
        }

        return employee;
    }

    private async Task RevokeSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;

        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = utcNow;
            token.RevokedByIp = _currentUserService.IpAddress;
        }
    }

    private async Task EnsureAnotherSuperAdminRemainsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var othersRemain = await _context.Users
            .AnyAsync(u => u.Id != userId && u.Role == UserRole.SuperAdmin && u.IsActive, cancellationToken);

        if (!othersRemain)
        {
            throw new ConflictException(
                "This is the only active Super Admin. Promote another account first.");
        }
    }
}
