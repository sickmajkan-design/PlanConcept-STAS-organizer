using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

/// <summary>Records money received that belongs to no project — a rental, or anything else.</summary>
public record RecordCompanyRevenueCommand : IRequest<CompanyRevenueDto>
{
    public decimal Amount { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? OccurredOn { get; init; }

    public CompanyRevenueSource Source { get; init; }

    public Guid? VehicleId { get; init; }

    public Guid? ToolId { get; init; }

    public string? Note { get; init; }
}

/// <summary>The rules a revenue must satisfy, shared by recording and correcting one.</summary>
internal static class CompanyRevenueRules
{
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, decimal> amount,
        Func<T, DateOnly?> occurredOn,
        Func<T, CompanyRevenueSource> source,
        Func<T, Guid?> vehicleId,
        Func<T, Guid?> toolId,
        Func<T, string?> note,
        IDateTimeProvider dateTimeProvider)
    {
        validator.RuleFor(x => amount(x)).GreaterThan(0).WithMessage("An amount must be more than zero.");

        validator.RuleFor(x => source(x)).IsInEnum();

        validator.RuleFor(x => occurredOn(x))
            .Must(d => d is null || d.Value <= DateOnly.FromDateTime(dateTimeProvider.UtcNow))
            .WithMessage("Income cannot be received in the future.");

        validator.RuleFor(x => vehicleId(x))
            .Null().When(x => source(x) != CompanyRevenueSource.VehicleRental)
            .WithMessage("A vehicle can only be named on a vehicle rental.");

        validator.RuleFor(x => toolId(x))
            .Null().When(x => source(x) != CompanyRevenueSource.ToolRental)
            .WithMessage("A tool can only be named on a tool rental.");

        validator.RuleFor(x => note(x)).MaximumLength(500);
    }

    public static async Task EnsureAssetsExistAsync(
        IApplicationDbContext context,
        Guid? vehicleId,
        Guid? toolId,
        CancellationToken cancellationToken)
    {
        if (vehicleId is { } v && !await context.Vehicles.AnyAsync(x => x.Id == v, cancellationToken))
        {
            throw new NotFoundException(nameof(Vehicle), v);
        }

        if (toolId is { } t && !await context.Tools.AnyAsync(x => x.Id == t, cancellationToken))
        {
            throw new NotFoundException(nameof(Tool), t);
        }
    }
}

public class RecordCompanyRevenueCommandValidator : AbstractValidator<RecordCompanyRevenueCommand>
{
    public RecordCompanyRevenueCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        CompanyRevenueRules.Apply(
            this, x => x.Amount, x => x.OccurredOn, x => x.Source, x => x.VehicleId, x => x.ToolId, x => x.Note,
            dateTimeProvider);
    }
}

public class RecordCompanyRevenueCommandHandler : IRequestHandler<RecordCompanyRevenueCommand, CompanyRevenueDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RecordCompanyRevenueCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CompanyRevenueDto> Handle(RecordCompanyRevenueCommand request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);
        await CompanyRevenueRules.EnsureAssetsExistAsync(_context, request.VehicleId, request.ToolId, cancellationToken);

        var revenue = new CompanyRevenue
        {
            Amount = request.Amount,
            OccurredOn = request.OccurredOn ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
            Source = request.Source,
            VehicleId = request.VehicleId,
            ToolId = request.ToolId,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            RecordedByUserId = _currentUserService.UserId,
        };

        _context.CompanyRevenues.Add(revenue);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.CompanyRevenues
            .AsNoTracking()
            .Where(r => r.Id == revenue.Id)
            .Select(CompanyRevenueMapping.Projection)
            .SingleAsync(cancellationToken);
    }
}
