import 'package:flutter_riverpod/flutter_riverpod.dart';

/// The Firebase token this handset is currently registered with, or null.
///
/// It sits on its own, outside [PushController], because of which way the
/// dependencies run. The push controller watches who is signed in — it has to,
/// since a device token is registered against a person. Signing out needs that
/// token, to have the API forget it while the session is still valid.
///
/// Reading it out of the push controller is therefore not an option: the auth
/// controller would depend on the provider that depends on the auth
/// controller, and Riverpod refuses that with a `CircularDependencyError`. It
/// refused it in production, which is how this file came to exist — sign-out
/// threw on its first line, before revoking anything and before clearing the
/// session, so pressing the button did nothing at all and the operator stayed
/// signed in. Nothing caught it, because the thrown error went to an `await`
/// nobody was watching.
///
/// A token is a fact about the handset rather than about the person holding
/// it, so a provider that depends on nothing is also where it belongs.
class DeviceTokenNotifier extends Notifier<String?> {
  @override
  String? build() => null;

  void remember(String token) => state = token;

  void forget() => state = null;
}

final deviceTokenProvider = NotifierProvider<DeviceTokenNotifier, String?>(
  DeviceTokenNotifier.new,
);
