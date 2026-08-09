import 'package:flutter/material.dart';

import '../../l10n/app_localizations.dart';
import '../network/api_exception.dart';

/// Turns a failure into a sentence, at the point where the language is known.
///
/// The same split as `AppMessage`: the network layer records what happened,
/// the widget layer says it. Doing it here rather than at construction also
/// means a failure already sitting in a controller's state re-reads in the new
/// language when the operator switches it, instead of keeping the language it
/// failed in.
extension ApiFailureText on ApiException {
  /// What to put in front of the operator.
  ///
  /// The server's own wording wins when there is any: "Employee number EMP-001
  /// is already in use" tells them what to do, and a translated generality
  /// does not. It arrives in English — the API is not localised — which is a
  /// real gap, but it is the backend's gap, and replacing a specific English
  /// sentence with a vague Serbian one would be a step backwards.
  String describe(AppLocalizations l10n) =>
      isFromServer ? message : kind.describe(l10n);
}

extension ApiFailureKindText on ApiFailureKind {
  String describe(AppLocalizations l10n) => switch (this) {
        ApiFailureKind.offline => l10n.failureOffline,
        ApiFailureKind.timeout => l10n.failureTimeout,
        ApiFailureKind.cancelled => l10n.failureCancelled,
        ApiFailureKind.certificate => l10n.failureCertificate,
        ApiFailureKind.badRequest => l10n.failureBadRequest,
        ApiFailureKind.unauthorized => l10n.failureUnauthorized,
        ApiFailureKind.forbidden => l10n.failureForbidden,
        ApiFailureKind.notFound => l10n.failureNotFound,
        ApiFailureKind.conflict => l10n.failureConflict,
        ApiFailureKind.server => l10n.failureServer,
        ApiFailureKind.unknown => l10n.failureUnknown,
      };

  /// The picture that goes with it.
  ///
  /// Being offline is the one an operator should be able to recognise across
  /// the cab of a truck without reading: it is the commonest state on a site
  /// and the only one where the answer is "walk twenty metres", not "call
  /// someone".
  IconData get icon => switch (this) {
        ApiFailureKind.offline || ApiFailureKind.timeout => Icons.cloud_off,
        ApiFailureKind.forbidden ||
        ApiFailureKind.unauthorized =>
          Icons.lock_outline,
        ApiFailureKind.notFound => Icons.search_off,
        _ => Icons.error_outline,
      };

  /// Whether offering "try again" is honest.
  ///
  /// It is not, for a permission failure: the same request will be refused
  /// again, and a button that does nothing twice is worse than no button.
  bool get isRetryable => switch (this) {
        ApiFailureKind.forbidden || ApiFailureKind.notFound => false,
        _ => true,
      };
}
