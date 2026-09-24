using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

/// <summary>Corrects a company revenue that was typed in wrong.</summary>
public record UpdateCompanyRevenueCommand : IRequest<CompanyRevenueDto>
{
    public Guid Id { get; init; }

    public decimal Amount { get; init; }

    public DateOnly? OccurredOn { get; init; }

    public CompanyRevenueSource Source { get; init; }

    public Guid? VehicleId { get; init; }

    public Guid? ToolId { get; init; }

    public string? Note { get; init; }
}

public class UpdateCompanyRevenueCommandValidator : AbstractValidator<UpdateCompanyRevenueCommand>
{
    public UpdateCompanyRevenueCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        CompanyRevenueRules.Apply(
            this, x => x.Amount, x => x.OccurredOn, x => x.Source, x => x.VehicleId, x => x.ToolId, x => x.Note,
            dateTimeProvider);
    }
}

public class UpdateCompanyRevenueCommandHandler : IRequestHandler<UpdateCompanyRevenueCommand, CompanyRevenueDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCompanyRevenueCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CompanyRevenueDto> Handle(UpdateCompanyRevenueCommand request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        var revenue = await _context.CompanyRevenues
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CompanyRevenue), request.Id);

        await CompanyRevenueRules.EnsureAssetsExistAsync(_context, request.VehicleId, request.ToolId, cancellationToken);

        revenue.Amount = request.Amount;
        revenue.Source = request.Source;
        revenue.VehicleId = request.VehicleId;
        revenue.ToolId = request.ToolId;
        revenue.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        // Left alone when not sent, so correcting an amount never moves the date.
        if (request.OccurredOn is { } occurredOn)
        {
            revenue.OccurredOn = occurredOn;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.CompanyRevenues
            .AsNoTracking()
            .Where(r => r.Id == revenue.Id)
            .Select(CompanyRevenueMapping.Projection)
            .SingleAsync(cancellationToken);
    }
}
