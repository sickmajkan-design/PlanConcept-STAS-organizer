using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Attachments.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Attachments.Queries.GetExpiringDocuments;

/// <summary>
/// Documents that have lapsed or are about to — or, with
/// <see cref="IncludeUndated"/> set, every document filed anywhere in the
/// company, dated or not.
/// </summary>
/// <remarks>
/// The narrow case is the whole reason expiry dates are stored. A certificate
/// that quietly ran out three months ago is a person who should not have
/// been on site, and nobody finds that by opening employee records one at a
/// time. The broad case answers a different, simpler question — "what have
/// we ever uploaded" — that happens to reuse the exact same shape once the
/// expiry filter is optional.
/// </remarks>
public record GetExpiringDocumentsQuery : IRequest<IReadOnlyList<AttachmentDto>>
{
    public const int MaxWithinDays = 365;

    /// <summary>
    /// How far ahead to look. Null — including simply omitting the parameter —
    /// means every document with an expiry date, however far out.
    /// Already-expired documents are always included either way. There is
    /// deliberately no server-side default: the caller states what it wants
    /// rather than a day count changing meaning depending on whether it was
    /// typed or assumed.
    /// </summary>
    public int? WithinDays { get; init; }

    /// <summary>
    /// Also include documents with no expiry date at all — a plain photo, a
    /// site document nobody dated. Off by default so the existing "what's
    /// lapsing" view stays exactly what it was; the whole-company "every
    /// document we've ever filed" view turns this on and ignores
    /// <see cref="WithinDays"/>, since a window has no meaning for a document
    /// that never expires.
    /// </summary>
    public bool IncludeUndated { get; init; }
}

public class GetExpiringDocumentsQueryValidator : AbstractValidator<GetExpiringDocumentsQuery>
{
    public GetExpiringDocumentsQueryValidator()
    {
        RuleFor(x => x.WithinDays)
            .InclusiveBetween(0, GetExpiringDocumentsQuery.MaxWithinDays)
            .WithMessage(
                $"The window must be between 0 and {GetExpiringDocumentsQuery.MaxWithinDays} days.")
            .When(x => x.WithinDays is not null);
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
        var query = _context.Attachments.AsNoTracking();

        if (request.IncludeUndated)
        {
            // A window still narrows the dated documents shown; undated ones
            // have nothing to compare against a cutoff, so they always pass.
            if (request.WithinDays is { } withinDays)
            {
                var cutoff = DateOnly.FromDateTime(_dateTimeProvider.UtcNow).AddDays(withinDays);
                query = query.Where(a => a.ExpiresAt == null || a.ExpiresAt <= cutoff);
            }
        }
        else
        {
            query = query.Where(a => a.ExpiresAt != null);

            if (request.WithinDays is { } withinDays)
            {
                var cutoff = DateOnly.FromDateTime(_dateTimeProvider.UtcNow).AddDays(withinDays);
                query = query.Where(a => a.ExpiresAt <= cutoff);
            }
        }

        return await query
            // Soonest — which means most-overdue — first; undated documents
            // (null sorts last in Postgres ascending order) trail behind
            // everything that actually has a date to act on.
            .OrderBy(a => a.ExpiresAt)
            .Select(AttachmentMapping.Projection)
            .ToListAsync(cancellationToken);
    }
}
