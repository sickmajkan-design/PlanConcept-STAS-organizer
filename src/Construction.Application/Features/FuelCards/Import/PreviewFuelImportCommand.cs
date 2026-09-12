using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.FuelCards.Import;

/// <summary>
/// Parses an uploaded statement against a column mapping and reports what
/// would happen, without writing anything — the read half of the "map, then
/// confirm" import flow, mirroring <c>PreviewHolidaySyncQuery</c>. Shaped as a
/// command rather than a query because it carries a multipart file upload,
/// which is a request body regardless of whether it changes anything.
/// </summary>
public record PreviewFuelImportCommand : IRequest<FuelImportPreviewDto>
{
    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public Stream Content { get; init; } = null!;

    public FuelImportColumnMapping Mapping { get; init; } = null!;

    public bool HasHeaderRow { get; init; } = true;
}

public class PreviewFuelImportCommandValidator : AbstractValidator<PreviewFuelImportCommand>
{
    public PreviewFuelImportCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(FuelImportRules.HasAllowedExtension)
            .WithMessage("The statement must be an .xlsx or .csv file.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("The file is empty.")
            .LessThanOrEqualTo(FuelImportRules.MaxSizeBytes)
            .WithMessage("The file is larger than the 10 MB limit.");

        RuleFor(x => x.Mapping).NotNull();
    }
}

public class PreviewFuelImportCommandHandler
    : IRequestHandler<PreviewFuelImportCommand, FuelImportPreviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFuelStatementParser _xlsxParser;

    public PreviewFuelImportCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFuelStatementParser xlsxParser)
    {
        _context = context;
        _currentUserService = currentUserService;
        _xlsxParser = xlsxParser;
    }

    public async Task<FuelImportPreviewDto> Handle(
        PreviewFuelImportCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanRecordSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not import fuel statements.");
        }

        var parsed = FuelStatementFileParser.Parse(request.FileName, request.Content, _xlsxParser);
        var cardsByNumber = await FuelCardLookup.LoadByCardNumber(_context, cancellationToken);

        var results = FuelImportRowResolver.Resolve(
            parsed.Rows, request.Mapping, request.HasHeaderRow, cardsByNumber);

        var readyCount = results.Count(r => r.Status == FuelImportRowStatus.Ready);

        return new FuelImportPreviewDto
        {
            TotalRows = results.Count,
            ReadyCount = readyCount,
            ProblemCount = results.Count - readyCount,
            Rows = results.Take(FuelImportRules.MaxPreviewRows).ToList(),
        };
    }
}
