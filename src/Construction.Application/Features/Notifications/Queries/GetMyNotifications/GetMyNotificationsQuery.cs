using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Notifications.Models;
using FluentValidation;
using MediatR;

namespace Construction.Application.Features.Notifications.Queries.GetMyNotifications;

/// <summary>The current user's notification inbox, newest first.</summary>
public record GetMyNotificationsQuery : IRequest<PagedList<NotificationDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public bool UnreadOnly { get; init; }
}

public class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    public GetMyNotificationsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}

public class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, PagedList<NotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetMyNotificationsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PagedList<NotificationDto>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("User is not authenticated.");

        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (request.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await PagedList<NotificationDto>.CreateAsync(
            query
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .ProjectTo<NotificationDto>(_mapper.ConfigurationProvider),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
