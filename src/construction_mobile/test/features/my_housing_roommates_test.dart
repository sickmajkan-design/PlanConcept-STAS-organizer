import 'package:construction_mobile/features/housing/data/my_housing.dart';
import 'package:flutter_test/flutter_test.dart';

/// Someone newly housed can call the people who already live there. An older server sends only
/// the names, and that still has to read.
void main() {
  Map<String, dynamic> housing(Map<String, dynamic> extra) => {
        'name': 'Stan 4',
        'address': 'Ulica 1',
        'startDate': '2026-10-05',
        'upcoming': false,
        'roommates': ['Ana Ilić'],
        ...extra,
      };

  test('reads the roommates with their phone numbers', () {
    final parsed = MyHousing.fromJson(housing({
      'roommateContacts': [
        {'name': 'Ana Ilić', 'phone': '+387 61 111 222'},
        {'name': 'Boris Kovač', 'phone': null},
      ],
    }));

    expect(parsed.roommateContacts.first.phone, '+387 61 111 222');
    expect(parsed.roommateContacts.last.phone, isNull);
  });

  test('a server that only sends names still works', () {
    final parsed = MyHousing.fromJson(housing({}));

    expect(parsed.roommates, ['Ana Ilić']);
    expect(parsed.roommateContacts, isEmpty);
  });
}
