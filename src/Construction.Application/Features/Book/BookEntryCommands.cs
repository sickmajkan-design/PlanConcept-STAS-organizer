using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Book;

/// <summary>What the create and change commands have in common, so one set of rules covers both.</summary>
public interface IBookEntryInput
{
    BookEntryKind Kind { get; }

    decimal Amount { get; }

    DateOnly OccurredOn { get; }

    Guid? CategoryId { get; }

    Guid? ProjectId { get; }

    Guid? EmployeeId { get; }

    Guid? CustomerId { get; }

    Guid? BranchId { get; }

    string? Counterparty { get; }

    string? Description { get; }

    BookPaymentMethod? Method { get; }

    DateOnly? DueDate { get; }

    DateOnly? PaidOn { get; }
}

internal sealed class BookEntryInputValidator<T> : AbstractValidator<T>
    where T : IBookEntryInput
{
    public BookEntryInputValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.Kind).IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("An amount must be more than zero.")
            .LessThan(1_000_000_000m).WithMessage("That amount is too large.");

        RuleFor(x => x.OccurredOn)
            .LessThanOrEqualTo(today)
            .WithMessage("An entry cannot be dated in the future. Use a due date for what is still to come.");

        RuleFor(x => x.PaidOn)
            .LessThanOrEqualTo(today)
            .WithMessage("Something cannot have been paid in the future.")
            .When(x => x.PaidOn is not null);

        RuleFor(x => x.Method).IsInEnum().When(x => x.Method is not null);

        RuleFor(x => x.Counterparty).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal static class BookEntryRules
{
    /// <summary>
    /// Finds the category an entry is filed under. An income or a cost must name one of the matching
    /// kind; a payment or payout does not need to, and falls back to the category it belongs to.
    /// </summary>
    public static async Task<BookCategory> ResolveCategoryAsync(
        IApplicationDbContext context,
        IBookEntryInput input,
        CancellationToken cancellationToken)
    {
        if (input.CategoryId is { } categoryId)
        {
            var category = await context.BookCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken)
                ?? throw new NotFoundException(nameof(BookCategory), categoryId);

            if (!category.IsActive)
            {
                throw new ConflictException("That category is switched off.");
            }

            var expected = input.Kind is BookEntryKind.Income or BookEntryKind.Payment
                ? BookCategoryKind.Income
                : BookCategoryKind.Cost;

            if (input.Kind is BookEntryKind.Income or BookEntryKind.Cost && category.Kind != expected)
            {
                throw new ConflictException(
                    input.Kind == BookEntryKind.Income
                        ? "An income must be filed under an income category."
                        : "A cost must be filed under a cost category.");
            }

            return category;
        }

        if (input.Kind is BookEntryKind.Income or BookEntryKind.Cost)
        {
            throw new ConflictException("Choose a category.");
        }

        var key = input.Kind == BookEntryKind.Payment ? BookRules.Keys.Collection : BookRules.Keys.WorkerPay;

        return await context.BookCategories.FirstOrDefaultAsync(c => c.SystemKey == key, cancellationToken)
            ?? throw new NotFoundException(nameof(BookCategory), key);
    }

    public static async Task EnsureReferencesExistAsync(
        IApplicationDbContext context,
        IBookEntryInput input,
        CancellationToken cancellationToken)
    {
        await Branches.BranchLookup.EnsureExistsAsync(context, input.BranchId, cancellationToken);

        if (input.ProjectId is { } projectId
            && !await context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        if (input.EmployeeId is { } employeeId
            && !await context.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken))
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }

        if (input.CustomerId is { } customerId
            && !await context.Customers.AnyAsync(c => c.Id == customerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Customer), customerId);
        }
    }

    /// <summary>A payment or payout is money that moved, so it is paid the day it happened.</summary>
    public static DateOnly? PaidOn(IBookEntryInput input) =>
        input.Kind is BookEntryKind.Payment or BookEntryKind.Payout
            ? input.PaidOn ?? input.OccurredOn
            : input.PaidOn;

    public static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Writes a line into the book of money.</summary>
public record CreateBookEntryCommand : IRequest<BookEntryDto>, IBookEntryInput
{
    public BookEntryKind Kind { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? ProjectId { get; init; }

    public Guid? EmployeeId { get; init; }

    public Guid? CustomerId { get; init; }

    /// <summary>The business unit this is booked to. Null follows the project, if any.</summary>
    public Guid? BranchId { get; init; }

    public string? Counterparty { get; init; }

    public string? Description { get; init; }

    public BookPaymentMethod? Method { get; init; }

    public DateOnly? DueDate { get; init; }

    public DateOnly? PaidOn { get; init; }

    /// <summary>
    /// A client that may send the entry twice (a phone that was offline) sets this. Sending it again
    /// returns the entry that was already made instead of making another.
    /// </summary>
    public Guid? ClientRequestId { get; init; }
}

public class CreateBookEntryCommandValidator : AbstractValidator<CreateBookEntryCommand>
{
    public CreateBookEntryCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        Include(new BookEntryInputValidator<CreateBookEntryCommand>(dateTimeProvider));
    }
}

public class CreateBookEntryCommandHandler : IRequestHandler<CreateBookEntryCommand, BookEntryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateBookEntryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BookEntryDto> Handle(CreateBookEntryCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        if (request.ClientRequestId is { } clientRequestId)
        {
            var existing = await _context.BookEntries
                .AsNoTracking()
                .Where(e => e.ClientRequestId == clientRequestId)
                .Select(BookEntryMapping.Projection)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                return existing;
            }
        }

        await BookEntryRules.EnsureReferencesExistAsync(_context, request, cancellationToken);
        var category = await BookEntryRules.ResolveCategoryAsync(_context, request, cancellationToken);

        var entry = new BookEntry
        {
            Kind = request.Kind,
            OccurredOn = request.OccurredOn,
            Amount = request.Amount,
            CategoryId = category.Id,
            ProjectId = request.ProjectId,
            EmployeeId = request.EmployeeId,
            CustomerId = request.CustomerId,
            BranchId = request.BranchId,
            Counterparty = BookEntryRules.Clean(request.Counterparty),
            Description = BookEntryRules.Clean(request.Description),
            Method = request.Method,
            DueDate = request.DueDate,
            PaidOn = BookEntryRules.PaidOn(request),
            Source = BookEntrySource.Manual,
            ClientRequestId = request.ClientRequestId,
            RecordedByUserId = _currentUserService.UserId,
        };

        _context.BookEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.BookEntries
            .AsNoTracking()
            .Where(e => e.Id == entry.Id)
            .Select(BookEntryMapping.Projection)
            .SingleAsync(cancellationToken);
    }
}

/// <summary>Corrects a line of the book. Only entries of the book itself can be changed here.</summary>
public record UpdateBookEntryCommand : IRequest<BookEntryDto>, IBookEntryInput
{
    /// <summary>Taken from the route.</summary>
    public Guid Id { get; init; }

    public BookEntryKind Kind { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? ProjectId { get; init; }

    public Guid? EmployeeId { get; init; }

    public Guid? CustomerId { get; init; }

    public Guid? BranchId { get; init; }

    public string? Counterparty { get; init; }

    public string? Description { get; init; }

    public BookPaymentMethod? Method { get; init; }

    public DateOnly? DueDate { get; init; }

    public DateOnly? PaidOn { get; init; }
}

public class UpdateBookEntryCommandValidator : AbstractValidator<UpdateBookEntryCommand>
{
    public UpdateBookEntryCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        Include(new BookEntryInputValidator<UpdateBookEntryCommand>(dateTimeProvider));
    }
}

public class UpdateBookEntryCommandHandler : IRequestHandler<UpdateBookEntryCommand, BookEntryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateBookEntryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BookEntryDto> Handle(UpdateBookEntryCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        var entry = await _context.BookEntries
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BookEntry), request.Id);

        await BookEntryRules.EnsureReferencesExistAsync(_context, request, cancellationToken);

        // An entry keeps its category when the caller does not name one; changing the kind is
        // what needs a category of the matching kind.
        var input = request.CategoryId is null && request.Kind == entry.Kind
            ? request with { CategoryId = entry.CategoryId }
            : request;

        var category = await BookEntryRules.ResolveCategoryAsync(_context, input, cancellationToken);

        entry.Kind = request.Kind;
        entry.OccurredOn = request.OccurredOn;
        entry.Amount = request.Amount;
        entry.CategoryId = category.Id;
        entry.ProjectId = request.ProjectId;
        entry.EmployeeId = request.EmployeeId;
        entry.CustomerId = request.CustomerId;
        entry.BranchId = request.BranchId;
        entry.Counterparty = BookEntryRules.Clean(request.Counterparty);
        entry.Description = BookEntryRules.Clean(request.Description);
        entry.Method = request.Method;
        entry.DueDate = request.DueDate;
        entry.PaidOn = BookEntryRules.PaidOn(request);

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.BookEntries
            .AsNoTracking()
            .Where(e => e.Id == entry.Id)
            .Select(BookEntryMapping.Projection)
            .SingleAsync(cancellationToken);
    }
}

/// <summary>Removes a line of the book. It is kept in the database, only hidden.</summary>
public record DeleteBookEntryCommand(Guid Id) : IRequest;

public class DeleteBookEntryCommandHandler : IRequestHandler<DeleteBookEntryCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteBookEntryCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteBookEntryCommand request, CancellationToken cancellationToken)
    {
        await BookRules.EnsureWriteAsync(_context, _currentUserService, cancellationToken);

        var entry = await _context.BookEntries
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(BookEntry), request.Id);

        _context.BookEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
