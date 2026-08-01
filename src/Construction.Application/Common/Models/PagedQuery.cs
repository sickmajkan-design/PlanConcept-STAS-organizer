using FluentValidation;

namespace Construction.Application.Common.Models;

/// <summary>A request for one page of a list endpoint.</summary>
public interface IPagedQuery
{
    int PageNumber { get; }

    int PageSize { get; }
}

/// <summary>
/// A paged request that also lets the caller choose the ordering. The chosen
/// field is checked against an allow-list rather than passed through, so a
/// client can never steer the SQL.
/// </summary>
public interface ISortablePagedQuery : IPagedQuery
{
    string? SortBy { get; }

    bool SortDescending { get; }
}

/// <summary>
/// Page bounds, shared by every list endpoint so the limits and their wording
/// are defined once.
/// </summary>
public abstract class PagedQueryValidator<T> : AbstractValidator<T>
    where T : IPagedQuery
{
    public const int DefaultMaxPageSize = 100;

    protected PagedQueryValidator(int maxPageSize = DefaultMaxPageSize)
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, maxPageSize)
            .WithMessage($"Page size must be between 1 and {maxPageSize}.");
    }
}

/// <summary>Page bounds plus the sort-field allow-list.</summary>
public abstract class SortablePagedQueryValidator<T> : PagedQueryValidator<T>
    where T : ISortablePagedQuery
{
    protected SortablePagedQueryValidator(
        IReadOnlyCollection<string> allowedSortFields,
        int maxPageSize = DefaultMaxPageSize)
        : base(maxPageSize)
    {
        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) ||
                            allowedSortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", allowedSortFields)}.");
    }
}
