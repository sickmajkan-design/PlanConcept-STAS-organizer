using AutoMapper;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Security;
using Construction.Application.Features.Users.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Users.Commands.ActivateUser;

/// <summary>
/// Restores access to a deactivated account — a returning seasonal worker, or
/// an offboarding done by mistake.
/// </summary>
public record ActivateUserCommand(Guid Id) : IRequest<UserDto>;

public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public ActivateUserCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var callerRole = _currentUserService.Role
            ?? throw new UnauthorizedException("User is not authenticated.");

        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("User", request.Id);

        RoleAdministration.EnsureCanManage(callerRole, user.Role);

        user.IsActive = true;

        // Someone locked out by failed sign-ins before being deactivated would
        // otherwise still be locked out on return.
        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;

        await _context.SaveChangesAsync(cancellationToken);

        // Sessions revoked at deactivation stay revoked; the account signs in
        // again rather than resuming where it left off.
        return _mapper.Map<UserDto>(user);
    }
}
