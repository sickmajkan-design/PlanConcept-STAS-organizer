import 'package:construction_mobile/core/config/server_address.dart';
import 'package:flutter_test/flutter_test.dart';

/// What the app will accept as the address of its server.
///
/// This is typed on a phone, by somebody standing on a site, probably once.
/// A wrong address does not announce itself — every request simply times out,
/// which on a construction site looks exactly like no signal, and no signal is
/// the last thing anyone would think to question. So the rules are strict and
/// the refusal is immediate, at the moment the mistake was made.
void main() {
  group('normalize', () {
    test('accepts a plain https address', () {
      expect(
        ServerAddress.normalize('https://organizer.example.com'),
        'https://organizer.example.com',
      );
    });

    test('accepts http with a port, which is what a laptop on the Wi-Fi is', () {
      expect(
        ServerAddress.normalize('http://192.168.1.20:5000'),
        'http://192.168.1.20:5000',
      );
    });

    /// Dio joins this to paths that already start with `/`, so a trailing
    /// slash produces `//api/v1/...` — which some proxies route and some
    /// answer 404 to, a difference nobody wants to debug from a site.
    test('drops a trailing slash', () {
      expect(
        ServerAddress.normalize('https://organizer.example.com/'),
        'https://organizer.example.com',
      );
    });

    test('trims what a keyboard adds', () {
      expect(
        ServerAddress.normalize('  https://organizer.example.com  '),
        'https://organizer.example.com',
      );
    });

    test('refuses an address with no scheme', () {
      // The likeliest mistake of all: typing what you would type into a
      // browser, which fills the scheme in for you.
      expect(ServerAddress.normalize('organizer.example.com'), isNull);
    });

    test('refuses a scheme that is not http or https', () {
      expect(ServerAddress.normalize('ftp://organizer.example.com'), isNull);
      expect(ServerAddress.normalize('javascript:alert(1)'), isNull);
    });

    test('refuses empty and blank', () {
      expect(ServerAddress.normalize(null), isNull);
      expect(ServerAddress.normalize(''), isNull);
      expect(ServerAddress.normalize('   '), isNull);
    });

    test('refuses a scheme with no host', () {
      expect(ServerAddress.normalize('https://'), isNull);
    });
  });
}
