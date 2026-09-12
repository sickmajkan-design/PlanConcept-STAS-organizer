using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// A file attached to exactly one record: an employee's certificate, a
/// project's drawing, a vehicle's insurance, a tool's calibration sheet.
/// </summary>
/// <remarks>
/// The owner is a set of nullable foreign keys with a check constraint
/// allowing exactly one, rather than the usual pair of `OwnerType` + `OwnerId`.
/// The discriminator pair is shorter to write and gives up everything the
/// database is for: no foreign key, so an attachment can outlive its owner and
/// point at nothing; no cascade, so deleting an employee leaves their medical
/// records behind. Here, removing an employee removes their documents with
/// them, which is also what a data-erasure request needs.
///
/// A new owner type means a new column and an updated constraint. That is a
/// migration, which is the point — adding one should be a decision, not a
/// value someone passes in. <see cref="WorkItem"/> was the fifth, so a defect
/// photograph disappears with the defect rather than outliving it;
/// <see cref="VehicleExpense"/>, <see cref="MaterialMovement"/>,
/// <see cref="EmployeeRate"/> and <see cref="FinanceEntry"/> were the sixth
/// through ninth, <see cref="ToolExpense"/> the tenth, so a receipt photo
/// disappears with the cost record it documents, and
/// <see cref="VehicleRentalRate"/> the eleventh, so a lease contract
/// disappears with the rate row it was signed for, and
/// <see cref="GeneralExpense"/>, <see cref="Accommodation"/> and
/// <see cref="AccommodationRate"/> the twelfth through fourteenth, and
/// <see cref="ToolRentalRate"/> the fifteenth, so a lease contract for a
/// rented tool disappears with the rate row the same way a vehicle's does.
/// </remarks>
public class Attachment : BaseEntity, ISoftDeletable, IAuditable
{
    /// <summary>Name as uploaded, shown to people. Never used as a path.</summary>
    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    /// <summary>
    /// Where <see cref="Construction.Application.Common.Interfaces.IFileStorage"/>
    /// put the bytes. Generated, never taken from the request.
    /// </summary>
    public string StorageKey { get; set; } = null!;

    public AttachmentCategory Category { get; set; } = AttachmentCategory.Other;

    public string? Description { get; set; }

    /// <summary>
    /// When the document stops being valid. Null for anything that does not
    /// lapse, such as a photograph.
    /// </summary>
    public DateOnly? ExpiresAt { get; set; }

    /// <summary>
    /// The earliest date this document may be deleted — a legal retention
    /// requirement (an invoice, a contract) rather than an everyday setting.
    /// Null means the ordinary rule applies: anyone allowed to delete may,
    /// whenever they like.
    /// </summary>
    public DateOnly? RetainUntil { get; set; }

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }

    public Guid? ToolId { get; set; }

    public Tool? Tool { get; set; }

    public Guid? WorkItemId { get; set; }

    public WorkItem? WorkItem { get; set; }

    public Guid? VehicleExpenseId { get; set; }

    public VehicleExpense? VehicleExpense { get; set; }

    public Guid? MaterialMovementId { get; set; }

    public MaterialMovement? MaterialMovement { get; set; }

    public Guid? EmployeeRateId { get; set; }

    public EmployeeRate? EmployeeRate { get; set; }

    public Guid? FinanceEntryId { get; set; }

    public FinanceEntry? FinanceEntry { get; set; }

    public Guid? ToolExpenseId { get; set; }

    public ToolExpense? ToolExpense { get; set; }

    public Guid? VehicleRentalRateId { get; set; }

    public VehicleRentalRate? VehicleRentalRate { get; set; }

    public Guid? GeneralExpenseId { get; set; }

    public GeneralExpense? GeneralExpense { get; set; }

    public Guid? AccommodationId { get; set; }

    public Accommodation? Accommodation { get; set; }

    public Guid? AccommodationRateId { get; set; }

    public AccommodationRate? AccommodationRate { get; set; }

    public Guid? ToolRentalRateId { get; set; }

    public ToolRentalRate? ToolRentalRate { get; set; }

    public Guid? UploadedByUserId { get; set; }

    public User? UploadedByUser { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    /// <summary>True once the document's validity has run out.</summary>
    public bool IsExpiredOn(DateOnly today) =>
        ExpiresAt is { } expiry && expiry < today;

    /// <summary>True while a retention requirement still forbids deleting this.</summary>
    public bool IsRetainedOn(DateOnly today) =>
        RetainUntil is { } until && until >= today;
}
