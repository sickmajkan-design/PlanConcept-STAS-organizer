import 'package:timezone/data/latest.dart' as tzdata;
import 'package:timezone/timezone.dart' as tz;

/// The organisation's calendar.
///
/// Which zone it is is a setting of the organisation (company settings), the same one the server and the admin panel
/// keep their calendar in: what day a shift belongs to, what "today" is, what time a clock-in reads as. It is never the
/// phone's zone, which is whatever the handset happens to be set to; a worker who travels, or a phone set wrong, must
/// not move the day a shift belongs to.
///
/// Two kinds of value come out of here, and the difference matters:
///
/// * an INSTANT (a UTC `DateTime`): one moment, the same everywhere;
/// * a WALL CLOCK `DateTime` (not UTC): a `DateTime` whose fields (`year`, `hour`…) read what the organisation's clock
///   read at that moment. It carries the clock's reading to code that already works with `day` and `weekday`; it is
///   never sent to the server or compared with an instant.
class CompanyTime {
  const CompanyTime._();

  /// What the platform uses until the organisation's own setting has been read.
  static const String defaultZoneId = 'Europe/Belgrade';

  static bool _ready = false;
  static String _zoneId = defaultZoneId;
  static tz.Location? _location;

  /// Loads the zone database, once. Safe to call again.
  static void ensureReady() {
    if (_ready) return;

    tzdata.initializeTimeZones();
    _ready = true;
    _location = tz.getLocation(_zoneId);
  }

  static String get zoneId => _zoneId;

  static tz.Location get location {
    ensureReady();
    return _location!;
  }

  /// Takes up the organisation's zone. An id the zone database does not know is ignored (the previous one stays):
  /// a wrong calendar is worse than a slightly old one. Returns whether the zone changed.
  static bool setZone(String? id) {
    ensureReady();

    if (id == null || id.isEmpty || id == _zoneId) return false;

    try {
      _location = tz.getLocation(id);
    } catch (_) {
      return false;
    }

    _zoneId = id;
    return true;
  }

  /// What the organisation's clock reads at [instant], as a wall clock `DateTime`.
  static DateTime wallClock(DateTime instant) {
    final at = tz.TZDateTime.from(instant.toUtc(), location);
    return DateTime(at.year, at.month, at.day, at.hour, at.minute, at.second);
  }

  /// Now, on the organisation's clock.
  static DateTime now() => wallClock(DateTime.now());

  /// Today's date on the organisation's calendar, at midnight (a wall clock `DateTime`).
  static DateTime today() {
    final n = now();
    return DateTime(n.year, n.month, n.day);
  }

  /// The instant at which the organisation's clock reads [wall]: the inverse of [wallClock].
  static DateTime instantOf(DateTime wall) =>
      tz.TZDateTime(location, wall.year, wall.month, wall.day, wall.hour, wall.minute, wall.second).toUtc();
}
