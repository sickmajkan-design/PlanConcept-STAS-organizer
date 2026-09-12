using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.PublicHolidays.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.PublicHolidays.Queries.PreviewHolidaySync;

/// <summary>
/// Fetches a country's public holidays for one year from the internet,
/// without writing anything — the calendar only changes once someone reviews
/// this list and imports the ones they want via <c>ImportPublicHolidaysCommand</c>.
/// </summary>
public record PreviewHolidaySyncQuery : IRequest<IReadOnlyList<PublicHolidayCandidateDto>>
{
    /// <summary>ISO 3166-1 alpha-2, e.g. "BA".</summary>
    public string CountryCode { get; init; } = null!;

    public int Year { get; init; }
}

public class PreviewHolidaySyncQueryValidator : AbstractValidator<PreviewHolidaySyncQuery>
{
    public PreviewHolidaySyncQueryValidator(IDateTimeProvider dateTimeProvider)
    {
        var thisYear = dateTimeProvider.UtcNow.Year;

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("A country is required.")
            .Length(2).WithMessage("Use the two-letter country code (ISO 3166-1 alpha-2).");

        RuleFor(x => x.Year)
            // A wide but sane window — the calendar itself accepts any date,
            // this just keeps a mistyped year from becoming a pointless call.
            .InclusiveBetween(thisYear - 2, thisYear + 5)
            .WithMessage("Pick a year closer to now.");
    }
}

public class PreviewHolidaySyncQueryHandler
    : IRequestHandler<PreviewHolidaySyncQuery, IReadOnlyList<PublicHolidayCandidateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPublicHolidaySource _source;

    public PreviewHolidaySyncQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPublicHolidaySource source)
    {
        _context = context;
        _currentUserService = currentUserService;
        _source = source;
    }

    public async Task<IReadOnlyList<PublicHolidayCandidateDto>> Handle(
        PreviewHolidaySyncQuery request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanSetLabourRate(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not manage the holiday calendar.");
        }

        var countryCode = request.CountryCode.Trim().ToUpperInvariant();

        var external = await _source.GetHolidaysAsync(countryCode, request.Year, cancellationToken);

        var existingDates = await _context.PublicHolidays
            .Where(h => h.Date.Year == request.Year && h.CountryCode == countryCode)
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);
        var existing = existingDates.ToHashSet();

        return external
            .Select(holiday => new PublicHolidayCandidateDto
            {
                Date = holiday.Date,
                // The local name is what shows up on a Bosnian (or wherever
                // the chosen country's) payslip and schedule — the English
                // name alongside it in the source data is for markets this
                // company isn't in.
                Name = string.IsNullOrWhiteSpace(holiday.LocalName) ? holiday.Name : holiday.LocalName,
                AlreadyOnCalendar = existing.Contains(holiday.Date),
            })
            .OrderBy(candidate => candidate.Date)
            .ToList();
    }
}
