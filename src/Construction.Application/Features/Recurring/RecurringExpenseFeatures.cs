using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Book;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Recurring;

public class RecurringExpenseDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = null!;

    public decimal Amount { get; init; }

    /// <summary>"Monthly", "Quarterly" or "Annual".</summary>
    public string Frequency { get; init; } = null!;

    public DateOnly StartMonth { get; init; }

    public int DayOfMonth { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public string? CountryCode { get; init; }

    public bool IsActive { get; init; }

    public DateOnly? EndDate { get; init; }

    public Guid? BranchId { get; init; }

    /// <summary>How many entries of the book it has made so far.</summary>
    public int EntriesMade { get; init; }
}

/// <summary>One line of what a month would make (or has made) from the templates.</summary>
public class RecurringPreviewRowDto
{
    public Guid RecurringExpenseId { get; init; }

    public string Name { get; init; } = null!;

    public string CategoryName { get; init; } = null!;

    public decimal Amount { get; init; }

    public DateOnly DueDate { get; init; }

    /// <summary>The entry already made for this period, when there is one.</summary>
    public Guid? EntryId { get; init; }

    public bool AlreadyMade => EntryId is not null;
}

/// <summary>Which periods a template falls in, and what an entry for one looks like. Shared by every path that makes entries.</summary>
public static class RecurringSchedule
{
    public static bool Applies(RecurringExpense template, int year, int month)
    {
        if (!template.IsActive)
        {
            return false;
        }

        var first = new DateOnly(year, month, 1);

        if (first < new DateOnly(template.StartMonth.Year, template.StartMonth.Month, 1))
        {
            return false;
        }

        if (template.EndDate is { } end && first > end)
        {
            return false;
        }

        var months = (year * 12 + month) - (template.StartMonth.Year * 12 + template.StartMonth.Month);

        return months % (int)template.Frequency == 0;
    }

    public static DateOnly DueDate(RecurringExpense template, int year, int month) =>
        new(year, month, Math.Clamp(template.DayOfMonth, 1, 28));

    /// <summary>
    /// Makes the entries a month's templates still owe. Idempotent: a period that already has its entry is
    /// skipped, and so is one whose entry was deleted, because the unique index keeps its place.
    /// </summary>
    /// <param name="onlyDue">True for the automatic job: an entry is made on its day, not before.</param>
    public static async Task<int> GenerateAsync(
        IApplicationDbContext context,
        int year,
        int month,
        DateOnly today,
        bool onlyDue,
        Guid? recordedBy,
        CancellationToken cancellationToken)
    {
        var templates = await context.RecurringExpenses
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        var made = await context.BookEntries
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.RecurringExpenseId != null && e.PeriodYear == year && e.PeriodMonth == month)
            .Select(e => e.RecurringExpenseId!.Value)
            .ToListAsync(cancellationToken);

        var created = 0;

        foreach (var template in templates)
        {
            if (!Applies(template, year, month) || made.Contains(template.Id))
            {
                continue;
            }

            var due = DueDate(template, year, month);

            if (onlyDue && due > today)
            {
                continue;
            }

            context.BookEntries.Add(new BookEntry
            {
                Kind = BookEntryKind.Cost,
                // The book does not hold an entry dated in the future; the due date carries the real day.
                OccurredOn = due > today ? today : due,
                Amount = template.Amount,
                CategoryId = template.CategoryId,
                ProjectId = template.ProjectId,
                BranchId = template.BranchId,
                Description = template.Name,
                DueDate = due,
                Source = BookEntrySource.Template,
                SourceId = null,
                RecurringExpenseId = template.Id,
                PeriodYear = year,
                PeriodMonth = month,
                RecordedByUserId = recordedBy,
            });

            created++;
        }

        if (created > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return created;
    }
}

public class RecurringExpenseInputValidator<T> : AbstractValidator<T>
    where T : IRecurringExpenseInput
{
    public RecurringExpenseInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0).LessThan(1_000_000_000m);
        RuleFor(x => x.Frequency).IsInEnum();
        RuleFor(x => x.DayOfMonth).InclusiveBetween(1, 28);
        RuleFor(x => x.CountryCode).Length(2).When(x => !string.IsNullOrWhiteSpace(x.CountryCode));

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartMonth)
            .When(x => x.EndDate is not null)
            .WithMessage("The end cannot be before the start.");
    }
}

public interface IRecurringExpenseInput
{
    string Name { get; }

    Guid CategoryId { get; }

    decimal Amount { get; }

    RecurringFrequency Frequency { get; }

    DateOnly StartMonth { get; }

    int DayOfMonth { get; }

    Guid? ProjectId { get; }

    string? CountryCode { get; }

    DateOnly? EndDate { get; }

    Guid? BranchId { get; }
}

internal static class RecurringRules
{
    public static async Task EnsureReferencesAsync(
        IApplicationDbContext context, IRecurringExpenseInput input, CancellationToken cancellationToken)
    {
        var category = await context.BookCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == input.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(BookCategory), input.CategoryId);

        if (category.Kind != BookCategoryKind.Cost || !category.IsActive)
        {
            throw new ConflictException("A recurring cost must be filed under a cost category that is in use.");
        }

        await Branches.BranchLookup.EnsureExistsAsync(context, input.BranchId, cancellationToken);

        if (input.ProjectId is { } projectId
            && !await context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), projectId);
        }
    }

    public static Task<RecurringExpenseDto> ReadAsync(
        IApplicationDbContext context, Guid id, CancellationToken cancellationToken) =>
        context.RecurringExpenses
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RecurringExpenseDto
            {
                Id = r.Id,
                Name = r.Name,
                CategoryId = r.CategoryId,
                CategoryName = r.Category.Name,
                Amount = r.Amount,
                Frequency = r.Frequency.ToString(),
                StartMonth = r.StartMonth,
                DayOfMonth = r.DayOfMonth,
                ProjectId = r.ProjectId,
                ProjectName = r.Project != null ? r.Project.Name : null,
                CountryCode = r.CountryCode,
                IsActive = r.IsActive,
                EndDate = r.EndDate,
                BranchId = r.BranchId,
                EntriesMade = context.BookEntries.IgnoreQueryFilters().Count(e => e.RecurringExpenseId == r.Id),
            })
            .FirstAsync(cancellationToken);

    public static string? Country(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
}

public record GetRecurringExpensesQuery : IRequest<IReadOnlyList<RecurringExpenseDto>>;

public class GetRecurringExpensesQueryHandler
    : IRequestHandler<GetRecurringExpensesQuery, IReadOnlyList<RecurringExpenseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetRecurringExpensesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<RecurringExpenseDto>> Handle(
        GetRecurringExpensesQuery request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureReadAsync(_context, _currentUserService, cancellationToken);

        return await _context.RecurringExpenses
            .AsNoTracking()
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.Name)
            .Select(r => new RecurringExpenseDto
            {
                Id = r.Id,
                Name = r.Name,
                CategoryId = r.CategoryId,
                CategoryName = r.Category.Name,
                Amount = r.Amount,
                Frequency = r.Frequency.ToString(),
                StartMonth = r.StartMonth,
                DayOfMonth = r.DayOfMonth,
                ProjectId = r.ProjectId,
                ProjectName = r.Project != null ? r.Project.Name : null,
                CountryCode = r.CountryCode,
                IsActive = r.IsActive,
                EndDate = r.EndDate,
                BranchId = r.BranchId,
                EntriesMade = _context.BookEntries.IgnoreQueryFilters().Count(e => e.RecurringExpenseId == r.Id),
            })
            .ToListAsync(cancellationToken);
    }
}

public record CreateRecurringExpenseCommand : IRequest<RecurringExpenseDto>, IRecurringExpenseInput
{
    public string Name { get; init; } = null!;

    public Guid CategoryId { get; init; }

    public decimal Amount { get; init; }

    public RecurringFrequency Frequency { get; init; }

    public DateOnly StartMonth { get; init; }

    public int DayOfMonth { get; init; }

    public Guid? ProjectId { get; init; }

    public string? CountryCode { get; init; }

    public DateOnly? EndDate { get; init; }

    public Guid? BranchId { get; init; }
}

public class CreateRecurringExpenseCommandValidator : AbstractValidator<CreateRecurringExpenseCommand>
{
    public CreateRecurringExpenseCommandValidator()
    {
        Include(new RecurringExpenseInputValidator<CreateRecurringExpenseCommand>());
    }
}

public class CreateRecurringExpenseCommandHandler
    : IRequestHandler<CreateRecurringExpenseCommand, RecurringExpenseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateRecurringExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<RecurringExpenseDto> Handle(
        CreateRecurringExpenseCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);
        await RecurringRules.EnsureReferencesAsync(_context, request, cancellationToken);

        var template = new RecurringExpense
        {
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Frequency = request.Frequency,
            StartMonth = new DateOnly(request.StartMonth.Year, request.StartMonth.Month, 1),
            DayOfMonth = request.DayOfMonth,
            ProjectId = request.ProjectId,
            CountryCode = RecurringRules.Country(request.CountryCode),
            EndDate = request.EndDate,
            BranchId = request.BranchId,
            IsActive = true,
        };

        _context.RecurringExpenses.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return await RecurringRules.ReadAsync(_context, template.Id, cancellationToken);
    }
}

/// <summary>Changes a template. The new amount applies from the next period; entries already made keep theirs.</summary>
public record UpdateRecurringExpenseCommand : IRequest<RecurringExpenseDto>, IRecurringExpenseInput
{
    /// <summary>Taken from the route.</summary>
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public Guid CategoryId { get; init; }

    public decimal Amount { get; init; }

    public RecurringFrequency Frequency { get; init; }

    public DateOnly StartMonth { get; init; }

    public int DayOfMonth { get; init; }

    public Guid? ProjectId { get; init; }

    public string? CountryCode { get; init; }

    public DateOnly? EndDate { get; init; }

    public Guid? BranchId { get; init; }
}

public class UpdateRecurringExpenseCommandValidator : AbstractValidator<UpdateRecurringExpenseCommand>
{
    public UpdateRecurringExpenseCommandValidator()
    {
        Include(new RecurringExpenseInputValidator<UpdateRecurringExpenseCommand>());
    }
}

public class UpdateRecurringExpenseCommandHandler
    : IRequestHandler<UpdateRecurringExpenseCommand, RecurringExpenseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateRecurringExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<RecurringExpenseDto> Handle(
        UpdateRecurringExpenseCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        var template = await _context.RecurringExpenses
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(RecurringExpense), request.Id);

        await RecurringRules.EnsureReferencesAsync(_context, request, cancellationToken);

        template.Name = request.Name.Trim();
        template.CategoryId = request.CategoryId;
        template.Amount = request.Amount;
        template.Frequency = request.Frequency;
        template.StartMonth = new DateOnly(request.StartMonth.Year, request.StartMonth.Month, 1);
        template.DayOfMonth = request.DayOfMonth;
        template.ProjectId = request.ProjectId;
        template.CountryCode = RecurringRules.Country(request.CountryCode);
        template.EndDate = request.EndDate;
        template.BranchId = request.BranchId;

        await _context.SaveChangesAsync(cancellationToken);

        return await RecurringRules.ReadAsync(_context, template.Id, cancellationToken);
    }
}

/// <summary>Switches a template on or off. A template that is off makes nothing more, and keeps what it made.</summary>
public record ToggleRecurringExpenseCommand(Guid Id) : IRequest<RecurringExpenseDto>;

public class ToggleRecurringExpenseCommandHandler
    : IRequestHandler<ToggleRecurringExpenseCommand, RecurringExpenseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ToggleRecurringExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<RecurringExpenseDto> Handle(
        ToggleRecurringExpenseCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        var template = await _context.RecurringExpenses
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(RecurringExpense), request.Id);

        template.IsActive = !template.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return await RecurringRules.ReadAsync(_context, template.Id, cancellationToken);
    }
}

/// <summary>What a month would make from the templates, and what it already made.</summary>
public record GetRecurringPreviewQuery(int Year, int Month) : IRequest<IReadOnlyList<RecurringPreviewRowDto>>;

public class GetRecurringPreviewQueryValidator : AbstractValidator<GetRecurringPreviewQuery>
{
    public GetRecurringPreviewQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class GetRecurringPreviewQueryHandler
    : IRequestHandler<GetRecurringPreviewQuery, IReadOnlyList<RecurringPreviewRowDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetRecurringPreviewQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<RecurringPreviewRowDto>> Handle(
        GetRecurringPreviewQuery request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureReadAsync(_context, _currentUserService, cancellationToken);

        var templates = await _context.RecurringExpenses
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        var made = await _context.BookEntries
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.RecurringExpenseId != null
                && e.PeriodYear == request.Year && e.PeriodMonth == request.Month)
            .Select(e => new { Id = e.RecurringExpenseId!.Value, Entry = e.Id })
            .ToListAsync(cancellationToken);

        return templates
            .Where(t => RecurringSchedule.Applies(t, request.Year, request.Month))
            .Select(t => new RecurringPreviewRowDto
            {
                RecurringExpenseId = t.Id,
                Name = t.Name,
                CategoryName = t.Category.Name,
                Amount = t.Amount,
                DueDate = RecurringSchedule.DueDate(t, request.Year, request.Month),
                EntryId = made.FirstOrDefault(m => m.Id == t.Id)?.Entry,
            })
            .OrderBy(r => r.DueDate)
            .ThenBy(r => r.Name)
            .ToList();
    }
}

/// <summary>Makes the entries a month still owes from the templates. Safe to run again: it makes each only once.</summary>
public record GenerateRecurringExpensesCommand(int Year, int Month) : IRequest<int>;

public class GenerateRecurringExpensesCommandValidator : AbstractValidator<GenerateRecurringExpensesCommand>
{
    public GenerateRecurringExpensesCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class GenerateRecurringExpensesCommandHandler : IRequestHandler<GenerateRecurringExpensesCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GenerateRecurringExpensesCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(GenerateRecurringExpensesCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        var today = _dateTimeProvider.Today;

        if (new DateOnly(request.Year, request.Month, 1) > new DateOnly(today.Year, today.Month, 1))
        {
            throw new ConflictException("Entries are made for the current month or an earlier one, not for a month to come.");
        }

        return await RecurringSchedule.GenerateAsync(
            _context, request.Year, request.Month, today, onlyDue: false, _currentUserService.UserId, cancellationToken);
    }
}

/// <summary>The automatic daily pass: makes this month's entries whose day has come. Runs without a user.</summary>
public record GenerateDueRecurringExpensesCommand : IRequest<int>;

public class GenerateDueRecurringExpensesCommandHandler : IRequestHandler<GenerateDueRecurringExpensesCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GenerateDueRecurringExpensesCommandHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(GenerateDueRecurringExpensesCommand request, CancellationToken cancellationToken)
    {
        var today = _dateTimeProvider.Today;

        return await RecurringSchedule.GenerateAsync(
            _context, today.Year, today.Month, today, onlyDue: true, recordedBy: null, cancellationToken);
    }
}
