import 'package:construction_mobile/features/notifications/data/models/app_notification.dart';
import 'package:construction_mobile/features/notifications/presentation/notification_text.dart';
import 'package:construction_mobile/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// A planner publishing a day, or reminding somebody to confirm it, sends the same kind of notice as a new posting;
/// the app tells the two apart by the data and speaks about the day, not about a "new project".
void main() {
  AppNotification notice(String dataJson) => AppNotification(
        id: '1',
        type: 'ProjectAssigned',
        title: 'Your schedule for 28.11.2026',
        body: 'On 28.11.2026 you are posted to Hala Brod.',
        dataJson: dataJson,
        createdAt: DateTime.utc(2026, 11, 27),
      );

  const published =
      '{"projectName":"Hala Brod","projectAddress":"Ulica 1, Brod","projectShiftStartTime":"07:00","date":"2026-11-28","published":"schedule"}';
  const reminder = '{"projectName":"Hala Brod","date":"2026-11-28","reminder":"schedule"}';
  const plain = '{"projectName":"Hala Brod","projectAddress":"Ulica 1, Brod"}';

  test('a published day names the day, the site, the address and the shift start in Serbian', () async {
    final l10n = await AppLocalizations.delegate.load(const Locale('sr'));

    final text = resolveNotificationText(l10n, notice(published));

    expect(text.title, 'Raspored za 28.11.2026.');
    expect(text.body, 'Za 28.11.2026. ste raspoređeni na gradilište "Hala Brod". Na adresi Ulica 1, Brod. Smjena počinje u 07:00.');
  });

  test('a reminder asks for confirmation and does not claim a new project', () async {
    final l10n = await AppLocalizations.delegate.load(const Locale('sr'));

    final text = resolveNotificationText(l10n, notice(reminder));

    expect(text.title, 'Potvrdite raspored');
    expect(text.body, 'Raspored za 28.11.2026. čeka vašu potvrdu: gradilište "Hala Brod".');
    expect(text.title, isNot(contains('novo gradilište')));
  });

  test('English reads the same facts', () async {
    final l10n = await AppLocalizations.delegate.load(const Locale('en'));

    final text = resolveNotificationText(l10n, notice(published));

    expect(text.title, 'Schedule for 28.11.2026.');
    expect(text.body, contains('Hala Brod'));
    expect(text.body, contains('Shift starts at 07:00.'));
  });

  test('a plain new posting still reads as a new project', () async {
    final l10n = await AppLocalizations.delegate.load(const Locale('sr'));

    final text = resolveNotificationText(l10n, notice(plain));

    expect(text.title, 'Dodijeljeni ste na novo gradilište');
    expect(text.body, contains('Hala Brod'));
  });
}
