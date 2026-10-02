import 'package:construction_mobile/features/notifications/data/models/app_notification.dart';
import 'package:construction_mobile/features/notifications/presentation/notification_text.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// A person is told when the business unit that employs them changes, in their own language.
void main() {
  AppNotification notice(String dataJson) => AppNotification(
        id: '1',
        type: 'EmployeeBranchChanged',
        title: 'Business unit changed',
        body: 'You are employed in X from 01.04.2026.',
        dataJson: dataJson,
        createdAt: DateTime.utc(2026, 10, 2),
      );

  test('Serbian names the unit and the day', () async {
    final l10n = await AppLocalizations.delegate.load(const Locale('sr'));

    final text = resolveNotificationText(l10n, notice('{"branchName":"Plan Concept Beograd","startDate":"2026-04-01"}'));

    expect(text.title, 'Promjena poslovne jedinice');
    expect(text.body, 'Zaposleni ste u jedinici Plan Concept Beograd od 01.04.2026.');
  });

  test('leaving every unit names none', () async {
    final l10n = await AppLocalizations.delegate.load(const Locale('sr'));

    final text = resolveNotificationText(l10n, notice('{"startDate":"2026-09-01"}'));

    expect(text.body, contains('01.09.2026.'));
    expect(text.body, isNot(contains('jedinici')));
  });

  test('English reads the same facts', () async {
    final l10n = await AppLocalizations.delegate.load(const Locale('en'));

    final text = resolveNotificationText(l10n, notice('{"branchName":"Berlin","startDate":"2026-04-01"}'));

    expect(text.title, 'Business unit changed');
    expect(text.body, contains('Berlin'));
  });

  test('without the date the server\'s own sentence is shown', () async {
    final l10n = await AppLocalizations.delegate.load(const Locale('sr'));

    final text = resolveNotificationText(l10n, notice('{"branchName":"Berlin"}'));

    expect(text.title, 'Business unit changed');
  });
}
