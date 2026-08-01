import 'package:flutter/material.dart';

/// One labelled field on a detail screen.
///
/// Every detail screen shows the same shape — icon, caption, value — and every
/// one of them has fields the API may leave empty, so the em dash placeholder
/// belongs here rather than at each call site. A value that is present but
/// blank is treated as missing, because the API returns `""` and `null`
/// interchangeably for optional text.
class InfoTile extends StatelessWidget {
  const InfoTile({
    super.key,
    required this.icon,
    required this.label,
    this.value,
  });

  final IconData icon;
  final String label;
  final String? value;

  @override
  Widget build(BuildContext context) {
    final hasValue = (value ?? '').trim().isNotEmpty;

    return ListTile(
      leading: Icon(icon),
      title: Text(label, style: Theme.of(context).textTheme.bodySmall),
      subtitle: Text(
        hasValue ? value! : '—',
        style: Theme.of(context).textTheme.bodyLarge,
      ),
      dense: true,
    );
  }
}
