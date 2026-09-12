using Construction.Domain.Enums;

namespace Construction.Application.Features.Notifications.Services;

/// <summary>
/// Renders a push notification's title/body in the recipient's own language,
/// for a system push queued before the app is even open — the one surface
/// the mobile app's own client-side <c>resolveNotificationText</c> cannot
/// reach, since there is no app running yet to pick a language. Mirrors that
/// Dart resolver's cases and field names exactly (see
/// `notification_text.dart` in the mobile project) so the two stay in step:
/// extending one without the other means the banner and the in-app detail
/// screen disagree.
///
/// English needs no case here — the caller's own <c>title</c>/<c>body</c>
/// (the same plain-English text stored on the <see cref="Domain.Entities.Notification"/>
/// row and shown by desktop) already is the English rendering, so it is
/// simply returned unchanged whenever the recipient's language is not
/// Serbian, or a case here does not recognize the type or is missing a
/// field it needs.
/// </summary>
public static class PushTextResolver
{
    public static (string Title, string Body) Resolve(
        NotificationType type,
        string fallbackTitle,
        string fallbackBody,
        IReadOnlyDictionary<string, string> data,
        string? language)
    {
        if (language != "sr")
        {
            return (fallbackTitle, fallbackBody);
        }

        string? Str(string key) => data.TryGetValue(key, out var value) ? value : null;

        switch (type)
        {
            case NotificationType.ProjectAssigned:
            {
                var projectName = Str("projectName");
                if (projectName is null) break;

                var address = Str("projectAddress");
                var shiftStartTime = Str("projectShiftStartTime");

                var body = address is null
                    ? $"Dodijeljeni ste na gradilište \"{projectName}\"."
                    : shiftStartTime is null
                        ? $"Dodijeljeni ste na gradilište \"{projectName}\", na adresi {address}."
                        : $"Dodijeljeni ste na gradilište \"{projectName}\", na adresi {address}. Smjena počinje u {shiftStartTime}.";

                return ("Dodijeljeni ste na novo gradilište", body);
            }

            case NotificationType.EmployeeAssigned:
            {
                var employeeName = Str("employeeName");
                var projectName = Str("projectName");
                if (employeeName is null || projectName is null) break;

                return (
                    "Radnik dodijeljen na vaše gradilište",
                    $"{employeeName} je dodijeljen(a) na gradilište \"{projectName}\".");
            }

            case NotificationType.VehicleAssigned:
            {
                var brand = Str("vehicleBrand");
                var model = Str("vehicleModel");
                var registration = Str("vehicleRegistration");
                if (brand is null || model is null || registration is null) break;

                return ("Vozilo dodijeljeno", $"Vozilo {brand} {model} ({registration}) vam je dodijeljeno.");
            }

            case NotificationType.ToolAssigned:
            {
                var toolName = Str("toolName");
                if (toolName is null) break;

                return ("Alat dodijeljen", $"Alat \"{toolName}\" vam je dodijeljen.");
            }

            case NotificationType.DocumentExpiring:
            {
                var fileName = Str("fileName");
                var expiresAt = Str("expiresAt");
                if (fileName is null || expiresAt is null) break;

                var ownerName = Str("ownerName");
                var expired = Str("expired") == "true";
                var date = IsoDate(expiresAt);

                var body = ownerName is null
                    ? $"{fileName} ({date})"
                    : $"{fileName} — {ownerName} ({date})";

                return (expired ? "Dokument je istekao" : "Dokument uskoro ističe", body);
            }

            case NotificationType.TaskAssigned:
            case NotificationType.DefectAssigned:
            {
                var title = Str("title");
                if (title is null) break;

                var dueDate = Str("dueDate");
                var body = dueDate is null ? title : $"{title} — rok {IsoDate(dueDate)}";

                return (
                    type == NotificationType.DefectAssigned
                        ? "Kvar vam je dodijeljen"
                        : "Zadatak vam je dodijeljen",
                    body);
            }

            case NotificationType.WorkItemDue:
            {
                var title = Str("title");
                var dueDate = Str("dueDate");
                if (title is null || dueDate is null) break;

                var overdue = Str("overdue") == "true";

                return (overdue ? "Kasni" : "Uskoro dospijeva", $"{title} ({IsoDate(dueDate)})");
            }

            case NotificationType.ShiftAutoClosed:
            {
                var shiftDate = Str("shiftDate");
                if (shiftDate is null) break;

                return (
                    "Smjena automatski zatvorena",
                    $"Niste odjavili smjenu {IsoDate(shiftDate)}, pa je automatski zatvorena i čeka pregled.");
            }

            case NotificationType.AbsenceEditProposed:
            {
                var startDate = Str("startDate");
                var endDate = Str("endDate");
                var approved = Str("approved");

                if (approved == "true" && startDate is not null && endDate is not null)
                {
                    return ("Izmjena odsustva potvrđena", $"{IsoDate(startDate)}–{IsoDate(endDate)}");
                }

                if (approved == "false")
                {
                    return ("Izmjena odsustva odbijena", "Druga strana je odbila predloženu izmjenu.");
                }

                if (startDate is not null && endDate is not null)
                {
                    return (
                        "Predložena izmjena odobrenog odsustva",
                        $"{IsoDate(startDate)}–{IsoDate(endDate)} — potvrdite ili odbijte.");
                }

                break;
            }

            case NotificationType.WeeklyReportDue:
            {
                var projectName = Str("projectName");
                var isoYear = Str("isoYear");
                var isoWeek = Str("isoWeek");
                if (projectName is null || isoYear is null || isoWeek is null) break;

                return ("Sedmični izvještaj još nije predat", $"{projectName} — KW{isoWeek}/{isoYear}");
            }

            case NotificationType.EmployeeClockedIn:
            {
                var employeeName = Str("employeeName");
                var projectName = Str("projectName");
                if (employeeName is null || projectName is null) break;

                return ("Prijava na posao", $"{employeeName} se prijavio(la) na gradilištu {projectName}.");
            }

            case NotificationType.EmployeeClockedOut:
            {
                var employeeName = Str("employeeName");
                var projectName = Str("projectName");
                var workedHours = Str("workedHours");
                var workedMinutes = Str("workedMinutes");
                if (employeeName is null || projectName is null || workedHours is null || workedMinutes is null)
                {
                    break;
                }

                return (
                    "Odjava s posla",
                    $"{employeeName} se odjavio(la) sa gradilišta {projectName} nakon {workedHours}h {workedMinutes}min.");
            }

            case NotificationType.UnassignedProjectClockIn:
            {
                var employeeName = Str("employeeName");
                var projectName = Str("projectName");
                if (employeeName is null || projectName is null) break;

                return (
                    "Prijava na nedodijeljeno gradilište",
                    $"{employeeName} se prijavio(la) na gradilištu {projectName}, iako trenutno nije tamo raspoređen(a).");
            }

            case NotificationType.DefectReported:
            {
                var reporterName = Str("reporterName");
                var title = Str("title");
                if (reporterName is null || title is null) break;

                return ("Prijavljen novi kvar", $"{reporterName} je prijavio(la): {title}");
            }

            case NotificationType.AbsenceRequested:
            {
                var employeeName = Str("employeeName");
                var startDate = Str("startDate");
                var endDate = Str("endDate");
                if (employeeName is null || startDate is null || endDate is null) break;

                return (
                    "Zahtjev za odsustvo",
                    $"{employeeName} je zatražio(la) odsustvo, od {IsoDate(startDate)} do {IsoDate(endDate)}.");
            }

            case NotificationType.DocumentRetentionEnded:
            {
                var fileName = Str("fileName");
                var retainUntil = Str("retainUntil");
                if (fileName is null || retainUntil is null) break;

                var ownerName = Str("ownerName");
                var date = IsoDate(retainUntil);

                var body = ownerName is null
                    ? $"{fileName} više ne mora biti sačuvan (rok čuvanja je bio do {date}). Obrišite ga sami ako više nije potreban."
                    : $"{fileName} — {ownerName} više ne mora biti sačuvan (rok čuvanja je bio do {date}). Obrišite ga sami ako više nije potreban.";

                return ("Period čuvanja dokumenta je istekao", body);
            }
        }

        return (fallbackTitle, fallbackBody);
    }

    /// <summary>
    /// `yyyy-MM-dd` to the day-first `dd.MM.yyyy.` convention the rest of
    /// this app uses — by splitting the string, not parsing it as a
    /// <see cref="DateTime"/>, since a calendar date has no timezone to get
    /// wrong that way. Mirrors mobile's own `_isoDate` helper exactly.
    /// </summary>
    private static string IsoDate(string iso)
    {
        var parts = iso.Split('-');
        return parts.Length == 3 ? $"{parts[2]}.{parts[1]}.{parts[0]}." : iso;
    }
}
