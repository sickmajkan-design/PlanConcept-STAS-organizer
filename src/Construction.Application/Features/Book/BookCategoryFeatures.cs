using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Book;

/// <summary>Lists the categories of the book, switched-off ones last.</summary>
public record GetBookCategoriesQuery : IRequest<IReadOnlyList<BookCategoryDto>>
{
    /// <summary>False leaves out the categories that were switched off, for a pick list.</summary>
    public bool IncludeInactive { get; init; } = true;

    /// <summary>The month whose entries are counted per category. Omitted means the current month.</summary>
    public int? Year { get; init; }

    public int? Month { get; init; }
}

public class GetBookCategoriesQueryHandler
    : IRequestHandler<GetBookCategoriesQuery, IReadOnlyList<BookCategoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    private readonly IDateTimeProvider _dateTimeProvider;

    public GetBookCategoriesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<BookCategoryDto>> Handle(
        GetBookCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        await BookRules.EnsureReadAsync(_context, _currentUserService, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var year = request.Year ?? now.Year;
        var month = request.Month ?? now.Month;
        var first = new DateOnly(year, Math.Clamp(month, 1, 12), 1);
        var last = first.AddMonths(1).AddDays(-1);

        var query = _context.BookCategories.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new BookCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Kind = c.Kind.ToString(),
                Group = c.Group,
                Bucket = c.Bucket.ToString(),
                SystemKey = c.SystemKey,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder,
                EntryCount = _context.BookEntries.Count(e => e.CategoryId == c.Id),
                MonthEntryCount = _context.BookEntries.Count(
                    e => e.CategoryId == c.Id && e.OccurredOn >= first && e.OccurredOn <= last),
            })
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Adds a category of the owner's own.</summary>
public record CreateBookCategoryCommand : IRequest<BookCategoryDto>
{
    public string Name { get; init; } = null!;

    public BookCategoryKind Kind { get; init; }

    public string? Group { get; init; }

    public BookCategoryBucket Bucket { get; init; }

    public int SortOrder { get; init; }
}

public class CreateBookCategoryCommandValidator : AbstractValidator<CreateBookCategoryCommand>
{
    public CreateBookCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Group).MaximumLength(100);
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Bucket).IsInEnum();
    }
}

public class CreateBookCategoryCommandHandler : IRequestHandler<CreateBookCategoryCommand, BookCategoryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateBookCategoryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BookCategoryDto> Handle(CreateBookCategoryCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        var name = request.Name.Trim();

        if (await _context.BookCategories.AnyAsync(c => c.Name == name, cancellationToken))
        {
            throw new ConflictException("A category with that name already exists.");
        }

        var category = new BookCategory
        {
            Name = name,
            Kind = request.Kind,
            Group = string.IsNullOrWhiteSpace(request.Group) ? null : request.Group.Trim(),
            Bucket = request.Bucket,
            SortOrder = request.SortOrder,
            IsActive = true,
        };

        _context.BookCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(category, 0);
    }

    internal static BookCategoryDto ToDto(BookCategory c, int entryCount) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Kind = c.Kind.ToString(),
        Group = c.Group,
        Bucket = c.Bucket.ToString(),
        SystemKey = c.SystemKey,
        IsActive = c.IsActive,
        SortOrder = c.SortOrder,
        EntryCount = entryCount,
    };
}

/// <summary>
/// Renames, regroups, reorders or switches a category on or off. The kind and the bucket are fixed
/// once entries are filed under it, because that would silently move past figures.
/// </summary>
public record UpdateBookCategoryCommand : IRequest<BookCategoryDto>
{
    /// <summary>Taken from the route.</summary>
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Group { get; init; }

    public int SortOrder { get; init; }

    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Where the category is counted. Omitted keeps it. It cannot be changed for a category the system
    /// seeds, because the settlement is built on where those go.
    /// </summary>
    public BookCategoryBucket? Bucket { get; init; }
}

public class UpdateBookCategoryCommandValidator : AbstractValidator<UpdateBookCategoryCommand>
{
    public UpdateBookCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Group).MaximumLength(100);
    }
}

public class UpdateBookCategoryCommandHandler : IRequestHandler<UpdateBookCategoryCommand, BookCategoryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateBookCategoryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BookCategoryDto> Handle(UpdateBookCategoryCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        var category = await _context.BookCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BookCategory), request.Id);

        if (!request.IsActive && category.SystemKey is not null)
        {
            throw new ConflictException(
                "A category the system relies on can be renamed but not switched off.");
        }

        var name = request.Name.Trim();

        if (await _context.BookCategories.AnyAsync(c => c.Id != category.Id && c.Name == name, cancellationToken))
        {
            throw new ConflictException("A category with that name already exists.");
        }

        if (request.Bucket is { } bucket && bucket != category.Bucket)
        {
            if (category.SystemKey is not null)
            {
                throw new ConflictException(
                    "Where a category the system relies on is counted cannot be changed.");
            }

            // An income stays in billing and a cost cannot become billing: the settlement is built on it.
            if ((category.Kind == BookCategoryKind.Income) != (bucket == BookCategoryBucket.Collection))
            {
                throw new ConflictException(
                    "Income is counted in billing, and a cost in worker pay, site costs or fixed costs.");
            }

            category.Bucket = bucket;
        }

        category.Name = name;
        category.Group = string.IsNullOrWhiteSpace(request.Group) ? null : request.Group.Trim();
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        var count = await _context.BookEntries.CountAsync(e => e.CategoryId == category.Id, cancellationToken);

        return CreateBookCategoryCommandHandler.ToDto(category, count);
    }
}

public record GetBookCategoryRulesQuery : IRequest<IReadOnlyList<BookCategoryRuleDto>>;

public class GetBookCategoryRulesQueryHandler
    : IRequestHandler<GetBookCategoryRulesQuery, IReadOnlyList<BookCategoryRuleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBookCategoryRulesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<BookCategoryRuleDto>> Handle(
        GetBookCategoryRulesQuery request,
        CancellationToken cancellationToken)
    {
        await BookRules.EnsureReadAsync(_context, _currentUserService, cancellationToken);

        return await _context.BookCategoryRules
            .AsNoTracking()
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Text)
            .Select(r => new BookCategoryRuleDto
            {
                Id = r.Id,
                Text = r.Text,
                CategoryId = r.CategoryId,
                CategoryName = r.Category.Name,
                Priority = r.Priority,
            })
            .ToListAsync(cancellationToken);
    }
}

/// <summary>Adds a rule: a description containing this text is suggested to go in that category.</summary>
public record CreateBookCategoryRuleCommand : IRequest<BookCategoryRuleDto>
{
    public string Text { get; init; } = null!;

    public Guid CategoryId { get; init; }

    /// <summary>Lower runs first. When left at zero the rule goes after the existing ones.</summary>
    public int Priority { get; init; }
}

public class CreateBookCategoryRuleCommandValidator : AbstractValidator<CreateBookCategoryRuleCommand>
{
    public CreateBookCategoryRuleCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
    }
}

public class CreateBookCategoryRuleCommandHandler
    : IRequestHandler<CreateBookCategoryRuleCommand, BookCategoryRuleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateBookCategoryRuleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BookCategoryRuleDto> Handle(
        CreateBookCategoryRuleCommand request,
        CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        var category = await _context.BookCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(BookCategory), request.CategoryId);

        var priority = request.Priority > 0
            ? request.Priority
            : (await _context.BookCategoryRules.MaxAsync(r => (int?)r.Priority, cancellationToken) ?? 0) + 1;

        var rule = new BookCategoryRule
        {
            Text = request.Text.Trim(),
            CategoryId = category.Id,
            Priority = priority,
        };

        _context.BookCategoryRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        return new BookCategoryRuleDto
        {
            Id = rule.Id,
            Text = rule.Text,
            CategoryId = category.Id,
            CategoryName = category.Name,
            Priority = rule.Priority,
        };
    }
}

public record DeleteBookCategoryRuleCommand(Guid Id) : IRequest;

public class DeleteBookCategoryRuleCommandHandler : IRequestHandler<DeleteBookCategoryRuleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteBookCategoryRuleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteBookCategoryRuleCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        var rule = await _context.BookCategoryRules
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BookCategoryRule), request.Id);

        _context.BookCategoryRules.Remove(rule);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Suggests a category for a description: the first rule by priority whose text it contains, in any
/// case, whose category is still switched on. Only ever a suggestion.
/// </summary>
public record SuggestBookCategoryQuery(string? Text) : IRequest<BookCategorySuggestionDto>;

public class SuggestBookCategoryQueryHandler : IRequestHandler<SuggestBookCategoryQuery, BookCategorySuggestionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SuggestBookCategoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BookCategorySuggestionDto> Handle(
        SuggestBookCategoryQuery request,
        CancellationToken cancellationToken)
    {
        await BookRules.EnsureReadAsync(_context, _currentUserService, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return new BookCategorySuggestionDto();
        }

        var rules = await _context.BookCategoryRules
            .AsNoTracking()
            .Where(r => r.Category.IsActive)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Text)
            .Select(r => new { r.Text, r.CategoryId, CategoryName = r.Category.Name })
            .ToListAsync(cancellationToken);

        var match = rules.FirstOrDefault(r =>
            request.Text.Contains(r.Text, StringComparison.OrdinalIgnoreCase));

        return match is null
            ? new BookCategorySuggestionDto()
            : new BookCategorySuggestionDto
            {
                CategoryId = match.CategoryId,
                CategoryName = match.CategoryName,
                MatchedRule = match.Text,
            };
    }
}
