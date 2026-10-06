using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A driver below Foreman records fuel for the vehicle signed out to them, photographs the receipt,
/// and sees their own fill-ups: and nothing beyond that.
/// </summary>
[Collection(ApiCollection.Name)]
public class DriverFuelTests
{
    private readonly ApiFixture _api;

    public DriverFuelTests(ApiFixture api)
    {
        _api = api;
    }

    private async Task<Guid> WorkerEmployeeIdAsync()
    {
        var userId = _api.UserIds[UserRole.Worker];

        return await _api.InScope(db => db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.EmployeeId!.Value)
            .SingleAsync());
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static Task<Guid> NewVehicleAsync(HttpClient office) => CreateAsync(office, "/api/v1/vehicles", new
    {
        brand = "Iveco",
        model = "Daily",
        registrationNumber = $"QA-{Guid.NewGuid():N}"[..12],
        tdNumber = $"TD-{Guid.NewGuid():N}"[..12],
        fuelType = "Diesel",
    });

    private async Task<Guid> VehicleInTheWorkersHandsAsync()
    {
        using var office = _api.ClientAs(UserRole.SuperAdmin);
        var vehicleId = await NewVehicleAsync(office);
        var employeeId = await WorkerEmployeeIdAsync();

        (await office.PostAsync($"/api/v1/vehicles/{vehicleId}/assign/{employeeId}", null)).EnsureSuccessStatusCode();

        return vehicleId;
    }

    private static object Fuel(Guid vehicleId, string kind = "Fuel") => new
    {
        vehicleId,
        kind,
        amount = 88.40m,
        occurredOn = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
        litres = kind == "Fuel" ? 52.3m : (decimal?)null,
        odometerKm = 120_500,
    };

    private static MultipartFormDataContent Receipt(Guid expenseId)
    {
        var photo = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0xFF, 0xD9]);
        photo.Headers.ContentType = new("image/jpeg");

        return new MultipartFormDataContent
        {
            { new StringContent("VehicleExpense"), "ownerType" },
            { new StringContent(expenseId.ToString()), "ownerId" },
            { new StringContent("Photo"), "category" },
            { photo, "file", "racun.jpg" }
        };
    }

    [Fact]
    public async Task A_worker_sees_only_the_vehicle_signed_out_to_them()
    {
        var mine = await VehicleInTheWorkersHandsAsync();
        using var office = _api.ClientAs(UserRole.SuperAdmin);
        var someoneElses = await NewVehicleAsync(office);

        using var worker = _api.ClientAs(UserRole.Worker);
        var response = await worker.GetFromJsonAsync<JsonElement>("/api/v1/vehicle-fuel/mine");
        var ids = response.EnumerateArray().Select(v => v.GetProperty("id").GetGuid()).ToList();

        Assert.Contains(mine, ids);
        Assert.DoesNotContain(someoneElses, ids);
    }

    [Fact]
    public async Task The_path_app_1_1_18_was_released_with_still_answers()
    {
        // The app on phones asks for the vehicle in hand at the path it shipped with; renaming it would
        // have left every installed copy with an empty vehicle list.
        using var worker = _api.ClientAs(UserRole.Worker);

        var response = await worker.GetAsync("/api/v1/vehicle-fuel/vehicles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_worker_records_a_fill_up_photographs_the_receipt_and_finds_it_in_their_list()
    {
        var vehicleId = await VehicleInTheWorkersHandsAsync();
        using var worker = _api.ClientAs(UserRole.Worker);

        var expenseId = await CreateAsync(worker, "/api/v1/vehicle-fuel", Fuel(vehicleId));

        var photo = await worker.PostAsync("/api/v1/attachments", Receipt(expenseId));
        Assert.True(photo.IsSuccessStatusCode, await photo.Content.ReadAsStringAsync());

        var mine = await worker.GetFromJsonAsync<JsonElement>("/api/v1/vehicle-fuel?pageNumber=1&pageSize=50");
        Assert.Contains(
            mine.GetProperty("items").EnumerateArray(),
            e => e.GetProperty("id").GetGuid() == expenseId);
    }

    [Fact]
    public async Task A_worker_cannot_record_fuel_for_a_vehicle_nobody_gave_them()
    {
        using var office = _api.ClientAs(UserRole.SuperAdmin);
        var someoneElses = await NewVehicleAsync(office);

        using var worker = _api.ClientAs(UserRole.Worker);
        var response = await worker.PostAsJsonAsync("/api/v1/vehicle-fuel", Fuel(someoneElses));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_worker_can_record_fuel_and_nothing_else()
    {
        var vehicleId = await VehicleInTheWorkersHandsAsync();
        using var worker = _api.ClientAs(UserRole.Worker);

        var repair = await worker.PostAsJsonAsync("/api/v1/vehicle-fuel", Fuel(vehicleId, kind: "Repair"));

        Assert.Equal(HttpStatusCode.Forbidden, repair.StatusCode);
    }

    [Fact]
    public async Task A_worker_cannot_put_a_receipt_on_a_cost_somebody_else_recorded()
    {
        using var office = _api.ClientAs(UserRole.SuperAdmin);
        var vehicleId = await NewVehicleAsync(office);
        var officeExpense = await CreateAsync(office, "/api/v1/vehicle-expenses", Fuel(vehicleId));

        using var worker = _api.ClientAs(UserRole.Worker);
        var response = await worker.PostAsync("/api/v1/attachments", Receipt(officeExpense));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_rest_of_the_cost_module_stays_closed_to_a_worker()
    {
        using var worker = _api.ClientAs(UserRole.Worker);

        Assert.Equal(HttpStatusCode.Forbidden, (await worker.GetAsync("/api/v1/vehicle-expenses")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await worker.GetAsync("/api/v1/fuel-cards")).StatusCode);
    }

    [Fact]
    public async Task A_worker_fill_up_with_everything_attached_matches_the_dkv_statement()
    {
        var vehicleId = await VehicleInTheWorkersHandsAsync();
        using var office = _api.ClientAs(UserRole.SuperAdmin);
        using var worker = _api.ClientAs(UserRole.Worker);

        var card = "7043" + Guid.NewGuid().ToString("N")[..14];
        await CreateAsync(office, "/api/v1/fuel-cards", new { vehicleId, provider = "DKV", cardNumber = card });

        var expenseId = await CreateAsync(worker, "/api/v1/vehicle-fuel", Fuel(vehicleId));
        (await worker.PostAsync("/api/v1/attachments", Receipt(expenseId))).EnsureSuccessStatusCode();

        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var header =
            "Št. kartice/kataloga;Registrska številka vozila;Čas transakcije;Status računa;Stroškovna skupina;"
            + "Skupina izdelkov;Vrsta izdelka;Koda izdelka;Skupna vrednost bruto (valuta računa);Država storitve";
        var line = $"{card};SD SMART;{day:dd.MM.yyyy} - 06:10;Obračunano;Stroški goriva;Dizelsko gorivo;Dizelsko gorivo;WA0009;88.40 EUR;DE";

        var file = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(header + "\r\n" + line));
        var upload = new MultipartFormDataContent { { file, "file", "Fuel_Report.csv" } };

        var result = await office.PostAsync("/api/v1/fuel-transactions/import", upload);
        var batchId = (await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("batchId").GetGuid();

        var rows = await office.GetFromJsonAsync<JsonElement>($"/api/v1/fuel-transactions?batchId={batchId}");
        var row = Assert.Single(rows.GetProperty("items").EnumerateArray());

        Assert.Equal("Matched", row.GetProperty("status").GetString());
        Assert.Equal(expenseId, row.GetProperty("vehicleExpenseId").GetGuid());
    }
}
