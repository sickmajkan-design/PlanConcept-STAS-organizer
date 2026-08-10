using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Users.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Users.Queries.GetUserById;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.Id)
            .Select(UserMapping.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return user ?? throw new NotFoundException("User", request.Id);
    }
}
