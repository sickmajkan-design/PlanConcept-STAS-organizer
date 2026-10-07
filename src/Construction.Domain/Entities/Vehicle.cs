using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

public class Vehicle : BaseEntity, ISoftDeletable, IAuditable
{
    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string RegistrationNumber { get; set; } = null!;

    /// <summary>
    /// The company's own number for the vehicle (the "TD" printed on a DKV statement next to
    /// the card). Optional: the office fills it in, since a driver adding a vehicle on the phone
    /// cannot know it. Statement rows are paired through the fuel card number, not this; it only
    /// backs the extra check of the label printed on the statement.
    /// </summary>
    public string? TdNumber { get; set; }

    public string? Vin { get; set; }

    /// <summary>Value encoded in the QR label attached to the physical vehicle.</summary>
    public string? QrCode { get; set; }

    /// <summary>Name of whatever GPS tracking platform this vehicle's tracker reports to. Free text — a leased vehicle often already comes with its lessor's own platform, not one the company chose.</summary>
    public string? GpsProvider { get; set; }

    /// <summary>Deep link to this vehicle on its GPS provider's own site. Opened in a new tab; no position data is fetched or stored here.</summary>
    public string? GpsTrackingUrl { get; set; }

    /// <summary>
    /// The last day the vehicle's registration is valid. Null when nobody has entered it yet. Shown in the
    /// vehicle's header, so the date to renew by is seen without opening a document.
    /// </summary>
    public DateOnly? RegistrationValidUntil { get; set; }

    /// <summary>
    /// The last day of the rental or lease, for a vehicle the company takes from somebody else. Null when no end
    /// is agreed or the vehicle is owned. Separate from <see cref="VehicleRentalRate"/>, whose end date means the
    /// rate stopped applying, not that the contract runs out.
    /// </summary>
    public DateOnly? RentedUntil { get; set; }

    /// <summary>The last day the technical inspection is valid. Null when nobody has entered it.</summary>
    public DateOnly? TechnicalInspectionValidUntil { get; set; }

    /// <summary>The last day the insurance is valid. Null when nobody has entered it.</summary>
    public DateOnly? InsuranceValidUntil { get; set; }

    /// <summary>When the next service is due. Null when nobody has entered it.</summary>
    public DateOnly? NextServiceDue { get; set; }

    public FuelType FuelType { get; set; }

    public VehicleStatus Status { get; set; } = VehicleStatus.Available;

    /// <summary>Owned outright, rented, or leased — orthogonal to <see cref="Status"/>.</summary>
    public VehicleOwnershipType OwnershipType { get; set; } = VehicleOwnershipType.Owned;

    public Guid? AssignedEmployeeId { get; set; }

    public Employee? AssignedEmployee { get; set; }

    /// <summary>
    /// Independent of <see cref="AssignedEmployeeId"/> — a vehicle can sit on a
    /// project and be held by an employee at the same time. Kept in sync with
    /// wherever the assigned employee is currently posted; see
    /// <c>AssignEmployeeToProjectCommand</c> and
    /// <c>RemoveEmployeeFromProjectCommand</c>.
    /// </summary>
    public Guid? AssignedProjectId { get; set; }

    public Project? AssignedProject { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public ICollection<VehicleExpense> Expenses { get; set; } = new List<VehicleExpense>();

    public ICollection<FuelCard> FuelCards { get; set; } = new List<FuelCard>();

    public ICollection<VehicleRentalRate> RentalRates { get; set; } = new List<VehicleRentalRate>();

    /// <summary>Every time this vehicle went out to another company. See <see cref="VehicleRentalOut"/>.</summary>
    public ICollection<VehicleRentalOut> RentalsOut { get; set; } = new List<VehicleRentalOut>();

    /// <summary>Vignettes, tunnel tolls and road-passage charges this vehicle carries. See <see cref="VehicleToll"/>.</summary>
    public ICollection<VehicleToll> Tolls { get; set; } = new List<VehicleToll>();

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    /// <summary>The business unit this vehicle belongs to in its own right. Null means it follows the project it is assigned to.</summary>
    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }
}
