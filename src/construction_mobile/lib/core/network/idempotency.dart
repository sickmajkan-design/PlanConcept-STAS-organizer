import 'dart:math';

/// The header the API reads to tell a retry from a second action.
const idempotencyHeader = 'Idempotency-Key';

final _random = Random.secure();

/// A fresh key, naming one attempt at one action.
///
/// Sixteen random bytes as hex — 32 characters, comfortably inside the API's
/// 8-to-128 limit. `Random.secure` rather than the default generator not for
/// secrecy but for collision resistance: the default is seeded from the clock,
/// and two phones on the same site starting the same screen in the same
/// millisecond is a thing that happens at the start of a shift.
///
/// A key is generated when the operator begins an action and kept until it
/// succeeds. Minting one per request would defeat the whole point — the
/// second attempt would look like a second action, which is exactly what the
/// server is being asked to distinguish.
String newIdempotencyKey() {
  final bytes = List<int>.generate(16, (_) => _random.nextInt(256));

  return bytes.map((byte) => byte.toRadixString(16).padLeft(2, '0')).join();
}
