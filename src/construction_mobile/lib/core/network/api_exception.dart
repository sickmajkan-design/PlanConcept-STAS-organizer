import 'package:dio/dio.dart';

/// Application-facing error type. Translates the API's RFC 7807
/// problem-details responses (and transport failures) into something the UI
/// can show directly.
class ApiException implements Exception {
  ApiException(
    this.message, {
    this.statusCode,
    this.fieldErrors = const {},
  });

  final String message;
  final int? statusCode;

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
        );
      case DioExceptionType.connectionError:
        return ApiException(
          'No connection to the server. Check your network and try again.',
        );
      case DioExceptionType.cancel:
        return ApiException('The request was cancelled.');
      case DioExceptionType.badCertificate:
        return ApiException('The server certificate could not be verified.');
      case DioExceptionType.badResponse:
      case DioExceptionType.unknown:
        return ApiException._fromResponse(exception);
    }
  }

  factory ApiException._fromResponse(DioException exception) {
    final response = exception.response;

    if (response == null) {
      return ApiException('Something went wrong. Please try again.');
    }

    final data = response.data;
    final statusCode = response.statusCode;

    if (data is Map) {
      final fieldErrors = _parseFieldErrors(data['errors']);

      final message = fieldErrors.isNotEmpty
          ? fieldErrors.values.first.first
          : (data['detail'] as String?) ??
              (data['title'] as String?) ??
              _defaultMessageFor(statusCode);

      return ApiException(
        message,
        statusCode: statusCode,
        fieldErrors: fieldErrors,
      );
    }

    return ApiException(_defaultMessageFor(statusCode), statusCode: statusCode);
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
