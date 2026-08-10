using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Application.Common.Validation;
using Construction.Application.Features.Users.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Users.Commands.CreateUser;

/// <summary>Creates a sign-in account, optionally tied to an employee record.</summary>
public record CreateUserCommand : IRequest<UserDto>
{
    public string Email { get; init; } = null!;

    public string Password { get; init; } = null!;

    public UserRole Role { get; init; }

    /// <summary>
    /// Links the account to a personnel record. Required for anyone who uses
    /// the mobile app: GPS reporting identifies the employee from this link,
    /// and an account without it is refused by those endpoints.
    /// </summary>
    public Guid? EmployeeId { get; init; }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(256);

        RuleFor(x => x.Password).StrongPassword();

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role is not a known role.");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var callerRole = _currentUserService.Role
            ?? throw new UnauthorizedException("User is not authenticated.");

        RoleAdministration.EnsureCanAssign(callerRole, request.Role);

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new ConflictException($"An account for '{email}' already exists.");
        }

        var employee = await ResolveEmployeeAsync(request.EmployeeId, cancellationToken);

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true,
            EmployeeId = employee?.Id
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        user.Employee = employee;

        return UserMapping.ToDto(user);
    }

    private async Task<Employee?> ResolveEmployeeAsync(Guid? employeeId, CancellationToken cancellationToken)
    {
        if (employeeId is not { } id)
        {
            return null;
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), id);

        // The database enforces this too; catching it here turns a constraint
        // violation into a message that says what to do about it.
        if (await _context.Users.AnyAsync(u => u.EmployeeId == id, cancellationToken))
        {
            throw new ConflictException(
                $"{employee.FirstName} {employee.LastName} already has an account.");
        }

        return employee;
    }
}
