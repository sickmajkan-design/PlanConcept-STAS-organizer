using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Authentication.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Authentication.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UserDto>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
                     ?? throw new UnauthorizedException("User is not authenticated.");

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(UserMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return user ?? throw new UnauthorizedException("User is not authenticated.");
    }
}
