import 'package:flutter/material.dart';

/// Material 3 theme built around a high-visibility safety amber — readable
/// on site, in daylight, through a scratched screen protector.
class AppTheme {
  const AppTheme._();

  static const Color _seed = Color(0xFFF57C00);

  /// The deep brown of the "today" card, and the cream text on it. Fixed in
  /// both light and dark: the card is the one dark object on the home screen
  /// and has to read the same in sun and in shade.
  static const Color heroCard = Color(0xFF3B2416);
  static const Color heroCardText = Color(0xFFF6EEE4);

  /// The burnt orange that marks what needs doing: a priority, a selection.
  static const Color accent = Color(0xFFB5541C);

  /// A filled button that sits beside another widget in a `Row`.
  ///
  /// The theme gives every filled button `Size.fromHeight(52)`, whose minimum
  /// width is infinite — right for a button that fills a column, and impossible
  /// in a row, where the width is not bounded: the button is then not laid out
  /// and is not drawn at all, without any message in a release build. A confirm
  /// button in a `Row` (the clock-out sheet, for one) simply was not there.
  /// This keeps the height and lets the width follow the label.
  static ButtonStyle get inlineFilledButton =>
      FilledButton.styleFrom(minimumSize: const Size(0, 52));

  static ThemeData light() => _build(Brightness.light);

  static ThemeData dark() => _build(Brightness.dark);

  static ThemeData _build(Brightness brightness) {
    final scheme = ColorScheme.fromSeed(
      seedColor: _seed,
      brightness: brightness,
    );

    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: scheme.surface,
      appBarTheme: AppBarTheme(
        centerTitle: false,
        backgroundColor: scheme.surface,
        foregroundColor: scheme.onSurface,
        elevation: 0,
        scrolledUnderElevation: 2,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: scheme.surfaceContainerHighest.withValues(alpha: 0.4),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide(color: scheme.outlineVariant),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide(color: scheme.primary, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide(color: scheme.error),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide(color: scheme.error, width: 2),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          // Comfortable to hit while wearing work gloves.
          minimumSize: const Size.fromHeight(52),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
      // A card has no margin of its own: the list around it gives the page inset (16) and
      // the gap between cards (8), so every screen lines up the same way.
      cardTheme: CardThemeData(
        margin: EdgeInsets.zero,
        elevation: 0,
        color: scheme.surfaceContainerLow,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
        ),
      ),
      listTileTheme: const ListTileThemeData(
        contentPadding: EdgeInsets.symmetric(horizontal: 20, vertical: 4),
      ),
    );
  }
}
