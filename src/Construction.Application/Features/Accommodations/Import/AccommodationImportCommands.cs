using Construction.Application.Common.Interfaces;
using Construction.Application.Features.FuelCards.Import;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Import;

/// <summary>Reads an uploaded housing list and reports what would happen, without writing anything.</summary>
public record PreviewAccommodationImportCommand : IRequest<AccommodationImportPreviewDto>
{
    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public Stream Content { get; init; } = null!;
}

/// <summary>Creates the accommodations and stays of every row that resolves cleanly.</summary>
public record ImportAccommodationsCommand : IRequest<AccommodationImportResultDto>
{
    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public Stream Content { get; init; } = null!;
}

public class PreviewAccommodationImportCommandValidator : AbstractValidator<PreviewAccommodationImportCommand>
{
    public PreviewAccommodationImportCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(FuelImportRules.MaxSizeBytes)
            .WithMessage("The file is larger than the 10 MB limit.");
    }
}

public class ImportAccommodationsCommandValidator : AbstractValidator<ImportAccommodationsCommand>
{
    public ImportAccommodationsCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(FuelImportRules.MaxSizeBytes)
            .WithMessage("The file is larger than the 10 MB limit.");
    }
}

internal static class AccommodationImportSupport
{
    public static async Task<IReadOnlyList<AccommodationImportRowResult>> ReadAsync(
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

        var accommodations = await context.Accommodations
            .AsNoTracking()
            .Select(a => new { a.Id, a.Address })
            .ToListAsync(cancellationToken);

        var byAddress = new Dictionary<string, Guid>();

        foreach (var accommodation in accommodations)
        {
            byAddress.TryAdd(AccommodationRowResolver.AddressKey(accommodation.Address), accommodation.Id);
        }

        var employees = await context.Employees
            .AsNoTracking()
            .Select(e => new { e.Id, e.EmployeeNumber, e.FirstName, e.LastName })
            .ToListAsync(cancellationToken);

        // The employee number is unambiguous; a full name is only used when
        // exactly one person has it, since guessing between two is worse than asking.
        var byKey = new Dictionary<string, (Guid Id, string Name)>();

        foreach (var employee in employees)
        {
            byKey[employee.EmployeeNumber.Trim().ToLowerInvariant()] =
                (employee.Id, $"{employee.FirstName} {employee.LastName}");
        }

        foreach (var group in employees.GroupBy(e => $"{e.FirstName} {e.LastName}".Trim().ToLowerInvariant()))
        {
            if (group.Count() == 1 && !byKey.ContainsKey(group.Key))
            {
                var only = group.First();
                byKey[group.Key] = (only.Id, $"{only.FirstName} {only.LastName}");
            }
        }

        var projects = await context.Projects
            .AsNoTracking()
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken);

        var projectsByName = new Dictionary<string, Guid>();

        foreach (var project in projects)
        {
            projectsByName.TryAdd(project.Name.Trim().ToLowerInvariant(), project.Id);
        }

        var stays = await context.AccommodationStays
            .AsNoTracking()
            .Select(s => new KnownStay(s.EmployeeId, s.AccommodationId, s.StartDate, s.EndDate))
            .ToListAsync(cancellationToken);

        return AccommodationRowResolver.Resolve(
            parsed.Rows,
            byAddress,
            byKey,
            projectsByName,
            stays,
            DateOnly.FromDateTime(dateTimeProvider.UtcNow));
    }
}

public class PreviewAccommodationImportCommandHandler
    : IRequestHandler<PreviewAccommodationImportCommand, AccommodationImportPreviewDto>
{
    private const int MaxPreviewRows = 100;

    private readonly IApplicationDbContext _context;
    private readonly IFuelStatementParser _xlsxParser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PreviewAccommodationImportCommandHandler(
        IApplicationDbContext context,
        IFuelStatementParser xlsxParser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _xlsxParser = xlsxParser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AccommodationImportPreviewDto> Handle(
        PreviewAccommodationImportCommand request,
        CancellationToken cancellationToken)
    {
        var rows = await AccommodationImportSupport.ReadAsync(
            _context, _xlsxParser, _dateTimeProvider, request.FileName, request.Content, cancellationToken);

        return new AccommodationImportPreviewDto
        {
            TotalRows = rows.Count,
            NewAccommodationCount = rows.Count(r => r.CreatesAccommodation),
            NewStayCount = rows.Count(r => r.CreatesStay),
            ProblemCount = rows.Count(r => !r.WillImport),
            Rows = rows.Take(MaxPreviewRows).ToList()
        };
    }
}

public class ImportAccommodationsCommandHandler
    : IRequestHandler<ImportAccommodationsCommand, AccommodationImportResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IFuelStatementParser _xlsxParser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public ImportAccommodationsCommandHandler(
        IApplicationDbContext context,
        IFuelStatementParser xlsxParser,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _xlsxParser = xlsxParser;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task<AccommodationImportResultDto> Handle(
        ImportAccommodationsCommand request,
        CancellationToken cancellationToken)
    {
        var rows = await AccommodationImportSupport.ReadAsync(
            _context, _xlsxParser, _dateTimeProvider, request.FileName, request.Content, cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var importable = rows.Where(r => r.WillImport).ToList();
        var createdAccommodations = 0;
        var createdStays = 0;

        var existing = await _context.Accommodations
            .AsNoTracking()
            .Select(a => new { a.Id, a.Address })
            .ToListAsync(cancellationToken);

        var idsByAddress = new Dictionary<string, Guid>();

        foreach (var accommodation in existing)
        {
            idsByAddress.TryAdd(AccommodationRowResolver.AddressKey(accommodation.Address), accommodation.Id);
        }

        await _context.ExecuteInTransactionAsync(
            async token =>
            {
                foreach (var row in importable)
                {
                    var key = AccommodationRowResolver.AddressKey(row.Address!);

                    if (!idsByAddress.TryGetValue(key, out var accommodationId))
                    {
                        var accommodation = new Accommodation
                        {
                            Address = row.Address!.Trim(),
                            Name = row.Name,
                            City = row.City,
                            Type = row.Type,
                            Floor = row.Floor,
                            Rooms = row.Rooms,
                            Beds = row.Beds
                        };

                        _context.Accommodations.Add(accommodation);
                        accommodationId = accommodation.Id;
                        idsByAddress[key] = accommodationId;
                        createdAccommodations++;

                        if (row.MonthlyRent is { } rent)
                        {
                            _context.AccommodationRates.Add(new AccommodationRate
                            {
                                AccommodationId = accommodationId,
                                Kind = AccommodationChargeKind.Monthly,
                                Amount = rent,
                                StartDate = row.MoveIn ?? today,
                                SetByUserId = _currentUserService.UserId
                            });
                        }
                    }

                    if (row.CreatesStay)
                    {
                        _context.AccommodationStays.Add(new AccommodationStay
                        {
                            AccommodationId = accommodationId,
                            EmployeeId = row.EmployeeId!.Value,
                            ProjectId = row.ProjectId,
                            StartDate = row.MoveIn ?? today,
                            EndDate = row.MoveOut
                        });
                        createdStays++;
                    }
                }

                await _context.SaveChangesAsync(token);
            },
            cancellationToken);

        var skipped = rows.Where(r => !r.WillImport).ToList();

        return new AccommodationImportResultDto
        {
            TotalRows = rows.Count,
            CreatedAccommodations = createdAccommodations,
            CreatedStays = createdStays,
            SkippedCount = skipped.Count,
            Skipped = skipped
        };
    }
}
