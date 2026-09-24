using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Invitations;

/// <summary>What the person opening a link needs to see before choosing a password.</summary>
public record InvitationPreviewDto(string FirstName, string? SuggestedEmail, DateTime ExpiresAt);

/// <summary>Checks a link without using it.</summary>
/// <remarks>
/// Anyone with the link can call this, so it answers "not found" for every
/// way a link can be unusable — unknown, used, expired, or its employee gone —
/// and never says which. It returns only a first name.
/// </remarks>
public record GetInvitationQuery(string Token) : IRequest<InvitationPreviewDto>;

public class GetInvitationQueryHandler : IRequestHandler<GetInvitationQuery, InvitationPreviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetInvitationQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<InvitationPreviewDto> Handle(
        GetInvitationQuery request,
        CancellationToken cancellationToken)
    {
        var hash = InvitationTokens.Hash(request.Token);
        var utcNow = _dateTimeProvider.UtcNow;

        var invitation = await _context.EmployeeInvitations
            .AsNoTracking()
            .Where(i => i.TokenHash == hash && i.UsedAt == null && i.ExpiresAt > utcNow)
            .Select(i => new { i.Employee.FirstName, i.Employee.Email, i.ExpiresAt })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(EmployeeInvitation), "link");

        return new InvitationPreviewDto(invitation.FirstName, invitation.Email, invitation.ExpiresAt);
    }
}
