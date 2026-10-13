using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Spreadsheets;
using Construction.Application.Features.Exports.Queries;
using MediatR;

namespace Construction.Application.Features.Book;

/// <summary>
/// The book as a spreadsheet for the accountant: the lines the filter selects, with the same figures as on
/// the screen. Numbers are numbers, so they can be added up.
/// </summary>
public record GetBookExportQuery : IBookRowFilter, IRequest<ExportFile>
{
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public Domain.Enums.BookEntryKind? Kind { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? ProjectId { get; init; }

    public bool? Open { get; init; }

    public string? Search { get; init; }

    public Guid? BranchId { get; init; }
}

public class GetBookExportQueryHandler : IRequestHandler<GetBookExportQuery, ExportFile>
{
    private const int MaxRows = 20_000;

    private readonly GetBookEntriesQueryHandler _entries;
    private readonly ISpreadsheetWriter _writer;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetBookExportQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISpreadsheetWriter writer,
        IDateTimeProvider dateTimeProvider)
    {
        _entries = new GetBookEntriesQueryHandler(context, currentUserService);
        _writer = writer;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ExportFile> Handle(GetBookExportQuery request, CancellationToken cancellationToken)
    {
        var page = await _entries.Handle(new GetBookEntriesQuery
        {
            PageNumber = 1,
            PageSize = MaxRows,
            From = request.From,
            To = request.To,
            Kind = request.Kind,
            CategoryId = request.CategoryId,
            ProjectId = request.ProjectId,
            Open = request.Open,
            Search = request.Search,
            BranchId = request.BranchId,
        }, cancellationToken);

        static string KindLabel(string kind) => kind switch
        {
            "Income" => "Prihod",
            "Cost" => "Trošak",
            "Payment" => "Uplata",
            "Payout" => "Isplata",
            _ => kind,
        };

        static string SourceLabel(BookRowDto row) => row.Source switch
        {
            "BookEntry" => row.Origin switch { "Template" => "Šablon", "Import" => "Uvoz", _ => "Ručno" },
            "VehicleExpense" => row.Origin == "Fuel" ? "Gorivo i vozila" : "Vozila",
            "ToolExpense" => "Alat",
            "GeneralExpense" => "Opći trošak",
            "FinanceEntry" => "Isplata radniku",
            "ProjectRevenue" or "CompanyRevenue" => "Prihod",
            "Invoice" => "Faktura",
            "InvoicePayment" => "Uplata fakture",
            "WagePayout" => row.Origin == "Final" ? "Konačna isplata" : "Akontacija",
            _ => row.Source,
        };

        var rows = page.Items
            .Select(r => (IReadOnlyList<object?>)new List<object?>
            {
                r.OccurredOn.ToDateTime(TimeOnly.MinValue),
                KindLabel(r.Kind),
                r.Description,
                r.CategoryName,
                r.ProjectName,
                r.Counterparty,
                SourceLabel(r),
                r.IsOpen ? "Otvoreno" : "Plaćeno",
                r.DueDate?.ToDateTime(TimeOnly.MinValue),
                // Direction in the sign, as on the screen: what came in is positive, what went out negative.
                r.Kind is "Income" or "Payment" ? r.Amount : -r.Amount,
            })
            .ToList();

        var sheet = new SpreadsheetSheet(
            "Knjiga zapisa",
            [
                new SpreadsheetColumn("Datum", SpreadsheetValueKind.Date),
                new SpreadsheetColumn("Vrsta", SpreadsheetValueKind.Text),
                new SpreadsheetColumn("Opis", SpreadsheetValueKind.Text),
                new SpreadsheetColumn("Kategorija", SpreadsheetValueKind.Text),
                new SpreadsheetColumn("Gradilište", SpreadsheetValueKind.Text),
                new SpreadsheetColumn("Druga strana", SpreadsheetValueKind.Text),
                new SpreadsheetColumn("Izvor", SpreadsheetValueKind.Text),
                new SpreadsheetColumn("Status", SpreadsheetValueKind.Text),
                new SpreadsheetColumn("Rok", SpreadsheetValueKind.Date),
                new SpreadsheetColumn("Iznos", SpreadsheetValueKind.Money),
            ],
            rows);

        var from = request.From?.ToString("yyyy-MM-dd") ?? "pocetak";
        var to = request.To?.ToString("yyyy-MM-dd") ?? "danas";

        return new ExportFile(
            $"knjiga-{from}-{to}.xlsx",
            _writer.ContentType,
            _writer.Write(Spreadsheet.Of(sheet, _dateTimeProvider.UtcNow.ToString("dd.MM.yyyy. HH:mm"))));
    }
}
