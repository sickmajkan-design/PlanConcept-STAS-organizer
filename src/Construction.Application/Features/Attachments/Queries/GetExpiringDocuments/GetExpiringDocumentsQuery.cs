using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Queries.GetExpiringDocuments;

/// <summary>
/// Documents that have lapsed or are about to.
/// </summary>
/// <remarks>
/// The whole reason expiry dates are stored. A certificate that quietly ran
/// out three months ago is a person who should not have been on site, and
/// nobody finds that by opening employee records one at a time.
/// </remarks>
public record GetExpiringDocumentsQuery : IRequest<IReadOnlyList<AttachmentDto>>
{
    public const int DefaultWithinDays = 30;

    public const int MaxWithinDays = 365;

    /// <summary>How far ahead to look. Already-expired documents are always included.</summary>
    public int WithinDays { get; init; } = DefaultWithinDays;
}

public class GetExpiringDocumentsQueryValidator : AbstractValidator<GetExpiringDocumentsQuery>
{
    public GetExpiringDocumentsQueryValidator()
    {
        RuleFor(x => x.WithinDays)
            .InclusiveBetween(0, GetExpiringDocumentsQuery.MaxWithinDays)
            .WithMessage(
                $"The window must be between 0 and {GetExpiringDocumentsQuery.MaxWithinDays} days.");
    }
}

public class GetExpiringDocumentsQueryHandler
    : IRequestHandler<GetExpiringDocumentsQuery, IReadOnlyList<AttachmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetExpiringDocumentsQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<AttachmentDto>> Handle(
        GetExpiringDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var cutoff = DateOnly
            .FromDateTime(_dateTimeProvider.UtcNow)
            .AddDays(request.WithinDays);

        return await _context.Attachments
            .AsNoTracking()
            .Where(a => a.ExpiresAt != null && a.ExpiresAt <= cutoff)
            // Soonest — which means most-overdue — first.
            .OrderBy(a => a.ExpiresAt)
            .Select(AttachmentMapping.Projection)
            .ToListAsync(cancellationToken);
    }
}
