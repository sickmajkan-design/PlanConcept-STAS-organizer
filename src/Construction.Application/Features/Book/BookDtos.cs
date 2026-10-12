using System.Linq.Expressions;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Book;

/// <summary>One line of the book: an entry of its own, or a figure read from the table that owns it.</summary>
public class BookRowDto
{
    /// <summary>
    /// "BookEntry", "VehicleExpense", "ToolExpense", "GeneralExpense", "FinanceEntry",
    /// "ProjectRevenue", "CompanyRevenue" or "Invoice" — where the figure is kept, and so where it is changed.
    /// </summary>
    public string Source { get; init; } = null!;

    public Guid Id { get; init; }

    /// <summary>"Income", "Cost", "Payment" or "Payout".</summary>
    public string Kind { get; init; } = null!;

    public DateOnly OccurredOn { get; init; }

    public decimal Amount { get; init; }

    public Guid? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public string? Counterparty { get; init; }

    public string? Description { get; init; }

    /// <summary>"Bank" or "Cash", when it was recorded.</summary>
    public string? Method { get; init; }

    public DateOnly? DueDate { get; init; }

    public DateOnly? PaidOn { get; init; }

    /// <summary>Not paid yet — a cost still owed or an invoice still open.</summary>
    public bool IsOpen { get; init; }

    /// <summary>Only an entry of the book itself can be changed or deleted here; the rest is changed where it was recorded.</summary>
    public bool Editable { get; init; }

    public Guid? BranchId { get; init; }
}

/// <summary>The common shape every source is projected to so they can be listed, sorted and paged as one.</summary>
internal class BookRow
{
    public string Source { get; set; } = null!;

    public Guid Id { get; set; }

    public string Kind { get; set; } = null!;

    public DateOnly OccurredOn { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Set for an entry of the book; the other sources are filed by <see cref="CategoryKey"/>.</summary>
    public Guid? CategoryId { get; set; }

    public string? CategoryKey { get; set; }

    public Guid? ProjectId { get; set; }

    public string? ProjectName { get; set; }

    public string? Counterparty { get; set; }

    public string? Description { get; set; }

    public string? Method { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? PaidOn { get; set; }

    public bool IsOpen { get; set; }

    public Guid? BranchId { get; set; }
}

/// <summary>An entry of the book, as the quick-entry form and the edit dialog see it.</summary>
public class BookEntryDto
{
    public Guid Id { get; init; }

    public string Kind { get; init; } = null!;

    public DateOnly OccurredOn { get; init; }

    public decimal Amount { get; init; }

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = null!;

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public Guid? EmployeeId { get; init; }

    public Guid? CustomerId { get; init; }

    public string? Counterparty { get; init; }

    public string? Description { get; init; }

    public string? Method { get; init; }

    public DateOnly? DueDate { get; init; }

    public DateOnly? PaidOn { get; init; }

    public Guid? BranchId { get; init; }

    public string? BranchName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public static class BookEntryMapping
{
    public static readonly Expression<Func<BookEntry, BookEntryDto>> Projection = e => new BookEntryDto
    {
        Id = e.Id,
        Kind = e.Kind.ToString(),
        OccurredOn = e.OccurredOn,
        Amount = e.Amount,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.Name,
        ProjectId = e.ProjectId,
        ProjectName = e.Project == null ? null : e.Project.Name,
        EmployeeId = e.EmployeeId,
        CustomerId = e.CustomerId,
        Counterparty = e.Counterparty,
        Description = e.Description,
        Method = e.Method == null ? null : e.Method.ToString(),
        DueDate = e.DueDate,
        PaidOn = e.PaidOn,
        BranchId = e.BranchId,
        BranchName = e.Branch == null ? null : e.Branch.Name,
        CreatedAt = e.CreatedAt,
    };
}

public class BookCategoryDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    /// <summary>"Income" or "Cost".</summary>
    public string Kind { get; init; } = null!;

    public string? Group { get; init; }

    /// <summary>"Collection", "WorkerEarnings", "SiteCosts" or "Fixed".</summary>
    public string Bucket { get; init; } = null!;

    /// <summary>Set for the categories the system seeds; those can be renamed but never switched off.</summary>
    public string? SystemKey { get; init; }

    public bool IsActive { get; init; }

    public int SortOrder { get; init; }

    /// <summary>How many entries of the book are filed under it.</summary>
    public int EntryCount { get; init; }
}

public class BookCategoryRuleDto
{
    public Guid Id { get; init; }

    public string Text { get; init; } = null!;

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = null!;

    public int Priority { get; init; }
}

public class BookCategorySuggestionDto
{
    public Guid? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    /// <summary>The text of the rule that matched, so the screen can say why.</summary>
    public string? MatchedRule { get; init; }
}
