using Construction.Application.Features.FuelCards.Import;
using Construction.Application.Features.Materials.Import;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Accommodations.Import;

/// <summary>An existing stay, as far as the importer needs to know it.</summary>
public readonly record struct KnownStay(Guid EmployeeId, Guid AccommodationId, DateOnly Start, DateOnly? End);

/// <summary>
/// Reads the rows of an uploaded housing list: one row per person staying (or
/// per accommodation, when the person column is empty). The columns are found by
/// name, Serbian or English, like the delivery import.
/// </summary>
public static class AccommodationRowResolver
{
    private static readonly Dictionary<string, string[]> Aliases = new()
    {
        ["address"] = ["address", "adresa", "ulica"],
        ["name"] = ["name", "naziv", "smjestaj", "smestaj", "objekat"],
        ["city"] = ["city", "grad", "mjesto"],
        ["type"] = ["type", "tip", "vrsta"],
        ["floor"] = ["floor", "sprat", "kat"],
        ["rooms"] = ["rooms", "sobe", "brojsoba"],
        ["beds"] = ["beds", "lezajevi", "kreveti", "brojlezaja", "brojkreveta"],
        ["rent"] = ["rent", "monthlyrent", "kirija", "mjesecnakirija", "najam"],
        ["employee"] = ["employee", "radnik", "zaposleni", "brojradnika", "employeenumber", "sifraradnika"],
        ["movein"] = ["movein", "from", "od", "useljenje", "datumuseljenja", "pocetak"],
        ["moveout"] = ["moveout", "to", "do", "iseljenje", "datumiseljenja", "kraj"],
        ["project"] = ["project", "gradiliste", "projekat", "projekt"],
    };

    private static readonly Dictionary<string, AccommodationType> Types = new()
    {
        ["stan"] = AccommodationType.Apartment,
        ["apartman"] = AccommodationType.Apartment,
        ["apartment"] = AccommodationType.Apartment,
        ["kuca"] = AccommodationType.House,
        ["house"] = AccommodationType.House,
        ["soba"] = AccommodationType.Room,
        ["room"] = AccommodationType.Room,
        ["hotel"] = AccommodationType.Hotel,
        ["pansion"] = AccommodationType.Hotel,
        ["hostel"] = AccommodationType.Hotel,
        ["ostalo"] = AccommodationType.Other,
        ["other"] = AccommodationType.Other,
    };

    /// <summary>Lowercase, spaces collapsed: "Ulica  1" and "ulica 1" name the same place.</summary>
    public static string AddressKey(string address) =>
        string.Join(' ', address.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static IReadOnlyList<AccommodationImportRowResult> Resolve(
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyDictionary<string, Guid> accommodationIdsByAddress,
        IReadOnlyDictionary<string, (Guid Id, string Name)> employeesByKey,
        IReadOnlyDictionary<string, Guid> projectIdsByName,
        IReadOnlyCollection<KnownStay> existingStays,
        DateOnly today)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var header = rows[0];
        var columns = new Dictionary<string, int>();

        for (var i = 0; i < header.Count; i++)
        {
            var name = MaterialDeliveryRowResolver.Normalise(header[i]);

            foreach (var (field, names) in Aliases)
            {
                if (!columns.ContainsKey(field) && names.Contains(name))
                {
                    columns[field] = i;
                }
            }
        }

        string? Cell(IReadOnlyList<string> row, string field)
        {
            var text = columns.TryGetValue(field, out var index) && index < row.Count
                ? row[index].Trim()
                : null;

            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        var results = new List<AccommodationImportRowResult>();
        var newAddresses = new HashSet<string>();
        var stays = existingStays.ToList();

        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];

            var address = Cell(row, "address");
            var typeText = Cell(row, "type");
            var roomsText = Cell(row, "rooms");
            var bedsText = Cell(row, "beds");
            var rentText = Cell(row, "rent");
            var employeeText = Cell(row, "employee");
            var projectText = Cell(row, "project");
            var moveInText = Cell(row, "movein");
            var moveOutText = Cell(row, "moveout");

            var status = AccommodationImportRowStatus.Ready;
            var type = AccommodationType.Apartment;
            int? rooms = null;
            int? beds = null;
            decimal? rent = null;
            DateOnly? moveIn = null;
            DateOnly? moveOut = null;
            (Guid Id, string Name)? employee = null;
            Guid? projectId = null;
            var creates = false;
            var createsStay = false;

            if (address is null)
            {
                status = AccommodationImportRowStatus.MissingAddress;
            }
            else if (typeText is not null
                && !Types.TryGetValue(MaterialDeliveryRowResolver.Normalise(typeText), out type))
            {
                status = AccommodationImportRowStatus.InvalidType;
            }
            else if ((roomsText is not null && !TryPositiveInt(roomsText, out rooms))
                || (bedsText is not null && !TryPositiveInt(bedsText, out beds))
                || (rentText is not null
                    && !(FuelStatementValueParser.TryParseDecimal(rentText, out var parsedRent)
                         && (rent = parsedRent) > 0)))
            {
                status = AccommodationImportRowStatus.InvalidNumber;
            }
            else if ((moveInText is not null && !TryDate(moveInText, out moveIn))
                || (moveOutText is not null && !TryDate(moveOutText, out moveOut))
                || (moveOut is not null && moveOut < (moveIn ?? today)))
            {
                status = AccommodationImportRowStatus.InvalidDate;
            }
            else if (employeeText is not null)
            {
                if (employeesByKey.TryGetValue(employeeText.ToLowerInvariant(), out var found))
                {
                    employee = found;
                }
                else
                {
                    status = AccommodationImportRowStatus.UnknownEmployee;
                }
            }

            if (status == AccommodationImportRowStatus.Ready && projectText is not null)
            {
                if (projectIdsByName.TryGetValue(projectText.ToLowerInvariant(), out var foundProject))
                {
                    projectId = foundProject;
                }
                else
                {
                    status = AccommodationImportRowStatus.UnknownProject;
                }
            }

            if (status == AccommodationImportRowStatus.Ready)
            {
                var key = AddressKey(address!);
                var knownId = accommodationIdsByAddress.TryGetValue(key, out var existingId) ? existingId : (Guid?)null;
                creates = knownId is null && !newAddresses.Contains(key);
                createsStay = employee is not null;

                if (employee is { } person)
                {
                    var start = moveIn ?? today;

                    // A row for a new address has no id yet; the address key stands in for it.
                    var accommodationId = knownId ?? Guid.Empty;

                    var sameAgain = knownId is not null && stays.Any(s =>
                        s.EmployeeId == person.Id && s.AccommodationId == knownId && s.Start == start);

                    if (sameAgain)
                    {
                        status = AccommodationImportRowStatus.AlreadyImported;
                    }
                    else if (stays.Any(s => s.EmployeeId == person.Id
                        && s.Start <= (moveOut ?? DateOnly.MaxValue)
                        && (s.End ?? DateOnly.MaxValue) >= start))
                    {
                        status = AccommodationImportRowStatus.StayConflict;
                    }
                    else
                    {
                        stays.Add(new KnownStay(person.Id, accommodationId, start, moveOut));
                    }
                }
                else if (!creates)
                {
                    status = AccommodationImportRowStatus.AlreadyImported;
                }

                if (status == AccommodationImportRowStatus.Ready && creates)
                {
                    newAddresses.Add(key);
                }
            }

            results.Add(new AccommodationImportRowResult
            {
                RowNumber = i + 1,
                Address = address,
                Name = Cell(row, "name"),
                City = Cell(row, "city"),
                Type = type,
                Floor = Cell(row, "floor"),
                Rooms = rooms,
                Beds = beds,
                MonthlyRent = rent,
                EmployeeText = employeeText,
                EmployeeId = employee?.Id,
                EmployeeName = employee?.Name,
                ProjectText = projectText,
                ProjectId = projectId,
                MoveIn = moveIn,
                MoveOut = moveOut,
                CreatesAccommodation = status == AccommodationImportRowStatus.Ready && creates,
                CreatesStay = status == AccommodationImportRowStatus.Ready && createsStay,
                Status = status
            });
        }

        return results;
    }

    private static bool TryPositiveInt(string text, out int? value)
    {
        if (int.TryParse(text, out var parsed) && parsed > 0)
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryDate(string text, out DateOnly? value)
    {
        if (FuelStatementValueParser.TryParseDate(text, out var parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }
}
