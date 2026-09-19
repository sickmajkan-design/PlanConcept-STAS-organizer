using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.FuelCards.Import;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Materials.Import;

/// <summary>Reads an uploaded delivery list and reports what would happen, without writing anything.</summary>
public record PreviewMaterialDeliveryImportCommand : IRequest<MaterialImportPreviewDto>
{
    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public Stream Content { get; init; } = null!;
}

/// <summary>Records every row of the same list that resolves cleanly, creating materials that do not exist yet.</summary>
public record ImportMaterialDeliveriesCommand : IRequest<MaterialImportResultDto>
{
    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public Stream Content { get; init; } = null!;
}

public class PreviewMaterialDeliveryImportCommandValidator
    : AbstractValidator<PreviewMaterialDeliveryImportCommand>
{
    public PreviewMaterialDeliveryImportCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(FuelImportRules.MaxSizeBytes)
            .WithMessage("The file is larger than the 10 MB limit.");
    }
}

public class ImportMaterialDeliveriesCommandValidator
    : AbstractValidator<ImportMaterialDeliveriesCommand>
{
    public ImportMaterialDeliveriesCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(FuelImportRules.MaxSizeBytes)
            .WithMessage("The file is larger than the 10 MB limit.");
    }
}

internal static class MaterialDeliveryImportSupport
{
    public static async Task<IReadOnlyList<MaterialImportRowResult>> ReadAsync(
        IApplicationDbContext context,
        IFuelStatementParser xlsxParser,
        IDateTimeProvider dateTimeProvider,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (!FuelImportRules.HasAllowedExtension(fileName))
        {
            throw new Construction.Application.Common.Exceptions.ValidationException(
            [
                new ValidationFailure(nameof(fileName), "The list must be an .xlsx or .csv file.")
            ]);
        }

        var parsed = FuelStatementFileParser.Parse(fileName, content, xlsxParser);

        var materials = await context.Materials
            .AsNoTracking()
            .Select(m => new { m.Id, m.Name })
            .ToListAsync(cancellationToken);

        var byName = new Dictionary<string, Guid>();

        foreach (var material in materials)
        {
            byName.TryAdd(material.Name.Trim().ToLowerInvariant(), material.Id);
        }

        var existing = await context.MaterialMovements
            .AsNoTracking()
            .Where(m => m.Kind == MaterialMovementKind.In && m.InvoiceNumber != null)
            .Select(m => new { m.MaterialId, m.InvoiceNumber, m.Quantity, m.UnitPrice, m.OccurredOn })
            .ToListAsync(cancellationToken);

        var existingKeys = existing
            .Select(m => (m.MaterialId, m.InvoiceNumber!.Trim(), m.Quantity, m.UnitPrice, m.OccurredOn))
            .ToHashSet();

        return MaterialDeliveryRowResolver.Resolve(
            parsed.Rows,
            byName,
            (materialId, invoice, quantity, price, date) =>
                existingKeys.Contains((materialId, invoice, quantity, price, date)),
            DateOnly.FromDateTime(dateTimeProvider.UtcNow));
    }
}

public class PreviewMaterialDeliveryImportCommandHandler
    : IRequestHandler<PreviewMaterialDeliveryImportCommand, MaterialImportPreviewDto>
{
    private const int MaxPreviewRows = 100;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFuelStatementParser _xlsxParser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PreviewMaterialDeliveryImportCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFuelStatementParser xlsxParser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _xlsxParser = xlsxParser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<MaterialImportPreviewDto> Handle(
        PreviewMaterialDeliveryImportCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not record stock movements.");
        }

        var rows = await MaterialDeliveryImportSupport.ReadAsync(
            _context, _xlsxParser, _dateTimeProvider, request.FileName, request.Content, cancellationToken);

        return new MaterialImportPreviewDto
        {
            TotalRows = rows.Count,
            ReadyCount = rows.Count(r => r.Status == MaterialImportRowStatus.Ready),
            NewMaterialCount = rows.Count(r => r.Status == MaterialImportRowStatus.NewMaterial),
            ProblemCount = rows.Count(r => !r.WillImport),
            Rows = rows.Take(MaxPreviewRows).ToList()
        };
    }
}

public class ImportMaterialDeliveriesCommandHandler
    : IRequestHandler<ImportMaterialDeliveriesCommand, MaterialImportResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFuelStatementParser _xlsxParser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ImportMaterialDeliveriesCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFuelStatementParser xlsxParser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _xlsxParser = xlsxParser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<MaterialImportResultDto> Handle(
        ImportMaterialDeliveriesCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not record stock movements.");
        }

        var rows = await MaterialDeliveryImportSupport.ReadAsync(
            _context, _xlsxParser, _dateTimeProvider, request.FileName, request.Content, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var importable = rows.Where(r => r.WillImport).ToList();
        var createdMaterials = 0;

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                // Materials named in the list that do not exist yet: one each,
                // even when several rows deliver the same new material.
                var created = new Dictionary<string, Material>();

                foreach (var row in importable)
                {
                    var key = row.MaterialName!.Trim().ToLowerInvariant();
                    Guid materialId;

                    if (row.MaterialId is { } existingId)
                    {
                        materialId = existingId;
                    }
                    else
                    {
                        if (!created.TryGetValue(key, out var material))
                        {
                            material = new Material
                            {
                                Name = row.MaterialName!.Trim(),
                                Unit = row.Unit!.Trim(),
                                UnitPrice = row.UnitPrice,
                                LastUpdated = now
                            };
                            created[key] = material;
                            _context.Materials.Add(material);
                            createdMaterials++;
                        }

                        materialId = material.Id;
                    }

                    var movement = new MaterialMovement
                    {
                        Kind = MaterialMovementKind.In,
                        Quantity = row.Quantity!.Value,
                        UnitPrice = row.UnitPrice,
                        OccurredOn = row.OccurredOn!.Value,
                        InvoiceNumber = row.InvoiceNumber!.Trim(),
                        Supplier = row.Supplier?.Trim(),
                        Note = row.Note?.Trim(),
                        RecordedByUserId = _currentUserService.UserId
                    };

                    if (row.MaterialId is null)
                    {
                        movement.Material = created[key];
                        created[key].Quantity += movement.Quantity;
                    }
                    else
                    {
                        movement.MaterialId = materialId;
                        await _context.Materials
                            .Where(m => m.Id == materialId)
                            .ExecuteUpdateAsync(
                                setters => setters
                                    .SetProperty(m => m.Quantity, m => m.Quantity + movement.Quantity)
                                    .SetProperty(m => m.LastUpdated, now)
                                    .SetProperty(m => m.UpdatedAt, now),
                                token);
                    }

                    _context.MaterialMovements.Add(movement);
                }

                await _context.SaveChangesAsync(token);
            },
            cancellationToken);

        var skipped = rows.Where(r => !r.WillImport).ToList();

        return new MaterialImportResultDto
        {
            TotalRows = rows.Count,
            CreatedDeliveries = importable.Count,
            CreatedMaterials = createdMaterials,
            SkippedCount = skipped.Count,
            Skipped = skipped
        };
    }
}
