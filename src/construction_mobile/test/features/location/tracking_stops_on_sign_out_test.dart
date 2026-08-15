import 'dart:async';

import 'package:construction_mobile/core/network/network_providers.dart';
import 'package:construction_mobile/features/auth/data/models/user.dart';
import 'package:construction_mobile/features/auth/presentation/auth_controller.dart';
import 'package:construction_mobile/features/location/data/location_queue.dart';
import 'package:construction_mobile/features/location/presentation/location_tracking_controller.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:geolocator/geolocator.dart';

/// Signing out has to stop the tracking, including mid-start.
///
/// Starting is a sequence of awaits: restoring the queue, asking the platform
/// for permission, loading the wording of the Android notification. Signing out
/// during any of them throws this controller away — and the subscription
/// opened a moment later belongs to an object nothing holds a reference to,
/// so nothing can ever cancel it.
///
/// On Android that is not an abstract leak. Reporting runs as a foreground
/// service, so what survives is a permanent notification in the status bar, a
/// wake lock and a GPS fix taken every ten metres, all for somebody who signed
/// out — with the app showing the sign-in screen and no way to make it stop
/// short of force-quitting.
class _FakeGeolocator extends GeolocatorPlatform {
  /// Held open so the test can sign out while the start is inside it.
  final Completer<LocationPermission> permission =
      Completer<LocationPermission>();

  int streamsOpened = 0;

  @override
  Future<bool> isLocationServiceEnabled() async => true;

  @override
  Future<LocationPermission> checkPermission() => permission.future;

  @override
  Future<LocationPermission> requestPermission() => permission.future;

  @override
  Stream<Position> getPositionStream({LocationSettings? locationSettings}) {
    streamsOpened++;
    return const Stream<Position>.empty();
  }
}

/// Empty storage, so the locale the notification is worded in resolves
/// immediately instead of waiting on a platform channel that is not here.
/// Without it the start stalls before it would ever open a stream, and the
/// assertion below would pass without meaning anything.
class _EmptyKeystore implements FlutterSecureStorage {
  @override
  dynamic noSuchMethod(Invocation invocation) {
    return switch (invocation.memberName) {
      #read => Future<String?>.value(),
      _ => Future<void>.value(),
    };
  }
}

class _MemoryQueueStore implements LocationQueueStore {
  String? value;

  @override
  Future<String?> read() async => value;

  @override
  Future<void> write(String written) async => value = written;

  @override
  Future<void> clear() async => value = null;
}

const _employee = User(
  id: '019fad65-d635-76f2-880f-d8d25aea67d0',
  email: 'ivan@construction.local',
  role: 'Foreman',
  employeeId: '019fad73-e894-791b-a6c3-715bddf61164',
  firstName: 'Ivan',
  lastName: 'Horvat',
);

/// Stands in for the auth controller: the only thing the tracking controller
/// wants from it is who is signed in, and null is signing out.
class _SignedIn extends Notifier<User?> {
  @override
  User? build() => _employee;

  void signOut() => state = null;
}

final _signedInProvider = NotifierProvider<_SignedIn, User?>(_SignedIn.new);

void main() {
  // `AppLocalizations.delegate.load` needs the binding, and the tracking
  // controller loads it to word the Android notification.
  TestWidgetsFlutterBinding.ensureInitialized();

  test('a sign-out during start never opens the stream', () async {
    final geolocator = _FakeGeolocator();
    GeolocatorPlatform.instance = geolocator;

    final container = ProviderContainer(
      overrides: [
        currentUserProvider.overrideWith((ref) => ref.watch(_signedInProvider)),
        locationQueueProvider.overrideWithValue(
          LocationQueue(_MemoryQueueStore()),
        ),
        secureStorageProvider.overrideWithValue(_EmptyKeystore()),
      ],
    );
    addTearDown(container.dispose);

    // Starting, and now waiting on the permission check.
    expect(
      container.read(locationTrackingProvider).status,
      LocationTrackingStatus.starting,
    );
    await Future<void>.delayed(Duration.zero);

    // Sign out while it is in there.
    container.read(_signedInProvider.notifier).signOut();
    container.read(locationTrackingProvider);

    // The permission the operator granted arrives after they have gone.
    geolocator.permission.complete(LocationPermission.always);

    // Generously more turns than the rest of the start needs, so that a stream
    // this test is meant to catch has every chance to be opened.
    for (var i = 0; i < 20; i++) {
      await Future<void>.delayed(Duration.zero);
    }

    expect(
      geolocator.streamsOpened,
      0,
      reason: 'a stream opened here is one nothing is left to cancel',
    );
    expect(
      container.read(locationTrackingProvider).status,
      LocationTrackingStatus.off,
    );
  });

  test('tracking is off for somebody who is not signed in', () async {
    GeolocatorPlatform.instance = _FakeGeolocator();

    final container = ProviderContainer(
      overrides: [
        currentUserProvider.overrideWithValue(null),
        locationQueueProvider.overrideWithValue(
          LocationQueue(_MemoryQueueStore()),
        ),
      ],
    );
    addTearDown(container.dispose);

    expect(
      container.read(locationTrackingProvider).status,
      LocationTrackingStatus.off,
    );
  });
}
