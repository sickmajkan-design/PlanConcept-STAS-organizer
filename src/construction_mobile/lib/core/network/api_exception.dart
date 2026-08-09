import 'package:dio/dio.dart';

/// What went wrong, separately from the words used to say it.
///
/// The words cannot be decided here: this layer has no `BuildContext` and no
/// locale, so anything it writes is English in front of a Serbian foreman. The
/// kind travels instead, and `describe` in `core/l10n/api_failure_text.dart`
/// turns it into a sentence at the point where the language is known.
///
/// [offline] is the one worth separating from the rest. On a site with no
/// signal it is not an error at all in the way the others are — nothing is
/// broken, nothing needs reporting, and the only useful advice is to try again
/// somewhere else. A screen that says "something went wrong" to that is
/// alarming and useless in equal measure.
enum ApiFailureKind {
  /// The request never reached the server: no route, no DNS, aeroplane mode.
  offline,
  timeout,
  cancelled,
  certificate,
  badRequest,
  unauthorized,
  forbidden,
  notFound,
  conflict,
  server,
  unknown,
}

/// Application-facing error type. Translates the API's RFC 7807
/// problem-details responses (and transport failures) into something the UI
/// can show directly.
class ApiException implements Exception {
  ApiException(
    this.message, {
    this.statusCode,
    this.fieldErrors = const {},
    this.kind = ApiFailureKind.unknown,
    this.isFromServer = false,
  });

  /// English. Kept for logs, `toString` and anywhere without a locale — see
  /// [isFromServer] before showing it to anyone.
  final String message;
  final int? statusCode;

  final ApiFailureKind kind;

  /// Whether [message] is the server's own words rather than a default
  /// written here.
  ///
  /// It decides which text a screen shows: the server's `detail` is specific
  /// ("Employee number EMP-001 is already in use") and worth more than a
  /// translated generality, while a default written in this file is a
  /// generality that may as well be in the reader's language.
  final bool isFromServer;

  /// Field name -> messages, populated for 400 validation responses.
  final Map<String, List<String>> fieldErrors;

  bool get isValidationError => fieldErrors.isNotEmpty;

  bool get isUnauthorized => statusCode == 401;

  /// First message recorded for [field], if the server reported one.
  /// Field names are matched case-insensitively because the API reports
  /// them in PascalCase while the client uses camelCase.
  String? errorFor(String field) {
    for (final entry in fieldErrors.entries) {
      if (entry.key.toLowerCase() == field.toLowerCase()) {
        return entry.value.isEmpty ? null : entry.value.first;
      }
    }
    return null;
  }

  factory ApiException.fromDioException(DioException exception) {
    switch (exception.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.transformTimeout:
        return ApiException(
          'The server took too long to respond. Please try again.',
          kind: ApiFailureKind.timeout,
        );
      case DioExceptionType.connectionError:
        return ApiException(
          'No connection to the server. Check your network and try again.',
          kind: ApiFailureKind.offline,
        );
      case DioExceptionType.cancel:
        return ApiException(
          'The request was cancelled.',
          kind: ApiFailureKind.cancelled,
        );
      case DioExceptionType.badCertificate:
        return ApiException(
          'The server certificate could not be verified.',
          kind: ApiFailureKind.certificate,
        );
      case DioExceptionType.badResponse:
      case DioExceptionType.unknown:
        return ApiException._fromResponse(exception);
    }
  }

  factory ApiException._fromResponse(DioException exception) {
    final response = exception.response;

    if (response == null) {
      // `unknown` with no response is Dio's shape for a socket that failed
      // before any of it arrived, which on a phone is nearly always the
      // network rather than the server.
      return ApiException(
        'No connection to the server. Check your network and try again.',
        kind: ApiFailureKind.offline,
      );
    }

    final data = response.data;
    final statusCode = response.statusCode;

    if (data is Map) {
      final fieldErrors = _parseFieldErrors(data['errors']);

      // `detail` and a field error are written for this one request and say
      // something the reader can act on. `title` is a status phrase — "An
      // unexpected error occurred", "Conflict" — which is exactly the
      // generality the localised sentence replaces, so it does not count as
      // the server having said anything.
      final fromServer = fieldErrors.isNotEmpty || data['detail'] is String;

      final message = fieldErrors.isNotEmpty
          ? fieldErrors.values.first.first
          : (data['detail'] as String?) ??
              (data['title'] as String?) ??
              _defaultMessageFor(statusCode);

      return ApiException(
        message,
        statusCode: statusCode,
        fieldErrors: fieldErrors,
        kind: _kindFor(statusCode),
        isFromServer: fromServer,
      );
    }

    return ApiException(
      _defaultMessageFor(statusCode),
      statusCode: statusCode,
      kind: _kindFor(statusCode),
    );
  }

  static Map<String, List<String>> _parseFieldErrors(Object? errors) {
    if (errors is! Map) {
      return const {};
    }

    final parsed = <String, List<String>>{};

    errors.forEach((key, value) {
      if (value is List) {
        parsed['$key'] = value.map((message) => '$message').toList();
      } else if (value != null) {
        parsed['$key'] = ['$value'];
      }
    });

    return parsed;
  }

  static ApiFailureKind _kindFor(int? statusCode) {
    return switch (statusCode) {
      400 => ApiFailureKind.badRequest,
      401 => ApiFailureKind.unauthorized,
      403 => ApiFailureKind.forbidden,
      404 => ApiFailureKind.notFound,
      409 => ApiFailureKind.conflict,
      final int code when code >= 500 => ApiFailureKind.server,
      _ => ApiFailureKind.unknown,
    };
  }

  static String _defaultMessageFor(int? statusCode) {
    return switch (statusCode) {
      400 => 'The request was rejected. Please check the entered data.',
      401 => 'Your session has expired. Please sign in again.',
      403 => 'You do not have permission to perform this action.',
      404 => 'The requested item could not be found.',
      409 => 'The action conflicts with the current data.',
      final int code when code >= 500 =>
        'The server encountered an error. Please try again later.',
      _ => 'Something went wrong. Please try again.',
    };
  }

  @override
  String toString() => message;
}
