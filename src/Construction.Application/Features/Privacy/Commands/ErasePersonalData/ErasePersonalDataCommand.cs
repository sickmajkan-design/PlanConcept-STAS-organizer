using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Privacy.Commands.ErasePersonalData;

/// <summary>What one erasure removed or redacted.</summary>
public record ErasureResult(
    Guid EmployeeId,
    int LocationRecords,
    int TimeEntryCoordinates,
    int AbsenceReasons,
    int Notifications,
    int DeviceTokens,
    int RefreshTokens,
    int PasswordResetTokens,
    int AttachmentsFlagged,
    bool AccountAnonymised);

/// <summary>
/// Erases an employee's personal data while keeping the employment record an
/// employer is obliged to hold.
/// </summary>
/// <remarks>
/// <para>
/// "Delete the person" is the wrong shape for a workforce system, and building
/// it that way would create a different problem: an employer must retain hours
/// worked, pay rates and the fact of employment for statutory periods that run
/// to years. A command that removed those would trade a privacy failure for a
/// bookkeeping one.
/// </para>
/// <para>
/// So this separates the two. What goes: everything that describes the person
/// or tracks them — contact details, date of birth, home address, the GPS
/// track, clock-in coordinates, absence reasons, device tokens, notifications
/// and sessions. What stays: the shift, its hours, its project, the rate that
/// applied, and the employee number they are recorded under. The result is a
/// timesheet that still adds up and no longer says who, where, or why they
/// were off sick.
/// </para>
/// <para>
/// <strong>Absence reasons are cleared, not kept.</strong> A free-text reason
/// on a sick-leave record is health data in all but name, which carries a
/// higher bar than the rest of this table. The dates and the leave type stay,
/// because payroll needs them; the sentence explaining the illness does not.
/// </para>
/// <para>
/// <strong>The audit trail is deliberately left alone,</strong> and that is the
/// one decision here that needs a lawyer rather than an engineer. Entries
/// record who changed what, including changes this employee made as a user, and
/// scrubbing them would destroy the trail's integrity for everyone else. The
/// position taken is that the trail is retained to demonstrate compliance —
/// which is itself a lawful basis — but a controller may disagree, and the
/// alternative is a code change rather than a setting. Documented in
/// docs/PRIVACY.md so it is a decision somebody made.
/// </para>
/// <para>
/// Irreversible, and it does not pretend otherwise: the rows are hard-deleted
/// rather than soft-deleted, because a soft-deleted GPS track is still a GPS
/// track.
/// </para>
/// </remarks>
public record ErasePersonalDataCommand : IRequest<ErasureResult>
{
    public Guid EmployeeId { get; init; }

    /// <summary>
    /// Why the erasure was carried out — a request from the person, the end of
    /// a retention period, a supervisory order.
    /// </summary>
    /// <remarks>
    /// Required, and recorded in the audit trail. An erasure with no stated
    /// reason is indistinguishable from someone quietly removing an
    /// inconvenient record, which is the thing an audit trail exists to tell
    /// apart.
    /// </remarks>
    public string Reason { get; init; } = null!;
}

public class ErasePersonalDataCommandValidator : AbstractValidator<ErasePersonalDataCommand>
{
    public ErasePersonalDataCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required; it is recorded in the audit trail.")
            .MinimumLength(10).WithMessage("Give a reason somebody reading the trail later could act on.")
            .MaximumLength(512);
    }
}

public class ErasePersonalDataCommandHandler
    : IRequestHandler<ErasePersonalDataCommand, ErasureResult>
{
    /// <summary>
    /// What the redacted name becomes.
    /// </summary>
    /// <remarks>
    /// A fixed marker rather than an empty string, so a screen showing a
    /// historical timesheet renders "Erased employee" instead of a blank where
    /// a name should be — which reads as a bug and gets investigated.
    /// </remarks>
    public const string RedactedName = "Erased";

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ErasePersonalDataCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ErasureResult> Handle(
        ErasePersonalDataCommand request,
        CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: an erasure request usually arrives after the
        // employee has left and been soft-deleted, which is exactly when the
        // ordinary filter would report them as not found.
        var employee = await _context.Employees
            .IgnoreQueryFilters()
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        var utcNow = _dateTimeProvider.UtcNow;

        // ---- what is removed outright ------------------------------------

        var locations = await _context.LocationRecords
            .IgnoreQueryFilters()
            .Where(l => l.EmployeeId == employee.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var coordinates = await _context.TimeEntries
            .IgnoreQueryFilters()
            .Where(t => t.EmployeeId == employee.Id
                && (t.StartLatitude != null || t.EndLatitude != null))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.StartLatitude, (double?)null)
                .SetProperty(t => t.StartLongitude, (double?)null)
                .SetProperty(t => t.EndLatitude, (double?)null)
                .SetProperty(t => t.EndLongitude, (double?)null), cancellationToken);

        // The dates and the leave type stay — payroll needs them. The sentence
        // explaining why somebody was off does not.
        var absenceReasons = await _context.Absences
            .IgnoreQueryFilters()
            .Where(a => a.EmployeeId == employee.Id
                && (a.Reason != null || a.ReviewNote != null))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.Reason, (string?)null)
                .SetProperty(a => a.ReviewNote, (string?)null), cancellationToken);

        var notifications = 0;
        var deviceTokens = 0;
        var refreshTokens = 0;
        var resetTokens = 0;
        var accountAnonymised = false;

        if (employee.User is { } user)
        {
            notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .ExecuteDeleteAsync(cancellationToken);

            deviceTokens = await _context.DeviceTokens
                .Where(d => d.UserId == user.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // These carry IP addresses as well as credentials.
            refreshTokens = await _context.RefreshTokens
                .Where(t => t.UserId == user.Id)
                .ExecuteDeleteAsync(cancellationToken);

            resetTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id)
                .ExecuteDeleteAsync(cancellationToken);

            // The row survives so that everything referencing "who did this"
            // still resolves, but it identifies nobody and cannot be signed
            // into. A unique, non-routable address keeps the email index happy
            // without colliding with a second erasure.
            user.Email = $"erased-{user.Id:N}@invalid";
            user.IsActive = false;
            user.EmployeeId = null;
            accountAnonymised = true;
        }

        // ---- what is redacted in place -----------------------------------

        // EmployeeNumber, EmploymentDate, Position and Status are left: they
        // are the employment record, not a description of the person.
        employee.FirstName = RedactedName;
        employee.LastName = employee.EmployeeNumber;
        employee.Phone = null;
        employee.Email = null;
        employee.Address = null;
        employee.DateOfBirth = null;

        if (!employee.IsDeleted)
        {
            employee.IsDeleted = true;
            employee.DeletedAt = utcNow;
        }

        // Attachments are counted, not deleted here: the bytes live in object
        // storage or on disk, and removing a database row would orphan them
        // rather than erase them. Reported so the caller knows there is a
        // second step, and named in docs/PRIVACY.md.
        var attachments = await _context.Attachments
            .IgnoreQueryFilters()
            .CountAsync(a => a.EmployeeId == employee.Id, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new ErasureResult(
            employee.Id,
            locations,
            coordinates,
            absenceReasons,
            notifications,
            deviceTokens,
            refreshTokens,
            resetTokens,
            attachments,
            accountAnonymised);
    }
}
