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
    /// the card). Required on every vehicle so a statement row can be checked against the
    /// vehicle its card is issued to. Nullable in the database only because vehicles created
    /// before the field existed have none yet; the API refuses to save one without it.
    /// </summary>
    public string? TdNumber { get; set; }

    public string? Vin { get; set; }

    /// <summary>Value encoded in the QR label attached to the physical vehicle.</summary>
    public string? QrCode { get; set; }

    /// <summary>Name of whatever GPS tracking platform this vehicle's tracker reports to. Free text — a leased vehicle often already comes with its lessor's own platform, not one the company chose.</summary>
    public string? GpsProvider { get; set; }

    /// <summary>Deep link to this vehicle on its GPS provider's own site. Opened in a new tab; no position data is fetched or stored here.</summary>
    public string? GpsTrackingUrl { get; set; }

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
