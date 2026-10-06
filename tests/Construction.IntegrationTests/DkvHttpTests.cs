using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The DKV statement check as the office and the drivers meet it: real HTTP
/// requests, a file in DKV's own column layout, drivers recording fill-ups the
/// way the phone does, and the notice that goes out when the two disagree.
/// </summary>
[Collection(ApiCollection.Name)]
public class DkvHttpTests
{
    private readonly ApiFixture _api;

    public DkvHttpTests(ApiFixture api)
    {
        _api = api;
    }

    /// <summary>The columns and headings of DKV's own export, in its own order.</summary>
    private const string DkvHeader =
        "Št. kartice/kataloga;Registrska številka vozila;Čas transakcije;Status računa;Stroškovna skupina;"
        + "Skupina izdelkov;Vrsta izdelka;Koda izdelka;Skupna vrednost bruto (valuta računa);Država storitve";

    private static string DkvLine(
        string card, string label, DateOnly date, string time, decimal amount,
        string group = "Dizelsko gorivo", string product = "Dizelsko gorivo", string code = "WA0009",
        string status = "Obračunano") =>
        string.Create(CultureInfo.InvariantCulture,
            $"{card};{label};{date:dd.MM.yyyy} - {time};{status};Stroški goriva;{group};{product};{code};{amount:0.00} EUR;DE");

    private static string NewCard() => "7043" + Guid.NewGuid().ToString("N")[..14];

    /// <summary>A number-only TD, as on the real statement, that no other test will have used.</summary>
    private static string NewTd() => Random.Shared.Next(100_000, 999_999).ToString(CultureInfo.InvariantCulture);

    private static MultipartFormDataContent Upload(params string[] lines)
    {
        var bytes = Encoding.UTF8.GetBytes(string.Join("\r\n", new[] { DkvHeader }.Concat(lines)));
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new("text/csv");

        return new MultipartFormDataContent { { file, "file", "Fuel_Report.csv" } };
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, text);

        return JsonDocument.Parse(text).RootElement;
    }

    /// <summary>
    /// What the phone does for a fill-up: record the cost with litres and kilometres, then send the
    /// photograph of the receipt as an attachment on it.
    /// </summary>
    private static async Task<Guid> DriverFillsUpAsync(
        HttpClient driver, Guid vehicleId, DateOnly day, decimal amount, bool sendReceipt = true)
    {
        var expenseId = await CreateAsync(driver, "/api/v1/vehicle-expenses", new
        {
            vehicleId,
            kind = "Fuel",
            amount,
            occurredOn = day.ToString("yyyy-MM-dd"),
            litres = 61m,
            odometerKm = 98_000
        });

        if (sendReceipt)
        {
            // A real (tiny) JPEG: start-of-image marker, end-of-image marker.
            var photo = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0xFF, 0xD9]);
            photo.Headers.ContentType = new("image/jpeg");

            var form = new MultipartFormDataContent
            {
                { new StringContent("VehicleExpense"), "ownerType" },
                { new StringContent(expenseId.ToString()), "ownerId" },
                { new StringContent("Photo"), "category" },
                { photo, "file", "racun.jpg" }
            };

            var response = await driver.PostAsync("/api/v1/attachments", form);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        }

        return expenseId;
    }

    private async Task<Guid> VehicleWithCardAsync(HttpClient admin, string td, string card, string fuelType = "Diesel")
    {
        var vehicleId = await CreateAsync(admin, "/api/v1/vehicles", new
        {
            brand = "Iveco",
            model = "Daily",
            registrationNumber = $"QA-{Guid.NewGuid():N}"[..12],
            tdNumber = td,
            fuelType,
        });

        await CreateAsync(admin, "/api/v1/fuel-cards", new { vehicleId, provider = "DKV", cardNumber = card });

        return vehicleId;
    }

    [Fact]
    public async Task A_week_of_fuelling_is_checked_against_what_the_drivers_recorded()
    {
        using var office = _api.ClientAs(UserRole.SuperAdmin);
        using var driver = _api.ClientAs(UserRole.Foreman);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var day1 = today.AddDays(-5);
        var day2 = today.AddDays(-4);

        var (tdA, cardA) = (NewTd(), NewCard());
        var (tdB, cardB) = (NewTd(), NewCard());
        var (tdC, cardC) = (NewTd(), NewCard());
        var stranger = NewCard();

        var vehicleA = await VehicleWithCardAsync(office, tdA, cardA);
        var vehicleB = await VehicleWithCardAsync(office, tdB, cardB);
        var vehicleC = await VehicleWithCardAsync(office, tdC, cardC); // a diesel van

        // What the drivers did at the pump: litres, kilometres and the receipt amount.
        await DriverFillsUpAsync(driver, vehicleA, day1, 140.02m);
        await DriverFillsUpAsync(driver, vehicleB, day1, 100.00m);

        var lines = new[]
        {
            // agrees with the driver's entry
            DkvLine(cardA, tdA, day1, "06:37", 140.02m),
            // the driver recorded 100.00, DKV says 117.31
            DkvLine(cardB, "SD SMART", day1, "17:17", 117.31m),
            // petrol on a diesel van
            DkvLine(cardC, tdC, day2, "03:04", 99.00m, group: "Bencin", product: "Bencin - RON 95", code: "WA0036"),
            // a card nobody has registered
            DkvLine(stranger, "SD SMART", day2, "11:39", 121.05m),
            // AdBlue on A, which nobody recorded
            DkvLine(cardA, tdA, day2, "18:23", 18.02m, group: "AdBlue", product: "AdBlue (pločevinke)", code: "WA0019"),
        };

        // ---- preview: reads, reports, writes nothing -------------------------------------
        var preview = await JsonAsync(await office.PostAsync("/api/v1/fuel-transactions/import/preview", Upload(lines)));

        Assert.Equal(5, preview.GetProperty("totalRows").GetInt32());
        Assert.Equal(5, preview.GetProperty("newCount").GetInt32());
        Assert.Equal(1, preview.GetProperty("matchedCount").GetInt32());
        Assert.Equal(2, preview.GetProperty("needsReviewCount").GetInt32());
        Assert.Equal(1, preview.GetProperty("noDriverEntryCount").GetInt32());
        Assert.Equal(1, preview.GetProperty("unknownCardCount").GetInt32());
        Assert.Equal(stranger, preview.GetProperty("unknownCards")[0].GetProperty("cardNumber").GetString());

        var beforeImport = await JsonAsync(await office.GetAsync($"/api/v1/fuel-transactions?cardNumber={cardA}"));
        Assert.Equal(0, beforeImport.GetProperty("totalCount").GetInt32());

        // ---- import ------------------------------------------------------------------------
        var imported = await JsonAsync(await office.PostAsync("/api/v1/fuel-transactions/import", Upload(lines)));
        var batchId = imported.GetProperty("batchId").GetGuid();

        Assert.Equal(5, imported.GetProperty("newCount").GetInt32());

        // Uploading the same file again changes nothing.
        var again = await JsonAsync(await office.PostAsync("/api/v1/fuel-transactions/import", Upload(lines)));
        Assert.Equal(0, again.GetProperty("newCount").GetInt32());
        Assert.Equal(5, again.GetProperty("duplicateCount").GetInt32());

        // ---- the office is told ---------------------------------------------------------------
        using var otherAdmin = _api.ClientAs(UserRole.Admin);
        var inbox = await JsonAsync(await otherAdmin.GetAsync("/api/v1/notifications?pageNumber=1&pageSize=50"));
        var notice = inbox.GetProperty("items").EnumerateArray()
            .FirstOrDefault(n => n.GetProperty("type").GetString() == "DkvStatementMismatch"
                && n.GetProperty("dataJson").GetString()!.Contains(batchId.ToString()));

        Assert.NotEqual(JsonValueKind.Undefined, notice.ValueKind);
        var data = JsonDocument.Parse(notice.GetProperty("dataJson").GetString()!).RootElement;
        Assert.Equal("4", data.GetProperty("total").GetString());
        Assert.Equal("2", data.GetProperty("needsReview").GetString());
        Assert.Equal("1", data.GetProperty("noDriverEntry").GetString());
        Assert.Equal("1", data.GetProperty("unknownCard").GetString());

        // The one who uploaded it is not pinged about their own upload.
        using var uploader = _api.ClientAs(UserRole.SuperAdmin);
        var uploaderInbox = await JsonAsync(await uploader.GetAsync("/api/v1/notifications?pageNumber=1&pageSize=50"));
        Assert.DoesNotContain(uploaderInbox.GetProperty("items").EnumerateArray(),
            n => n.GetProperty("type").GetString() == "DkvStatementMismatch"
                && n.GetProperty("dataJson").GetString()!.Contains(batchId.ToString()));

        // ---- the to-do list (repeated status keys bind as a list) -----------------------------------
        var todo = await JsonAsync(await office.GetAsync(
            $"/api/v1/fuel-transactions?batchId={batchId}&status=NeedsReview&status=UnknownCard&pageNumber=1&pageSize=50"));
        Assert.Equal(3, todo.GetProperty("totalCount").GetInt32());

        var counts = await JsonAsync(await office.GetAsync("/api/v1/fuel-transactions/counts"));
        Assert.True(counts.GetProperty("NeedsReview").GetInt32() >= 2);

        // ---- settling a row ---------------------------------------------------------------------------
        var amountRow = todo.GetProperty("items").EnumerateArray()
            .First(r => r.GetProperty("issue").GetString() == "AmountMismatch");
        var rowId = amountRow.GetProperty("id").GetGuid();

        Assert.Equal(100.00m, amountRow.GetProperty("expenseAmount").GetDecimal());
        Assert.Equal(117.31m, amountRow.GetProperty("amount").GetDecimal());

        var noReason = await office.PostAsJsonAsync($"/api/v1/fuel-transactions/{rowId}/resolve",
            new { resolution = "Confirm" });
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        var settled = await JsonAsync(await office.PostAsJsonAsync($"/api/v1/fuel-transactions/{rowId}/resolve",
            new { resolution = "Confirm", note = "Driver misread the receipt; DKV figure is right." }));
        Assert.Equal("Resolved", settled.GetProperty("status").GetString());

        // ---- the unknown card gets a vehicle and its rows are checked again -----------------------------
        var assigned = await office.PostAsJsonAsync("/api/v1/fuel-transactions/assign-card",
            new { cardNumber = stranger, vehicleId = vehicleB });
        assigned.EnsureSuccessStatusCode();

        var strangerRows = await JsonAsync(await office.GetAsync($"/api/v1/fuel-transactions?cardNumber={stranger}"));
        Assert.NotEqual("UnknownCard", strangerRows.GetProperty("items")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_fill_up_without_the_receipt_photo_is_reported_to_the_office()
    {
        using var office = _api.ClientAs(UserRole.SuperAdmin);
        using var driver = _api.ClientAs(UserRole.Foreman);

        var day = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3);
        var (td, card) = (NewTd(), NewCard());
        var vehicle = await VehicleWithCardAsync(office, td, card);

        await DriverFillsUpAsync(driver, vehicle, day, 120.50m, sendReceipt: false);

        var imported = await JsonAsync(await office.PostAsync("/api/v1/fuel-transactions/import",
            Upload(DkvLine(card, td, day, "07:15", 120.50m))));
        var batchId = imported.GetProperty("batchId").GetGuid();

        var rows = await JsonAsync(await office.GetAsync($"/api/v1/fuel-transactions?batchId={batchId}"));
        var row = Assert.Single(rows.GetProperty("items").EnumerateArray());

        Assert.Equal("NeedsReview", row.GetProperty("status").GetString());
        Assert.Equal("IncompleteEntry", row.GetProperty("issue").GetString());
        Assert.Contains("receipt photo", row.GetProperty("issueDetail").GetString());
    }

    [Fact]
    public async Task Drivers_cannot_see_or_upload_the_statement()
    {
        using var driver = _api.ClientAs(UserRole.Foreman);

        var upload = await driver.PostAsync("/api/v1/fuel-transactions/import", Upload("x"));
        var list = await driver.GetAsync("/api/v1/fuel-transactions");

        Assert.Equal(HttpStatusCode.Forbidden, upload.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
    }

    [Fact]
    public async Task A_file_that_is_not_a_dkv_statement_gets_a_clear_refusal()
    {
        using var office = _api.ClientAs(UserRole.SuperAdmin);

        var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(Encoding.UTF8.GetBytes("Name;Amount\r\na;1")), "file", "wrong.csv" }
        };

        var response = await office.PostAsync("/api/v1/fuel-transactions/import/preview", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("DKV", body);
    }

    [Fact]
    public async Task A_vehicle_cannot_be_saved_without_a_td_number()
    {
        using var office = _api.ClientAs(UserRole.SuperAdmin);

        var response = await office.PostAsJsonAsync("/api/v1/vehicles", new
        {
            brand = "Iveco",
            model = "Daily",
            registrationNumber = $"QA-{Guid.NewGuid():N}"[..12],
            fuelType = "Diesel",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("TdNumber", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
}
