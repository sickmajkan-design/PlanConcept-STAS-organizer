import 'package:flutter/material.dart';

import '../l10n/app_locales.dart';

/// The colour of a subcontractor (kooperant) wherever a person appears: the same purple as the admin panel
/// (`contractorColor` in its theme.ts). Purple appears nowhere else in the app, so it always means "not our own
/// worker", and the word is always shown beside it.
class ContractorColors {
  const ContractorColors._();

  static const Color main = Color(0xFF7B3FB8);
  static const Color soft = Color(0xFFEFE4F8);
  static const Color text = Color(0xFF4A1D7A);

  /// A subcontractor's name as text: dark purple on light, a light purple on dark.
  static Color name(BuildContext context) =>
      Theme.of(context).brightness == Brightness.dark ? const Color(0xFFD9B8F5) : text;
}

/// The small "Kooperant" pill shown beside a subcontractor's name.
class ContractorBadge extends StatelessWidget {
  const ContractorBadge({super.key, this.dense = false});

  final bool dense;

  @override
  Widget build(BuildContext context) {
    final style = Theme.of(context).textTheme.labelSmall?.copyWith(
          color: ContractorColors.text,
          fontWeight: FontWeight.w700,
          fontSize: dense ? 10.5 : null,
        );

    return Container(
      padding: EdgeInsets.symmetric(horizontal: dense ? 7 : 9, vertical: dense ? 1 : 2),
      decoration: BoxDecoration(
        color: ContractorColors.soft,
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: ContractorColors.main),
      ),
      child: Text(context.l10n.employeeTypeSubcontractor, style: style),
    );
  }
}
