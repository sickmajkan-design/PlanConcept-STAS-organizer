/// Turns API enum names such as `ProjectManager` or `OnLeave` into
/// `Project Manager` / `On Leave` for display.
String humanizeEnum(String value) {
  return value
      .replaceAllMapped(
        RegExp('(?<=[a-z])[A-Z]'),
        (match) => ' ${match.group(0)}',
      )
      .trim();
}

/// Avatar initials from a name, falling back to the first character of
/// [fallback] (usually an email) when no name is available.
String initialsOf(String? firstName, String? lastName, {String? fallback}) {
  final first = firstName?.trim() ?? '';
  final last = lastName?.trim() ?? '';

  if (first.isNotEmpty && last.isNotEmpty) {
    return '${first[0]}${last[0]}'.toUpperCase();
  }

  final single = first.isNotEmpty ? first : last;

  if (single.isNotEmpty) {
    return single[0].toUpperCase();
  }

  final alternative = fallback?.trim() ?? '';
  return alternative.isEmpty ? '?' : alternative[0].toUpperCase();
}

String _twoDigits(int value) => value.toString().padLeft(2, '0');

/// Day-first date, the convention on site paperwork.
String formatDate(DateTime? value) {
  if (value == null) {
    return '—';
  }

  final local = value.isUtc ? value.toLocal() : value;
  return '${_twoDigits(local.day)}.${_twoDigits(local.month)}.${local.year}.';
}

String formatDateTime(DateTime? value) {
  if (value == null) {
    return '—';
  }

  final local = value.isUtc ? value.toLocal() : value;
  return '${formatDate(local)} ${_twoDigits(local.hour)}:${_twoDigits(local.minute)}';
}

/// Compact "how long ago" label for timestamps such as the last GPS fix.
String formatRelative(DateTime? value) {
  if (value == null) {
    return 'never';
  }

  final elapsed = DateTime.now().toUtc().difference(value.toUtc());

  if (elapsed.isNegative || elapsed.inSeconds < 60) {
    return 'just now';
  }

  if (elapsed.inMinutes < 60) {
    return '${elapsed.inMinutes} min ago';
  }

  if (elapsed.inHours < 24) {
    return '${elapsed.inHours} h ago';
  }

  if (elapsed.inDays < 7) {
    return '${elapsed.inDays} d ago';
  }

  return formatDate(value);
}
