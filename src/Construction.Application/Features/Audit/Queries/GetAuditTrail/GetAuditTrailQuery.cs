using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Audit.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Audit.Queries.GetAuditTrail;

/// <summary>
/// The trail, newest first, narrowed however the question was asked.
/// </summary>
/// <remarks>
/// Two questions get asked of an audit trail, and both are supported here:
/// "what happened to this record" (<see cref="EntityName"/> and
/// <see cref="EntityId"/>) and "what did this person do"
/// (<see cref="UserId"/>). Each has an index behind it.
/// </remarks>
public record GetAuditTrailQuery : IPagedQuery, IRequest<PagedList<AuditEntryDto>>
{
    /// <summary>The CLR type name, e.g. <c>Employee</c>. Case-insensitive.</summary>
    public string? EntityName { get; init; }

    public Guid? EntityId { get; init; }

    /// <summary>Everything done by one account.</summary>
    public Guid? UserId { get; init; }

    public AuditAction? Action { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}

public class GetAuditTrailQueryValidator : PagedQueryValidator<GetAuditTrailQuery>
{
    public GetAuditTrailQueryValidator()
    {
        RuleFor(x => x.EntityName)
            .MaximumLength(128);

        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("'From' must not be after 'To'.")
            .OverridePropertyName(nameof(GetAuditTrailQuery.From));

        // An id without a type would match rows across tables that happen to
        // share a key. Guids do not collide in practice, but the index is
        // keyed on the pair and an id-only query cannot use it.
        RuleFor(x => x.EntityName)
            .NotEmpty()
            .When(x => x.EntityId is not null)
            .WithMessage("'EntityName' is required when filtering by 'EntityId'.");
    }
}

public class GetAuditTrailQueryHandler
    : IRequestHandler<GetAuditTrailQuery, PagedList<AuditEntryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAuditTrailQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedList<AuditEntryDto>> Handle(
        GetAuditTrailQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            // Equality, not a search. The caller names a type, and matching
            // loosely would let "Employee" pull in "EmployeeRate".
            var name = request.EntityName.Trim();

            query = query.Where(a => a.EntityName.ToLower() == name.ToLower());
        }

        if (request.EntityId is { } entityId)
        {
            query = query.Where(a => a.EntityId == entityId);
        }

        if (request.UserId is { } userId)
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (request.Action is { } action)
        {
            query = query.Where(a => a.Action == action);
        }

        if (request.From is { } from)
        {
            query = query.Where(a => a.OccurredAt >= AsUtc(from));
        }

        if (request.To is { } to)
        {
            query = query.Where(a => a.OccurredAt <= AsUtc(to));
        }

        return await PagedList<AuditEntryDto>.CreateAsync(
            query
                .OrderByDescending(a => a.OccurredAt)
                // Several rows share a timestamp — one save writes them all
                // with the same clock reading — so without a tiebreaker the
                // order between them is undefined and paging can repeat a row.
                .ThenByDescending(a => a.Id)
                .ProjectTo<AuditEntryDto>(_mapper.ConfigurationProvider),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
