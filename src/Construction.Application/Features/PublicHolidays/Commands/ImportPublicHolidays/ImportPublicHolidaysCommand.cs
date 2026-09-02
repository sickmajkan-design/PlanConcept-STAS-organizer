using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.PublicHolidays.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.PublicHolidays.Commands.ImportPublicHolidays;

/// <summary>One holiday chosen from a <c>PreviewHolidaySyncQuery</c> result to add to the calendar.</summary>
public record PublicHolidayImportItem(DateOnly Date, string Name);

/// <summary>
/// Adds the chosen holidays to the calendar in one request — the write half
/// of the "sync from the internet" flow. A date already on the calendar is
/// silently skipped rather than refused, since a review screen offering
/// already-present dates as pre-checked would otherwise fail on every reuse.
/// </summary>
public record ImportPublicHolidaysCommand : IRequest<IReadOnlyList<PublicHolidayDto>>
{
    public IReadOnlyList<PublicHolidayImportItem> Items { get; init; } = Array.Empty<PublicHolidayImportItem>();
}

public class ImportPublicHolidaysCommandValidator : AbstractValidator<ImportPublicHolidaysCommand>
{
    public ImportPublicHolidaysCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Choose at least one holiday to import.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Date).NotEqual(default(DateOnly)).WithMessage("A date is required.");
            item.RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        });
    }
}

public class ImportPublicHolidaysCommandHandler
    : IRequestHandler<ImportPublicHolidaysCommand, IReadOnlyList<PublicHolidayDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ImportPublicHolidaysCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<PublicHolidayDto>> Handle(
        ImportPublicHolidaysCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSetLabourRate(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not manage the holiday calendar.");
        }

        var requestedDates = request.Items.Select(i => i.Date).ToList();

        var existingDates = await _context.PublicHolidays
            .Where(h => requestedDates.Contains(h.Date))
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);
        var existing = existingDates.ToHashSet();

        var toInsert = request.Items
            .Where(item => !existing.Contains(item.Date))
            // Two selected rows sharing a date (a stale checklist reused
            // across a re-search) would otherwise hit the same unique-index
            // conflict the single-add path already guards against.
            .GroupBy(item => item.Date)
            .Select(group => group.First())
            .Select(item => new PublicHoliday { Date = item.Date, Name = item.Name.Trim() })
            .ToList();

        if (toInsert.Count == 0)
        {
            return Array.Empty<PublicHolidayDto>();
        }

        _context.PublicHolidays.AddRange(toInsert);
        await _context.SaveChangesAsync(cancellationToken);

        return toInsert.Select(PublicHolidayMapping.ToDto).ToList();
    }
}
