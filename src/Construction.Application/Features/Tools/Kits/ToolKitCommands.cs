using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Kits;

// ---- what the case is made of ------------------------------------------------------------------------------------

public record GetToolKitQuery(Guid ToolId) : IRequest<ToolKitDto>;

public class GetToolKitQueryHandler(IApplicationDbContext context) : IRequestHandler<GetToolKitQuery, ToolKitDto>
{
    public Task<ToolKitDto> Handle(GetToolKitQuery request, CancellationToken cancellationToken) =>
        ToolKitReader.LoadAsync(context, request.ToolId, cancellationToken);
}

public abstract record ToolKitItemFields
{
    public string Name { get; init; } = null!;

    public int Quantity { get; init; } = 1;

    public decimal UnitPrice { get; init; }
}

public abstract class ToolKitItemFieldsValidator<T> : AbstractValidator<T>
    where T : ToolKitItemFields
{
    protected ToolKitItemFieldsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1000);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1_000_000);
    }
}

public record AddToolKitItemCommand : ToolKitItemFields, IRequest<ToolKitDto>
{
    public Guid ToolId { get; init; }
}

public class AddToolKitItemCommandValidator : ToolKitItemFieldsValidator<AddToolKitItemCommand>;

public class AddToolKitItemCommandHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<AddToolKitItemCommand, ToolKitDto>
{
    public async Task<ToolKitDto> Handle(AddToolKitItemCommand request, CancellationToken cancellationToken)
    {
        await ToolKitReader.LoadKitAsync(context, request.ToolId, cancellationToken);

        var next = (await context.ToolKitItems
            .Where(i => i.ToolId == request.ToolId)
            .MaxAsync(i => (int?)i.SortOrder, cancellationToken) ?? 0) + 1;

        var item = new ToolKitItem
        {
            ToolId = request.ToolId,
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            SortOrder = next,
        };

        context.ToolKitItems.Add(item);
        context.ToolKitEvents.Add(Event(request.ToolId, item, ToolKitEventType.ItemAdded, user, amount: item.Quantity * item.UnitPrice));
        await context.SaveChangesAsync(cancellationToken);

        return await ToolKitReader.LoadAsync(context, request.ToolId, cancellationToken);
    }

    internal static ToolKitEvent Event(
        Guid toolId,
        ToolKitItem item,
        ToolKitEventType type,
        ICurrentUserService user,
        decimal? amount = null,
        Guid? employeeId = null,
        string? note = null) =>
        new()
        {
            ToolId = toolId,
            ItemId = item.Id,
            ItemName = item.Name,
            Type = type,
            Amount = amount,
            EmployeeId = employeeId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            RecordedByUserId = user.UserId,
        };
}

public record UpdateToolKitItemCommand : ToolKitItemFields, IRequest<ToolKitDto>
{
    public Guid ToolId { get; init; }

    public Guid ItemId { get; init; }
}

public class UpdateToolKitItemCommandValidator : ToolKitItemFieldsValidator<UpdateToolKitItemCommand>;

public class UpdateToolKitItemCommandHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<UpdateToolKitItemCommand, ToolKitDto>
{
    public async Task<ToolKitDto> Handle(UpdateToolKitItemCommand request, CancellationToken cancellationToken)
    {
        await ToolKitReader.LoadKitAsync(context, request.ToolId, cancellationToken);
        var item = await FindItemAsync(context, request.ToolId, request.ItemId, cancellationToken);

        var before = $"{item.Name} × {item.Quantity} @ {item.UnitPrice:0.00}";
        item.Name = request.Name.Trim();
        item.Quantity = request.Quantity;
        item.UnitPrice = request.UnitPrice;
        var after = $"{item.Name} × {item.Quantity} @ {item.UnitPrice:0.00}";

        if (before != after)
        {
            context.ToolKitEvents.Add(AddToolKitItemCommandHandler.Event(
                request.ToolId, item, ToolKitEventType.ItemChanged, user, note: $"{before} → {after}"));
        }

        await context.SaveChangesAsync(cancellationToken);

        return await ToolKitReader.LoadAsync(context, request.ToolId, cancellationToken);
    }

    internal static async Task<ToolKitItem> FindItemAsync(
        IApplicationDbContext context, Guid toolId, Guid itemId, CancellationToken cancellationToken) =>
        await context.ToolKitItems.FirstOrDefaultAsync(i => i.Id == itemId && i.ToolId == toolId, cancellationToken)
            ?? throw new NotFoundException(nameof(ToolKitItem), itemId);
}

public record RemoveToolKitItemCommand(Guid ToolId, Guid ItemId) : IRequest<ToolKitDto>;

public class RemoveToolKitItemCommandHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<RemoveToolKitItemCommand, ToolKitDto>
{
    public async Task<ToolKitDto> Handle(RemoveToolKitItemCommand request, CancellationToken cancellationToken)
    {
        await ToolKitReader.LoadKitAsync(context, request.ToolId, cancellationToken);
        var item = await UpdateToolKitItemCommandHandler.FindItemAsync(context, request.ToolId, request.ItemId, cancellationToken);

        context.ToolKitEvents.Add(AddToolKitItemCommandHandler.Event(request.ToolId, item, ToolKitEventType.ItemRemoved, user));
        context.ToolKitItems.Remove(item);
        await context.SaveChangesAsync(cancellationToken);

        return await ToolKitReader.LoadAsync(context, request.ToolId, cancellationToken);
    }
}

// ---- what happens to an item -------------------------------------------------------------------------------------

public record ActOnToolKitItemCommand : IRequest<ToolKitDto>
{
    public Guid ToolId { get; init; }

    public Guid ItemId { get; init; }

    public ToolKitAction Action { get; init; }

    /// <summary>For a charge or a refund: the sum. Left out, it is the worth of the item.</summary>
    public decimal? Amount { get; init; }

    /// <summary>For a charge or a refund: the worker. Left out, it is whoever holds the case.</summary>
    public Guid? EmployeeId { get; init; }

    public string? Note { get; init; }

    /// <summary>
    /// For a replacement the worker bought: false when the damage was theirs and they are not refunded.
    /// </summary>
    public bool Refund { get; init; } = true;

    /// <summary>Required when a refund is owed: with the pay, or in cash.</summary>
    public ToolKitPayoutMethod? PayoutMethod { get; init; }
}

public class ActOnToolKitItemCommandValidator : AbstractValidator<ActOnToolKitItemCommand>
{
    public ActOnToolKitItemCommandValidator()
    {
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.Note).MaximumLength(500);
        RuleFor(x => x.PayoutMethod).IsInEnum();
        RuleFor(x => x.PayoutMethod)
            .NotNull().WithMessage("Choose how the refund is paid: with the pay, or in cash.")
            .When(x => x.Action == ToolKitAction.ReplacementBought && x.Refund);
    }
}

public class ActOnToolKitItemCommandHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<ActOnToolKitItemCommand, ToolKitDto>
{
    private static readonly Dictionary<ToolKitAction, (ToolKitItemState[] From, ToolKitItemState To, ToolKitEventType Event)> Rules = new()
    {
        [ToolKitAction.ReportMissing] = ([ToolKitItemState.InKit], ToolKitItemState.Missing, ToolKitEventType.ReportedMissing),
        [ToolKitAction.ReportBroken] = ([ToolKitItemState.InKit], ToolKitItemState.Broken, ToolKitEventType.ReportedBroken),
        // A broken item has to reach the office before anything else is decided about it.
        [ToolKitAction.ReceiveBroken] = ([ToolKitItemState.Broken], ToolKitItemState.BrokenHandedIn, ToolKitEventType.BrokenReceived),
        [ToolKitAction.ChargeWorker] = ([ToolKitItemState.Missing], ToolKitItemState.Charged, ToolKitEventType.ChargedToWorker),
        [ToolKitAction.WriteOff] = ([ToolKitItemState.Missing, ToolKitItemState.BrokenHandedIn], ToolKitItemState.WrittenOff, ToolKitEventType.WrittenOff),
        [ToolKitAction.Restore] = (
            [ToolKitItemState.Missing, ToolKitItemState.BrokenHandedIn, ToolKitItemState.Charged, ToolKitItemState.WrittenOff],
            ToolKitItemState.InKit,
            ToolKitEventType.Restored),
        [ToolKitAction.ReplacementBought] = (
            [ToolKitItemState.Missing, ToolKitItemState.BrokenHandedIn],
            ToolKitItemState.InKit,
            ToolKitEventType.ReplacementBought),
    };

    public async Task<ToolKitDto> Handle(ActOnToolKitItemCommand request, CancellationToken cancellationToken)
    {
        var tool = await ToolKitReader.LoadKitAsync(context, request.ToolId, cancellationToken);
        var item = await UpdateToolKitItemCommandHandler.FindItemAsync(context, request.ToolId, request.ItemId, cancellationToken);
        var (from, to, eventType) = Rules[request.Action];

        if (!from.Contains(item.State))
        {
            throw new ConflictException($"'{request.Action}' is not possible while the item is '{item.State}'.");
        }

        decimal? amount = null;
        Guid? employeeId = null;
        ToolKitPayoutMethod? payoutMethod = null;
        var noRefund = request.Action == ToolKitAction.ReplacementBought && !request.Refund;

        if (request.Action is ToolKitAction.ChargeWorker or ToolKitAction.ReplacementBought)
        {
            employeeId = request.EmployeeId ?? tool.AssignedEmployeeId
                ?? throw new ConflictException("Name the worker: the case is not held by anyone.");

            if (!await context.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken))
            {
                throw new NotFoundException(nameof(Employee), employeeId);
            }

            // Without a refund no money is owed, so there is no amount to settle.
            amount = noRefund ? null : request.Amount ?? item.Quantity * item.UnitPrice;
            payoutMethod = noRefund || request.Action != ToolKitAction.ReplacementBought ? null : request.PayoutMethod;
        }

        item.State = to;
        var line = AddToolKitItemCommandHandler.Event(
            request.ToolId, item, noRefund ? ToolKitEventType.ReplacementNotRefunded : eventType, user, amount, employeeId, request.Note);
        line.PayoutMethod = payoutMethod;
        context.ToolKitEvents.Add(line);
        await context.SaveChangesAsync(cancellationToken);

        return await ToolKitReader.LoadAsync(context, request.ToolId, cancellationToken);
    }
}

/// <summary>Marks a charge as collected or a refund as paid out.</summary>
public record SettleToolKitEventCommand(Guid ToolId, Guid EventId) : IRequest<ToolKitDto>;

public class SettleToolKitEventCommandHandler(IApplicationDbContext context, ICurrentUserService user)
    : IRequestHandler<SettleToolKitEventCommand, ToolKitDto>
{
    public async Task<ToolKitDto> Handle(SettleToolKitEventCommand request, CancellationToken cancellationToken)
    {
        await ToolKitReader.LoadKitAsync(context, request.ToolId, cancellationToken);

        var line = await context.ToolKitEvents
            .FirstOrDefaultAsync(e => e.Id == request.EventId && e.ToolId == request.ToolId, cancellationToken)
            ?? throw new NotFoundException(nameof(ToolKitEvent), request.EventId);

        if (line.Type is not (ToolKitEventType.ChargedToWorker or ToolKitEventType.ReplacementBought))
        {
            throw new ConflictException("Only a charge or a refund can be settled.");
        }

        if (line.SettledAt is null)
        {
            line.SettledAt = DateTime.UtcNow;
            context.ToolKitEvents.Add(new ToolKitEvent
            {
                ToolId = request.ToolId,
                ItemId = line.ItemId,
                ItemName = line.ItemName,
                Type = ToolKitEventType.Settled,
                Amount = line.Amount,
                EmployeeId = line.EmployeeId,
                Note = line.Type == ToolKitEventType.ChargedToWorker ? "charge collected" : "refund paid",
                RecordedByUserId = user.UserId,
            });
            await context.SaveChangesAsync(cancellationToken);
        }

        return await ToolKitReader.LoadAsync(context, request.ToolId, cancellationToken);
    }
}
