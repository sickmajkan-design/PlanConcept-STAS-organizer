import 'package:construction_mobile/features/materials/presentation/materials_screen.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('formatQuantity', () {
    test('trims trailing zeros', () {
      expect(formatQuantity(12.5), '12.5');
      expect(formatQuantity(40), '40');
      expect(formatQuantity(0.25), '0.25');
    });

    test('keeps significant decimals up to three places', () {
      expect(formatQuantity(1.125), '1.125');
    });
  });
}
