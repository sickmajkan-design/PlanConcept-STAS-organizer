using System.Text.Json;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Notifications.Broadcasts;

// ---- what the audience would be ------------------------------------------------------------------------------

public record AudiencePersonDto(Guid? EmployeeId, Guid? UserId, string Name, string Role, bool HasAccount, IReadOnlyList<string> Via);

public record AudiencePreviewDto
{
    public IReadOnlyList<AudiencePersonDto> People { get; init; } = [];

    public IReadOnlyList<AudienceChip> Chips { get; init; } = [];

    /// <summary>Everybody the choices name.</summary>
    public int Total { get; init; }

    /// <summary>Of them, those with an account, who can be sent something.</summary>
    public int Reachable { get; init; }

    public int WithoutAccount { get; init; }
}

/// <summary>Who a choice of targets would reach, before anything is sent.</summary>
public record GetAudiencePreviewQuery(IReadOnlyList<AudienceTarget> Targets) : IRequest<AudiencePreviewDto>;

public class GetAudiencePreviewQueryHandler : IRequestHandler<GetAudiencePreviewQuery, AudiencePreviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAudiencePreviewQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AudiencePreviewDto> Handle(GetAudiencePreviewQuery request, CancellationToken cancellationToken)
    {
        var audience = await AudienceResolver.ResolveAsync(_context, _dateTimeProvider.Today, request.Targets, cancellationToken);

        return new AudiencePreviewDto
        {
            People = audience.People
                .Select(p => new AudiencePersonDto(p.EmployeeId, p.UserId, p.Name, p.Role, p.HasAccount, p.Via))
                .ToList(),
            Chips = audience.Chips,
            Total = audience.People.Count,
            Reachable = audience.Reachable.Count,
            WithoutAccount = audience.WithoutAccount,
        };
    }
}

// ---- sending ----------------------------------------------------------------------------------------------------

public record SendBroadcastResult(Guid BroadcastId, int Recipients, int WithoutAccount);

/// <summary>Sends an announcement to everybody the choices name, and keeps a record of it.</summary>
public record SendBroadcastCommand : IRequest<SendBroadcastResult>
{
    public string Title { get; init; } = null!;

    public string Body { get; init; } = null!;

    public IReadOnlyList<AudienceTarget> Targets { get; init; } = [];

    /// <summary>When set, each recipient has to confirm they read it.</summary>
    public bool RequiresAcknowledgment { get; init; }
}

public class SendBroadcastCommandValidator : AbstractValidator<SendBroadcastCommand>
{
    public const int MaxTargets = 40;

    public SendBroadcastCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(256);
        RuleFor(x => x.Body).NotEmpty().WithMessage("Body is required.").MaximumLength(4000);

        RuleFor(x => x.Targets)
            .NotEmpty().WithMessage("Choose who the announcement is for.")
            .Must(t => t.Count <= MaxTargets).WithMessage($"At most {MaxTargets} choices.");

        RuleForEach(x => x.Targets).ChildRules(target =>
        {
            target.RuleFor(t => t.Kind).IsInEnum();
            target.RuleFor(t => t.Id).NotEmpty()
                .When(t => t.Kind is AudienceKind.Employee or AudienceKind.Group or AudienceKind.Project or AudienceKind.Branch);
            target.RuleFor(t => t.Role).NotNull().IsInEnum()
                .When(t => t.Kind == AudienceKind.Role);
        });
    }
}

public class SendBroadcastCommandHandler : IRequestHandler<SendBroadcastCommand, SendBroadcastResult>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendBroadcastCommandHandler(
        IApplicationDbContext context,
        INotificationService notifications,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _notifications = notifications;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<SendBroadcastResult> Handle(SendBroadcastCommand request, CancellationToken cancellationToken)
    {
        var audience = await AudienceResolver.ResolveAsync(_context, _dateTimeProvider.Today, request.Targets, cancellationToken);
        var reachable = audience.Reachable;

        if (reachable.Count == 0)
        {
            throw new ConflictException("Nobody in this audience has an account, so nobody can receive the announcement.");
        }

        var broadcast = new NotificationBroadcast
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            RequiresAcknowledgment = request.RequiresAcknowledgment,
            SentByUserId = _currentUser.UserId,
            AudienceJson = JsonSerializer.Serialize(audience.Chips),
            RecipientCount = reachable.Count,
            WithoutAccountCount = audience.WithoutAccount,
        };

        _context.NotificationBroadcasts.Add(broadcast);
        await _context.SaveChangesAsync(cancellationToken);

        var sent = await _notifications.NotifyUsersAsync(
            reachable.Select(p => p.UserId!.Value).ToList(),
            NotificationType.GeneralAnnouncement,
            broadcast.Title,
            broadcast.Body,
            data: null,
            broadcast.RequiresAcknowledgment,
            cancellationToken,
            broadcast.Id);

        // Through which choice each person was reached, so the sender can see it beside their name.
        foreach (var group in reachable.GroupBy(p => string.Join("; ", p.Via)))
        {
            var ids = group.Select(p => p.UserId!.Value).ToList();
            var via = group.Key.Length > 300 ? group.Key[..300] : group.Key;

            await _context.Notifications
                .Where(n => n.BroadcastId == broadcast.Id && ids.Contains(n.UserId))
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.Via, via), cancellationToken);
        }

        return new SendBroadcastResult(broadcast.Id, sent, audience.WithoutAccount);
    }
}

// ---- the history -------------------------------------------------------------------------------------------------

public record BroadcastListItemDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = null!;

    public string Preview { get; init; } = null!;

    public IReadOnlyList<AudienceChip> Audience { get; init; } = [];

    public DateTime SentAt { get; init; }

    public string? SentBy { get; init; }

    public bool RequiresAcknowledgment { get; init; }

    public int Recipients { get; init; }

    public int Read { get; init; }

    public int Acknowledged { get; init; }
}

public record GetBroadcastsQuery : IRequest<PagedList<BroadcastListItemDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    /// <summary>Only announcements that ask for a confirmation.</summary>
    public bool? RequiresAcknowledgment { get; init; }

    /// <summary>Only announcements that not everybody has read yet.</summary>
    public bool? NotAllRead { get; init; }
}

public class GetBroadcastsQueryHandler : IRequestHandler<GetBroadcastsQuery, PagedList<BroadcastListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBroadcastsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<BroadcastListItemDto>> Handle(GetBroadcastsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.NotificationBroadcasts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(b => EF.Functions.Like(b.Title.ToLower(), $"%{term.ToLower()}%")
                || EF.Functions.Like(b.AudienceJson.ToLower(), $"%{term.ToLower()}%"));
        }

        if (request.RequiresAcknowledgment == true)
        {
            query = query.Where(b => b.RequiresAcknowledgment);
        }

        if (request.NotAllRead == true)
        {
            query = query.Where(b => _context.Notifications.Count(n => n.BroadcastId == b.Id && !n.IsReminder && !n.IsRead) > 0);
        }

        var page = await PagedList<BroadcastListItemDto>.CreateAsync(
            query
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BroadcastListItemDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Preview = b.Body.Length > 140 ? b.Body.Substring(0, 140) : b.Body,
                    SentAt = b.CreatedAt,
                    SentBy = b.SentByUser != null ? b.SentByUser.Email : null,
                    RequiresAcknowledgment = b.RequiresAcknowledgment,
                    Recipients = b.RecipientCount,
                    Read = _context.Notifications.Count(n => n.BroadcastId == b.Id && !n.IsReminder && n.IsRead),
                    Acknowledged = _context.Notifications.Count(n => n.BroadcastId == b.Id && !n.IsReminder && n.AcknowledgedAt != null),
                }),
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        // The audience is stored as JSON; it is read here, for the page only.
        var audiences = await _context.NotificationBroadcasts
            .AsNoTracking()
            .Where(b => page.Items.Select(i => i.Id).Contains(b.Id))
            .Select(b => new { b.Id, b.AudienceJson })
            .ToDictionaryAsync(b => b.Id, b => b.AudienceJson, cancellationToken);

        var items = page.Items
            .Select(i => i with { Audience = ReadAudience(audiences.GetValueOrDefault(i.Id)) })
            .ToList();

        return new PagedList<BroadcastListItemDto>(items, page.TotalCount, page.PageNumber, page.PageSize);
    }

    internal static IReadOnlyList<AudienceChip> ReadAudience(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<AudienceChip>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

public record BroadcastRecipientDto
{
    public Guid UserId { get; init; }

    public string Name { get; init; } = null!;

    public string Role { get; init; } = null!;

    /// <summary>Through which choice of the audience the person was reached.</summary>
    public string? Via { get; init; }

    /// <summary>Whether a phone is registered for push; without one the notice is only in the bell.</summary>
    public bool HasDevice { get; init; }

    public bool IsRead { get; init; }

    public DateTime? ReadAt { get; init; }

    public DateTime? AcknowledgedAt { get; init; }

    /// <summary>How many reminders the person has had.</summary>
    public int Reminders { get; init; }
}

public record BroadcastDetailDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = null!;

    public string Body { get; init; } = null!;

    public IReadOnlyList<AudienceChip> Audience { get; init; } = [];

    public DateTime SentAt { get; init; }

    public string? SentBy { get; init; }

    public bool RequiresAcknowledgment { get; init; }

    public int WithoutAccount { get; init; }

    public DateTime? LastReminderAt { get; init; }

    public IReadOnlyList<BroadcastRecipientDto> Recipients { get; init; } = [];
}

public record GetBroadcastQuery(Guid Id) : IRequest<BroadcastDetailDto>;

public class GetBroadcastQueryHandler : IRequestHandler<GetBroadcastQuery, BroadcastDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetBroadcastQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BroadcastDetailDto> Handle(GetBroadcastQuery request, CancellationToken cancellationToken)
    {
        var broadcast = await _context.NotificationBroadcasts
            .AsNoTracking()
            .Where(b => b.Id == request.Id)
            .Select(b => new
            {
                b.Id,
                b.Title,
                b.Body,
                b.AudienceJson,
                b.CreatedAt,
                SentBy = b.SentByUser != null ? b.SentByUser.Email : null,
                b.RequiresAcknowledgment,
                b.WithoutAccountCount,
                b.LastReminderAt,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(NotificationBroadcast), request.Id);

        var rows = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.BroadcastId == request.Id)
            .Select(n => new
            {
                n.UserId,
                n.IsReminder,
                n.IsRead,
                n.ReadAt,
                n.AcknowledgedAt,
                n.Via,
                Name = n.User.Employee != null ? n.User.Employee.FirstName + " " + n.User.Employee.LastName : n.User.Email,
                n.User.Role,
            })
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(r => r.UserId).Distinct().ToList();

        var withDevice = (await _context.DeviceTokens
                .AsNoTracking()
                .Where(d => userIds.Contains(d.UserId))
                .Select(d => d.UserId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var recipients = rows
            .Where(r => !r.IsReminder)
            .Select(r => new BroadcastRecipientDto
            {
                UserId = r.UserId,
                Name = r.Name,
                Role = AudienceResolver.RoleLabel(r.Role),
                Via = r.Via,
                HasDevice = withDevice.Contains(r.UserId),
                IsRead = r.IsRead,
                ReadAt = r.ReadAt,
                AcknowledgedAt = r.AcknowledgedAt,
                Reminders = rows.Count(x => x.IsReminder && x.UserId == r.UserId),
            })
            .OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new BroadcastDetailDto
        {
            Id = broadcast.Id,
            Title = broadcast.Title,
            Body = broadcast.Body,
            Audience = GetBroadcastsQueryHandler.ReadAudience(broadcast.AudienceJson),
            SentAt = broadcast.CreatedAt,
            SentBy = broadcast.SentBy,
            RequiresAcknowledgment = broadcast.RequiresAcknowledgment,
            WithoutAccount = broadcast.WithoutAccountCount,
            LastReminderAt = broadcast.LastReminderAt,
            Recipients = recipients,
        };
    }
}

// ---- reminder ---------------------------------------------------------------------------------------------------

/// <summary>
/// Sends the announcement again to those who have not read it (and, when it asks for a confirmation, to those who
/// read it and have not confirmed). Nobody gets a reminder more than once a day.
/// </summary>
public record RemindBroadcastCommand(Guid Id) : IRequest<int>;

public class RemindBroadcastCommandHandler : IRequestHandler<RemindBroadcastCommand, int>
{
    private static readonly TimeSpan MinGap = TimeSpan.FromHours(24);

    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemindBroadcastCommandHandler(
        IApplicationDbContext context,
        INotificationService notifications,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _notifications = notifications;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(RemindBroadcastCommand request, CancellationToken cancellationToken)
    {
        var broadcast = await _context.NotificationBroadcasts
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(NotificationBroadcast), request.Id);

        var now = _dateTimeProvider.UtcNow;
        var recently = now - MinGap;

        var remindedLately = _context.Notifications
            .Where(n => n.BroadcastId == broadcast.Id && n.IsReminder && n.CreatedAt >= recently)
            .Select(n => n.UserId);

        var userIds = await _context.Notifications
            .Where(n => n.BroadcastId == broadcast.Id
                && !n.IsReminder
                && (!n.IsRead || (n.RequiresAcknowledgment && n.AcknowledgedAt == null))
                && !remindedLately.Contains(n.UserId))
            .Select(n => n.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return 0;
        }

        var sent = await _notifications.NotifyUsersAsync(
            userIds,
            NotificationType.GeneralAnnouncement,
            $"Podsjetnik: {broadcast.Title}",
            broadcast.Body,
            data: null,
            broadcast.RequiresAcknowledgment,
            cancellationToken,
            broadcast.Id,
            isReminder: true);

        broadcast.LastReminderAt = now;
        await _context.SaveChangesAsync(cancellationToken);

        return sent;
    }
}
