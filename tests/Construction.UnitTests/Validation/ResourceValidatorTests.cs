using Construction.Application.Features.Materials.Commands.AdjustMaterialQuantity;
using Construction.Application.Features.Materials.Commands.CreateMaterial;
using Construction.Application.Features.Tools.Commands.CreateTool;
using Construction.Application.Features.Vehicles.Commands.CreateVehicle;
using Construction.Domain.Enums;

namespace Construction.UnitTests.Validation;

public class VehicleValidatorTests
{
    private readonly CreateVehicleCommandValidator _validator = new();

    private static CreateVehicleCommand Valid() => new()
    {
        Brand = "Ford",
        Model = "Transit",
        RegistrationNumber = "ZG1234AB",
        FuelType = FuelType.Diesel,
        Status = VehicleStatus.Available
    };

    [Fact]
    public void Accepts_a_complete_request()
    {
        ValidationAssert.Valid(_validator, Valid());
    }

    [Fact]
    public void Accepts_a_vehicle_without_a_vin()
    {
        ValidationAssert.Valid(_validator, Valid() with { Vin = null });
    }

    [Fact]
    public void Requires_brand_model_and_registration_number()
    {
        ValidationAssert.Invalid(_validator, Valid() with { Brand = "" }, "Brand");
        ValidationAssert.Invalid(_validator, Valid() with { Model = "" }, "Model");
        ValidationAssert.Invalid(
            _validator, Valid() with { RegistrationNumber = "" }, "RegistrationNumber");
    }

    [Fact]
    public void Caps_the_registration_number_and_vin_at_the_column_width()
    {
        ValidationAssert.Invalid(
            _validator, Valid() with { RegistrationNumber = new string('A', 33) }, "RegistrationNumber");
        ValidationAssert.Invalid(_validator, Valid() with { Vin = new string('A', 33) }, "Vin");
    }

    [Fact]
    public void Rejects_a_fuel_type_or_status_outside_the_enum()
    {
        ValidationAssert.Invalid(_validator, Valid() with { FuelType = (FuelType)99 }, "FuelType");
        ValidationAssert.Invalid(_validator, Valid() with { Status = (VehicleStatus)99 }, "Status");
    }
}

public class ToolValidatorTests
{
    private readonly CreateToolCommandValidator _validator = new();

    private static CreateToolCommand Valid() => new()
    {
        Name = "Cordless drill",
        Status = ToolStatus.Available
    };

    [Fact]
    public void Accepts_a_minimal_request()
    {
        ValidationAssert.Valid(_validator, Valid());
    }

    [Fact]
    public void Accepts_a_tool_with_category_serial_number_and_qr_code()
    {
        ValidationAssert.Valid(_validator, Valid() with
        {
            Category = "Power tools",
            SerialNumber = "SN-9001",
            QrCode = "QR-9001"
        });
    }

    [Fact]
    public void Requires_a_name()
    {
        ValidationAssert.Invalid(_validator, Valid() with { Name = "" }, "Name");
    }

    [Fact]
    public void Caps_the_optional_identifiers_at_their_column_widths()
    {
        ValidationAssert.Invalid(
            _validator, Valid() with { Category = new string('C', 129) }, "Category");
        ValidationAssert.Invalid(
            _validator, Valid() with { SerialNumber = new string('S', 129) }, "SerialNumber");
        ValidationAssert.Invalid(_validator, Valid() with { QrCode = new string('Q', 257) }, "QrCode");
    }

    [Fact]
    public void Rejects_a_status_outside_the_enum()
    {
        ValidationAssert.Invalid(_validator, Valid() with { Status = (ToolStatus)99 }, "Status");
    }
}

public class MaterialValidatorTests
{
    private readonly CreateMaterialCommandValidator _validator = new();

    private static CreateMaterialCommand Valid() => new()
    {
        Name = "Cement",
        Unit = "bag",
        Quantity = 120.5m
    };

    [Fact]
    public void Accepts_a_complete_request()
    {
        ValidationAssert.Valid(_validator, Valid());
    }

    [Fact]
    public void Accepts_zero_stock()
    {
        // A material can legitimately be fully consumed but still tracked.
        ValidationAssert.Valid(_validator, Valid() with { Quantity = 0m });
    }

    [Fact]
    public void Requires_a_name_and_a_unit()
    {
        ValidationAssert.Invalid(_validator, Valid() with { Name = "" }, "Name");
        ValidationAssert.Invalid(_validator, Valid() with { Unit = "" }, "Unit");
    }

    [Fact]
    public void Rejects_a_negative_quantity()
    {
        ValidationAssert.Invalid(_validator, Valid() with { Quantity = -1m }, "Quantity");
    }
}

public class AdjustMaterialQuantityValidatorTests
{
    private readonly AdjustMaterialQuantityCommandValidator _validator = new();

    [Fact]
    public void Accepts_a_positive_change_for_stock_received()
    {
        ValidationAssert.Valid(_validator, new AdjustMaterialQuantityCommand { Change = 25m });
    }

    [Fact]
    public void Accepts_a_negative_change_for_stock_consumed()
    {
        ValidationAssert.Valid(_validator, new AdjustMaterialQuantityCommand { Change = -40m });
    }

    [Fact]
    public void Rejects_a_zero_change_because_it_would_record_nothing()
    {
        ValidationAssert.Invalid(
            _validator, new AdjustMaterialQuantityCommand { Change = 0m }, "Change");
    }

    [Fact]
    public void Caps_the_reason_at_the_column_it_is_stored_in()
    {
        // The reason is now written to the movement's Note, which is 500. It
        // used to be capped at 512 against nothing in particular; a longer
        // one would reach the database and be refused there instead of here.
        ValidationAssert.Valid(
            _validator,
            new AdjustMaterialQuantityCommand { Change = 1m, Reason = new string('r', 500) });
        ValidationAssert.Invalid(
            _validator,
            new AdjustMaterialQuantityCommand { Change = 1m, Reason = new string('r', 501) },
            "Reason");
    }
}
