using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Materials.Models;
using Construction.Application.Features.Costs;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Materials.Commands.CreateMaterial;

/// <summary>
/// Creates a material. When it starts with stock on the shelf, that stock is
/// recorded as a movement in the same save, so the history begins with what
/// came in rather than with an unexplained number.
/// </summary>
public record CreateMaterialCommand : MaterialCommandBase, IRequest<MaterialDto>
{
    /// <summary>Invoice or receipt the starting stock was bought against.</summary>
    public string? InvoiceNumber { get; init; }

    public string? Supplier { get; init; }

    /// <summary>What a unit cost when it was bought; falls back to the reference price.</summary>
    public decimal? PurchaseUnitPrice { get; init; }

    /// <summary>When the goods arrived. Defaults to today.</summary>
    public DateOnly? ReceivedOn { get; init; }

    public string? ReceiptNote { get; init; }

    public bool HasReceiptDetails =>
        !string.IsNullOrWhiteSpace(InvoiceNumber)
        || !string.IsNullOrWhiteSpace(Supplier)
        || PurchaseUnitPrice is not null;
}

public class CreateMaterialCommandValidator : MaterialCommandBaseValidator<CreateMaterialCommand>
{
    public CreateMaterialCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.InvoiceNumber).MaximumLength(100);
        RuleFor(x => x.Supplier).MaximumLength(200);
        RuleFor(x => x.ReceiptNote).MaximumLength(500);

        RuleFor(x => x.PurchaseUnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Price must not be negative.")
            .When(x => x.PurchaseUnitPrice is not null);

        // The same rule a delivery entered on its own has to meet: the invoice
        // is the paper trail back to what was paid.
        RuleFor(x => x.InvoiceNumber)
            .NotEmpty()
            .WithMessage("A delivery needs an invoice or receipt number.")
            .When(x => x.Quantity > 0 && x.HasReceiptDetails);

        RuleFor(x => x.ReceivedOn)
            .LessThanOrEqualTo(today)
            .WithMessage("Stock cannot move in the future.")
            .GreaterThanOrEqualTo(today.AddDays(-CostRules.MaxBackdatingDays))
            .WithMessage(
                $"A movement cannot be recorded more than {CostRules.MaxBackdatingDays} days back.")
            .When(x => x.ReceivedOn is not null);
    }
}

public class CreateMaterialCommandHandler : IRequestHandler<CreateMaterialCommand, MaterialDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public CreateMaterialCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task<MaterialDto> Handle(
        CreateMaterialCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ProjectId is { } projectId)
        {
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == projectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException(nameof(Project), projectId);
            }
        }

        var material = new Material
        {
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            Quantity = request.Quantity,
            Warehouse = request.Warehouse?.Trim(),
            UnitPrice = request.UnitPrice,
            ProjectId = request.ProjectId,
            LastUpdated = _dateTimeProvider.UtcNow
        };

        _context.Materials.Add(material);

        if (request.Quantity > 0)
        {
            var receipt = request.HasReceiptDetails;

            // A price and a supplier are spending records: only someone who may
            // record spending can attach them.
            if (receipt && !CostRules.CanRecordSpending(_currentUserService.Role))
            {
                throw new ForbiddenAccessException("You may not record stock movements.");
            }

            var occurredOn = request.ReceivedOn ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

            // With an invoice it is a delivery, priced from what was paid.
            // Without one it is an opening balance: stock that was already on
            // the shelf, so there is no purchase to price.
            material.Movements.Add(new MaterialMovement
            {
                Kind = receipt ? MaterialMovementKind.In : MaterialMovementKind.Adjustment,
                Quantity = request.Quantity,
                UnitPrice = receipt ? request.PurchaseUnitPrice ?? request.UnitPrice : null,
                OccurredOn = occurredOn,
                InvoiceNumber = receipt ? request.InvoiceNumber?.Trim() : null,
                Supplier = string.IsNullOrWhiteSpace(request.Supplier) ? null : request.Supplier.Trim(),
                Note = string.IsNullOrWhiteSpace(request.ReceiptNote)
                    ? (receipt ? null : "Opening stock")
                    : request.ReceiptNote.Trim(),
                RecordedByUserId = _currentUserService.UserId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Reload through a projection so the project name is populated.
        return await _context.Materials
            .AsNoTracking()
            .Where(m => m.Id == material.Id)
            .Select(MaterialMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
