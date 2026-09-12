using Construction.Application.Common.Interfaces;
using Construction.Application.Features.ScheduledReports.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Construction.Application.Features.ScheduledReports.Commands.CreateScheduledReportSubscription;

public record CreateScheduledReportSubscriptionCommand : IRequest<ScheduledReportSubscriptionDto>
{
    /// <summary>Defaults to the creator's own address when left blank.</summary>
    public string? RecipientEmail { get; init; }

    public ScheduledReportType ReportType { get; init; }

    public ScheduledReportCadence Cadence { get; init; }

    public DayOfWeek? DayOfWeek { get; init; }

    public int? DayOfMonth { get; init; }

    public string? Language { get; init; }
}

public class CreateScheduledReportSubscriptionCommandValidator
    : AbstractValidator<CreateScheduledReportSubscriptionCommand>
{
    public CreateScheduledReportSubscriptionCommandValidator()
    {
        RuleFor(x => x.RecipientEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.RecipientEmail));

        RuleFor(x => x.ReportType).IsInEnum();

        RuleFor(x => x.Cadence).IsInEnum();

        RuleFor(x => x.DayOfWeek)
            .NotNull().WithMessage("A weekday is required for a weekly report.")
            .When(x => x.Cadence == ScheduledReportCadence.Weekly);

        RuleFor(x => x.DayOfMonth)
            .InclusiveBetween(1, 28)
            .WithMessage("The day of the month must be between 1 and 28.")
            .When(x => x.Cadence == ScheduledReportCadence.Monthly);

        RuleFor(x => x.Language)
            .Must(l => l is "sr" or "en")
            .WithMessage("Language must be 'sr' or 'en'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Language));
    }
}

public class CreateScheduledReportSubscriptionCommandHandler
    : IRequestHandler<CreateScheduledReportSubscriptionCommand, ScheduledReportSubscriptionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateScheduledReportSubscriptionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ScheduledReportSubscriptionDto> Handle(
        CreateScheduledReportSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var utcNow = _dateTimeProvider.UtcNow;

        var subscription = new ScheduledReportSubscription
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = userId,
            RecipientEmail = string.IsNullOrWhiteSpace(request.RecipientEmail)
                ? _currentUserService.Email!
                : request.RecipientEmail.Trim(),
            ReportType = request.ReportType,
            Cadence = request.Cadence,
            DayOfWeek = request.Cadence == ScheduledReportCadence.Weekly ? request.DayOfWeek : null,
            DayOfMonth = request.Cadence == ScheduledReportCadence.Monthly ? request.DayOfMonth : null,
            Language = string.IsNullOrWhiteSpace(request.Language) ? "sr" : request.Language,
            NextRunAtUtc = ScheduledReportScheduling.ComputeNextRun(
                request.Cadence, request.DayOfWeek, request.DayOfMonth, utcNow),
        };

        _context.ScheduledReportSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        return new ScheduledReportSubscriptionDto
        {
            Id = subscription.Id,
            RecipientEmail = subscription.RecipientEmail,
            ReportType = subscription.ReportType,
            Cadence = subscription.Cadence,
            DayOfWeek = subscription.DayOfWeek,
            DayOfMonth = subscription.DayOfMonth,
            Language = subscription.Language,
            NextRunAtUtc = subscription.NextRunAtUtc,
            CreatedByEmail = _currentUserService.Email!,
        };
    }
}
