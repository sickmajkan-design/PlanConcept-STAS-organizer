import 'package:flutter/material.dart';

import '../l10n/app_locales.dart';
import '../l10n/enum_labels.dart';

/// Colour-coded label for the API's status enums. Colours are grouped by
/// meaning rather than per entity, so the same colour always means the same
/// thing across employees, projects, vehicles and tools.
///
/// [kind] says which enum the value comes from — needed because the label is
/// translated and the same value inflects differently per entity. See
/// [EnumKind].
class StatusChip extends StatelessWidget {
  const StatusChip({
    super.key,
    required this.status,
    required this.kind,
    this.dense = false,
  });

  final String status;
  final EnumKind kind;
  final bool dense;

  static const _good = <String>{'Active', 'Available', 'Completed'};
  static const _caution = <String>{'OnLeave', 'Planned', 'OnHold', 'Assigned', 'InService'};
  static const _bad = <String>{'Suspended', 'UnderRepair', 'Lost'};

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    final (background, foreground) = switch (status) {
      _ when _good.contains(status) => (
          scheme.tertiaryContainer,
          scheme.onTertiaryContainer,
        ),
      _ when _caution.contains(status) => (
          scheme.secondaryContainer,
          scheme.onSecondaryContainer,
        ),
      _ when _bad.contains(status) => (
          scheme.errorContainer,
          scheme.onErrorContainer,
        ),
      // Terminated, Cancelled, Retired, OutOfService and anything new.
      _ => (scheme.surfaceContainerHighest, scheme.onSurfaceVariant),
    };

    return Container(
      padding: EdgeInsets.symmetric(
        horizontal: dense ? 8 : 10,
        vertical: dense ? 2 : 4,
      ),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        enumLabel(context.l10n, kind, status),
        style: (dense
                ? Theme.of(context).textTheme.labelSmall
                : Theme.of(context).textTheme.labelMedium)
            ?.copyWith(color: foreground, fontWeight: FontWeight.w600),
      ),
    );
  }
}
