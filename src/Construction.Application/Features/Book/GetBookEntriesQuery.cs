using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Finance;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Book;

/// <summary>
/// The book: the entries of the book itself together with the money the other tables already
/// hold — approved vehicle and tool costs, general expenses, pay entries, revenues and invoices —
/// newest first. Nothing is copied: each line says where it lives, and is changed there.
/// </summary>
public record GetBookEntriesQuery : IPagedQuery, IBookRowFilter, IRequest<PagedList<BookRowDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 50;

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    /// <summary>"Income", "Cost", "Payment" or "Payout".</summary>
    public BookEntryKind? Kind { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? ProjectId { get; init; }

    /// <summary>True lists only what is still open; false only what is settled.</summary>
    public bool? Open { get; init; }

    /// <summary>Part of the counterparty or the description, in any case.</summary>
    public string? Search { get; init; }

    public Guid? BranchId { get; init; }
}

/// <summary>What narrows the book: the same for the list and for its totals.</summary>
public interface IBookRowFilter
{
    DateOnly? From { get; }

    DateOnly? To { get; }

    BookEntryKind? Kind { get; }

    Guid? CategoryId { get; }

    Guid? ProjectId { get; }

    bool? Open { get; }

    string? Search { get; }

    Guid? BranchId { get; }
}

public class GetBookEntriesQueryValidator : PagedQueryValidator<GetBookEntriesQuery>
{
    public GetBookEntriesQueryValidator() : base(maxPageSize: 200)
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From is not null && x.To is not null);

        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public class GetBookEntriesQueryHandler : IRequestHandler<GetBookEntriesQuery, PagedList<BookRowDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBookEntriesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<BookRowDto>> Handle(GetBookEntriesQuery request, CancellationToken cancellationToken)
    {
        var (rows, categories) = await FilteredAsync(request, cancellationToken);

        var page = await PagedList<BookRow>.CreateAsync(
            rows.OrderByDescending(r => r.OccurredOn).ThenBy(r => r.Source).ThenBy(r => r.Id),
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return ToDtos(page, categories);
    }

    /// <summary>The lines the filter asks for, before paging. Shared by the list and by its totals.</summary>
    internal async Task<(IQueryable<BookRow> Rows, IReadOnlyList<CategoryRef> Categories)> FilteredAsync(
        IBookRowFilter request,
        CancellationToken cancellationToken)
    {
        await BookRules.EnsureReadAsync(_context, _currentUserService, cancellationToken);

        var canSeePay = await FinanceRules.CanSeePayAsync(_context, _currentUserService, cancellationToken);
        var branchVehicles = await BranchScope.VehicleIdsAsync(_context, request.BranchId, cancellationToken);
        var branchTools = await BranchScope.ToolIdsAsync(_context, request.BranchId, cancellationToken);

        var categories = await _context.BookCategories
            .AsNoTracking()
            .Select(c => new CategoryRef(c.Id, c.Name, c.SystemKey))
            .ToListAsync(cancellationToken);

        var rows = BuildRows(request, canSeePay, branchVehicles, branchTools);

        if (request.From is { } from)
        {
            rows = rows.Where(r => r.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            rows = rows.Where(r => r.OccurredOn <= to);
        }

        if (request.Kind is { } kind)
        {
            var kindName = kind.ToString();
            rows = rows.Where(r => r.Kind == kindName);
        }

        if (request.CategoryId is { } categoryId)
        {
            // An entry of the book is filed by id; the rows read from other tables by the key
            // of the system category they belong to, so one filter finds both.
            var key = categories.FirstOrDefault(c => c.Id == categoryId)?.SystemKey;
            rows = rows.Where(r => r.CategoryId == categoryId || (key != null && r.CategoryKey == key));
        }

        if (request.ProjectId is { } projectId)
        {
            rows = rows.Where(r => r.ProjectId == projectId);
        }

        if (request.Open is { } open)
        {
            rows = rows.Where(r => r.IsOpen == open);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);
            rows = rows.Where(r =>
                (r.Counterparty != null && EF.Functions.Like(r.Counterparty.ToLower(), pattern, SearchPattern.Escape))
                || (r.Description != null && EF.Functions.Like(r.Description.ToLower(), pattern, SearchPattern.Escape)));
        }

        return (rows, categories);
    }

    internal sealed record CategoryRef(Guid Id, string Name, string? SystemKey);

    private static PagedList<BookRowDto> ToDtos(PagedList<BookRow> page, IReadOnlyList<CategoryRef> categories)
    {
        var byId = categories.ToDictionary(c => c.Id, c => c);
        var byKey = categories
            .Where(c => c.SystemKey != null)
            .ToDictionary(c => c.SystemKey!, c => c);

        var items = page.Items.Select(r =>
        {
            var category = r.CategoryId is { } id && byId.TryGetValue(id, out var byIdMatch)
                ? byIdMatch
                : r.CategoryKey is { } key && byKey.TryGetValue(key, out var byKeyMatch)
                    ? byKeyMatch
                    : null;

            return new BookRowDto
            {
                Source = r.Source,
                Id = r.Id,
                Kind = r.Kind,
                OccurredOn = r.OccurredOn,
                Amount = r.Amount,
                CategoryId = category?.Id,
                CategoryName = category?.Name,
                ProjectId = r.ProjectId,
                ProjectName = r.ProjectName,
                Counterparty = r.Counterparty,
                Description = r.Description,
                Method = r.Method,
                DueDate = r.DueDate,
                PaidOn = r.PaidOn,
                IsOpen = r.IsOpen,
                Editable = r.Source == nameof(Domain.Entities.BookEntry),
                BranchId = r.BranchId,
            };
        }).ToList();

        return new PagedList<BookRowDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }

    private IQueryable<BookRow> BuildRows(
        IBookRowFilter request,
        bool canSeePay,
        HashSet<Guid>? branchVehicles,
        HashSet<Guid>? branchTools)
    {
        var branchId = request.BranchId;

        var entries = _context.BookEntries.AsNoTracking().InBranch(branchId)
            .Select(e => new BookRow
            {
                Source = "BookEntry",
                Id = e.Id,
                Kind = e.Kind.ToString(),
                OccurredOn = e.OccurredOn,
                Amount = e.Amount,
                CategoryId = e.CategoryId,
                CategoryKey = e.Category.SystemKey,
                ProjectId = e.ProjectId,
                ProjectName = e.Project == null ? null : e.Project.Name,
                Counterparty = e.Counterparty,
                Description = e.Description,
                Method = e.Method == null ? null : e.Method.ToString(),
                DueDate = e.DueDate,
                PaidOn = e.PaidOn,
                // Only a cost or an income can be open; a payment or payout is money that already moved.
                IsOpen = e.PaidOn == null
                    && (e.Kind == BookEntryKind.Cost || e.Kind == BookEntryKind.Income),
                BranchId = e.BranchId,
            });

        var vehicleExpenses = _context.VehicleExpenses.AsNoTracking()
            .Where(e => e.Status == VehicleExpenseStatus.Approved)
            .Where(e => branchVehicles == null || branchVehicles.Contains(e.VehicleId))
            .Select(e => new BookRow
            {
                Source = "VehicleExpense",
                Id = e.Id,
                Kind = "Cost",
                OccurredOn = e.OccurredOn,
                Amount = e.Amount,
                CategoryId = null,
                CategoryKey = e.Kind == VehicleExpenseKind.Fuel
                    ? BookRules.Keys.Fuel
                    : e.Kind == VehicleExpenseKind.Insurance ? BookRules.Keys.Insurance : BookRules.Keys.Other,
                ProjectId = e.Vehicle.AssignedProjectId,
                ProjectName = e.Vehicle.AssignedProject == null ? null : e.Vehicle.AssignedProject.Name,
                Counterparty = e.Supplier,
                Description = e.Vehicle.Brand + " " + e.Vehicle.Model + " (" + e.Vehicle.RegistrationNumber + ")",
                Method = null,
                DueDate = null,
                PaidOn = e.OccurredOn,
                IsOpen = false,
                BranchId = e.Vehicle.BranchId,
            });

        var toolExpenses = _context.ToolExpenses.AsNoTracking()
            .Where(e => branchTools == null || branchTools.Contains(e.ToolId))
            .Select(e => new BookRow
            {
                Source = "ToolExpense",
                Id = e.Id,
                Kind = "Cost",
                OccurredOn = e.OccurredOn,
                Amount = e.Amount,
                CategoryId = null,
                CategoryKey = BookRules.Keys.Tools,
                ProjectId = e.Tool.AssignedProjectId,
                ProjectName = e.Tool.AssignedProject == null ? null : e.Tool.AssignedProject.Name,
                Counterparty = e.Supplier,
                Description = e.Tool.Name,
                Method = null,
                DueDate = null,
                PaidOn = e.OccurredOn,
                IsOpen = false,
                BranchId = e.Tool.BranchId,
            });

        var generalExpenses = _context.GeneralExpenses.AsNoTracking().InBranch(branchId)
            .Select(e => new BookRow
            {
                Source = "GeneralExpense",
                Id = e.Id,
                Kind = "Cost",
                OccurredOn = e.OccurredOn,
                Amount = e.Amount,
                CategoryId = null,
                CategoryKey = e.Category == GeneralExpenseCategory.Housing
                    ? BookRules.Keys.Housing
                    : e.Category == GeneralExpenseCategory.Bookkeeping ? BookRules.Keys.FixedCosts : BookRules.Keys.Other,
                ProjectId = e.ProjectId,
                ProjectName = e.Project == null ? null : e.Project.Name,
                Counterparty = e.Supplier,
                Description = e.Note,
                Method = null,
                DueDate = null,
                PaidOn = e.OccurredOn,
                IsOpen = false,
                BranchId = e.BranchId,
            });

        var projectRevenues = _context.ProjectRevenues.AsNoTracking().InBranch(branchId)
            .Select(r => new BookRow
            {
                Source = "ProjectRevenue",
                Id = r.Id,
                Kind = "Income",
                OccurredOn = r.OccurredOn,
                Amount = r.Amount,
                CategoryId = null,
                CategoryKey = BookRules.Keys.Collection,
                ProjectId = r.ProjectId,
                ProjectName = r.Project.Name,
                Counterparty = null,
                Description = r.Note,
                Method = null,
                DueDate = null,
                PaidOn = r.OccurredOn,
                IsOpen = false,
                BranchId = r.Project.BranchId,
            });

        var companyRevenues = _context.CompanyRevenues.AsNoTracking().InBranch(branchId)
            .Select(r => new BookRow
            {
                Source = "CompanyRevenue",
                Id = r.Id,
                Kind = "Income",
                OccurredOn = r.OccurredOn,
                Amount = r.Amount,
                CategoryId = null,
                CategoryKey = BookRules.Keys.Collection,
                ProjectId = null,
                ProjectName = null,
                Counterparty = null,
                Description = r.Note,
                Method = null,
                DueDate = null,
                PaidOn = r.OccurredOn,
                IsOpen = false,
                BranchId = r.BranchId,
            });

        var invoices = _context.Invoices.AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled)
            .Where(i => branchId == null
                || i.Project.Branch!.Path.Contains(BranchTree.Token(branchId.Value)))
            .Select(i => new BookRow
            {
                Source = "Invoice",
                Id = i.Id,
                Kind = "Income",
                OccurredOn = i.IssueDate,
                Amount = i.Amount,
                CategoryId = null,
                CategoryKey = BookRules.Keys.Collection,
                ProjectId = i.ProjectId,
                ProjectName = i.Project.Name,
                Counterparty = null,
                Description = i.Number,
                Method = null,
                DueDate = i.DueDate,
                PaidOn = null,
                IsOpen = i.Status == InvoiceStatus.Issued,
                BranchId = i.Project.BranchId,
            });

        var rows = entries
            .Concat(vehicleExpenses)
            .Concat(toolExpenses)
            .Concat(generalExpenses)
            .Concat(projectRevenues)
            .Concat(companyRevenues)
            .Concat(invoices);

        if (canSeePay)
        {
            rows = rows.Concat(_context.FinanceEntries.AsNoTracking().InBranch(branchId)
                .Select(e => new BookRow
                {
                    Source = "FinanceEntry",
                    Id = e.Id,
                    Kind = "Cost",
                    OccurredOn = e.OccurredOn,
                    Amount = e.Amount,
                    CategoryId = null,
                    CategoryKey = BookRules.Keys.WorkerPay,
                    ProjectId = e.ProjectId,
                    ProjectName = e.Project == null ? null : e.Project.Name,
                    Counterparty = e.Employee.FirstName + " " + e.Employee.LastName,
                    Description = e.Note,
                    Method = null,
                    DueDate = null,
                    PaidOn = e.OccurredOn,
                    IsOpen = false,
                    BranchId = e.BranchId,
                }));
        }

        return rows;
    }
}

public class BookTotalsDto
{
    /// <summary>Income and payments received: what came in.</summary>
    public decimal Inflow { get; init; }

    /// <summary>Costs and payouts: what went out.</summary>
    public decimal Outflow { get; init; }

    public int Count { get; init; }
}

/// <summary>
/// What the whole filtered book adds up to, not only the page on the screen. Income and payments are what
/// came in; costs and payouts are what went out.
/// </summary>
public record GetBookTotalsQuery : IBookRowFilter, IRequest<BookTotalsDto>
{
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public BookEntryKind? Kind { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? ProjectId { get; init; }

    public bool? Open { get; init; }

    public string? Search { get; init; }

    public Guid? BranchId { get; init; }
}

public class GetBookTotalsQueryHandler : IRequestHandler<GetBookTotalsQuery, BookTotalsDto>
{
    private readonly GetBookEntriesQueryHandler _entries;

    public GetBookTotalsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _entries = new GetBookEntriesQueryHandler(context, currentUserService);
    }

    public async Task<BookTotalsDto> Handle(GetBookTotalsQuery request, CancellationToken cancellationToken)
    {
        var (rows, _) = await _entries.FilteredAsync(request, cancellationToken);

        var inflowKinds = new[] { nameof(BookEntryKind.Income), nameof(BookEntryKind.Payment) };

        var inflow = await rows.Where(r => inflowKinds.Contains(r.Kind))
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;
        var outflow = await rows.Where(r => !inflowKinds.Contains(r.Kind))
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;
        var count = await rows.CountAsync(cancellationToken);

        return new BookTotalsDto { Inflow = inflow, Outflow = outflow, Count = count };
    }
}
