using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Finance;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences;

/// <summary>What the firm decided about leave: what a day of it pays, and whose holidays are not leave days.</summary>
public class LeaveSettingsDto
{
    /// <summary>Per working day, in euro. Null until the firm sets it.</summary>
    public decimal? AnnualLeaveDailyRate { get; init; }

    /// <summary>ISO 3166-1 alpha-2, or null when only weekends are non-working.</summary>
    public string? HolidayCountryCode { get; init; }
}

/// <summary>Reads the firm's leave settings. The amount is money, so it needs the finance grant.</summary>
public record GetLeaveSettingsQuery : IRequest<LeaveSettingsDto>;

public class GetLeaveSettingsQueryHandler : IRequestHandler<GetLeaveSettingsQuery, LeaveSettingsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetLeaveSettingsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<LeaveSettingsDto> Handle(GetLeaveSettingsQuery request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        return await _context.CompanySettings
            .AsNoTracking()
            .Select(c => new LeaveSettingsDto
            {
                AnnualLeaveDailyRate = c.AnnualLeaveDailyRate,
                HolidayCountryCode = c.LeaveHolidayCountryCode,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new LeaveSettingsDto();
    }
}

public record UpdateLeaveSettingsCommand : IRequest<LeaveSettingsDto>
{
    public decimal? AnnualLeaveDailyRate { get; init; }

    public string? HolidayCountryCode { get; init; }
}

public class UpdateLeaveSettingsCommandValidator : AbstractValidator<UpdateLeaveSettingsCommand>
{
    /// <summary>A guard against a slipped decimal point, not a policy on pay.</summary>
    public const decimal MaxDailyRate = 1000m;

    public UpdateLeaveSettingsCommandValidator()
    {
        RuleFor(x => x.AnnualLeaveDailyRate)
            .InclusiveBetween(0m, MaxDailyRate)
            .When(x => x.AnnualLeaveDailyRate is not null);

        RuleFor(x => x.HolidayCountryCode)
            .Matches("^[A-Za-z]{2}$").WithMessage("Use a two-letter country code, e.g. DE.")
            .When(x => !string.IsNullOrWhiteSpace(x.HolidayCountryCode));
    }
}

public class UpdateLeaveSettingsCommandHandler : IRequestHandler<UpdateLeaveSettingsCommand, LeaveSettingsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateLeaveSettingsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<LeaveSettingsDto> Handle(UpdateLeaveSettingsCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role is not (UserRole.SuperAdmin or UserRole.Admin))
        {
            throw new ForbiddenAccessException("Only management may change the leave settings.");
        }

        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        var settings = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            // The profile row is created by the first save of the company profile; until then
            // there is nowhere to keep this, and inventing a nameless company would be worse.
            throw new ConflictException("Save the company profile first (Settings), then set the leave rules.");
        }

        settings.AnnualLeaveDailyRate = request.AnnualLeaveDailyRate;
        settings.LeaveHolidayCountryCode = string.IsNullOrWhiteSpace(request.HolidayCountryCode)
            ? null
            : request.HolidayCountryCode.Trim().ToUpperInvariant();

        await _context.SaveChangesAsync(cancellationToken);

        return new LeaveSettingsDto
        {
            AnnualLeaveDailyRate = settings.AnnualLeaveDailyRate,
            HolidayCountryCode = settings.LeaveHolidayCountryCode,
        };
    }
}
