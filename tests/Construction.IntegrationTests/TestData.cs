using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Construction.Infrastructure.Authentication;

namespace Construction.IntegrationTests;

/// <summary>Seeding helpers, so each test states only what it actually cares about.</summary>
public static class TestData
{
    public const string Password = "Test1234!";

    private static readonly IPasswordHasher Hasher = new PasswordHasher();

    private static string UniqueSuffix() => Guid.NewGuid().ToString("N")[..10];

    public static async Task<User> SeedUserAsync(
        TestScope scope,
        UserRole role = UserRole.Admin,
        Guid? employeeId = null,
        bool isActive = true,
        string? email = null)
    {
        var user = new User
        {
            Email = email ?? $"user-{UniqueSuffix()}@construction.test",
            PasswordHash = Hasher.Hash(Password),
            Role = role,
            IsActive = isActive,
            EmployeeId = employeeId
        };

        scope.Db.Users.Add(user);
        await scope.Db.SaveChangesAsync();

        return user;
    }

    public static async Task<Employee> SeedEmployeeAsync(
        TestScope scope,
        string? employeeNumber = null,
        string firstName = "Ivan",
        string lastName = "Horvat",
        EmployeeStatus status = EmployeeStatus.Active)
    {
        var employee = new Employee
        {
            EmployeeNumber = employeeNumber ?? $"EMP-{UniqueSuffix()}",
            FirstName = firstName,
            LastName = lastName,
            Position = "Site Manager",
            EmploymentDate = new DateOnly(2020, 3, 1),
            Status = status
        };

        scope.Db.Employees.Add(employee);
        await scope.Db.SaveChangesAsync();

        return employee;
    }

    public static async Task<Customer> SeedCustomerAsync(TestScope scope, string? name = null)
    {
        var customer = new Customer
        {
            Name = name ?? $"Customer {UniqueSuffix()}"
        };

        scope.Db.Customers.Add(customer);
        await scope.Db.SaveChangesAsync();

        return customer;
    }

    public static async Task<Project> SeedProjectAsync(
        TestScope scope,
        string? name = null,
        ProjectStatus status = ProjectStatus.Active,
        string? countryCode = null)
    {
        var project = new Project
        {
            Name = name ?? $"Project {UniqueSuffix()}",
            Status = status,
            CountryCode = countryCode,
        };

        scope.Db.Projects.Add(project);
        await scope.Db.SaveChangesAsync();

        return project;
    }

    public static async Task<Material> SeedMaterialAsync(
        TestScope scope,
        decimal quantity,
        string unit = "bag",
        string? name = null)
    {
        var material = new Material
        {
            Name = name ?? $"Material {UniqueSuffix()}",
            Unit = unit,
            Quantity = quantity,
            LastUpdated = DateTime.UtcNow
        };

        scope.Db.Materials.Add(material);
        await scope.Db.SaveChangesAsync();

        return material;
    }

    public static async Task<Tool> SeedToolAsync(TestScope scope, string? name = null)
    {
        var tool = new Tool
        {
            Name = name ?? $"Tool {UniqueSuffix()}",
            Status = ToolStatus.Available
        };

        scope.Db.Tools.Add(tool);
        await scope.Db.SaveChangesAsync();

        return tool;
    }

    public static async Task<Vehicle> SeedVehicleAsync(
        TestScope scope,
        string? registrationNumber = null)
    {
        var vehicle = new Vehicle
        {
            Brand = "Ford",
            Model = "Transit",
            RegistrationNumber = registrationNumber ?? $"ZG{UniqueSuffix()}",
            FuelType = FuelType.Diesel,
            Status = VehicleStatus.Available
        };

        scope.Db.Vehicles.Add(vehicle);
        await scope.Db.SaveChangesAsync();

        return vehicle;
    }

    public static async Task<VehicleRentalRate> SeedVehicleRentalRateAsync(
        TestScope scope,
        Guid? vehicleId = null)
    {
        var rate = new VehicleRentalRate
        {
            VehicleId = vehicleId ?? (await SeedVehicleAsync(scope)).Id,
            MonthlyAmount = 350m,
            StartDate = new DateOnly(2026, 1, 1)
        };

        scope.Db.VehicleRentalRates.Add(rate);
        await scope.Db.SaveChangesAsync();

        return rate;
    }

    public static async Task<GeneralExpense> SeedGeneralExpenseAsync(
        TestScope scope,
        GeneralExpenseCategory category = GeneralExpenseCategory.Bookkeeping)
    {
        var expense = new GeneralExpense
        {
            Category = category,
            Amount = 120m,
            OccurredOn = new DateOnly(2026, 1, 15)
        };

        scope.Db.GeneralExpenses.Add(expense);
        await scope.Db.SaveChangesAsync();

        return expense;
    }

    public static async Task<Accommodation> SeedAccommodationAsync(
        TestScope scope,
        string? address = null)
    {
        var accommodation = new Accommodation
        {
            Address = address ?? $"Ulica {UniqueSuffix()}"
        };

        scope.Db.Accommodations.Add(accommodation);
        await scope.Db.SaveChangesAsync();

        return accommodation;
    }

    public static async Task<AccommodationRate> SeedAccommodationRateAsync(
        TestScope scope,
        Guid? accommodationId = null)
    {
        var rate = new AccommodationRate
        {
            AccommodationId = accommodationId ?? (await SeedAccommodationAsync(scope)).Id,
            MonthlyAmount = 300m,
            StartDate = new DateOnly(2026, 1, 1)
        };

        scope.Db.AccommodationRates.Add(rate);
        await scope.Db.SaveChangesAsync();

        return rate;
    }
}
