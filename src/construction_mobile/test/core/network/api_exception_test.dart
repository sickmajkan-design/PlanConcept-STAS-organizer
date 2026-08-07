import 'package:construction_mobile/core/network/api_exception.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

DioException _responseError(int statusCode, Object? body) {
  final options = RequestOptions(path: '/api/v1/auth/login');

  return DioException(
    requestOptions: options,
    type: DioExceptionType.badResponse,
    response: Response<Object?>(
      requestOptions: options,
      statusCode: statusCode,
      data: body,
    ),
  );
}

void main() {
  group('ApiException.fromDioException', () {
    test('extracts field errors from a validation problem-details body', () {
      final exception = ApiException.fromDioException(
        _responseError(400, {
          'title': 'One or more validation errors occurred.',
          'status': 400,
          'errors': {
            'Email': ['Email is not a valid email address.'],
            'Password': ['Password is required.'],
          },
        }),
      );

      expect(exception.isValidationError, isTrue);
      expect(exception.statusCode, 400);
      expect(exception.errorFor('email'), 'Email is not a valid email address.');
      // The API reports PascalCase field names; lookups are case-insensitive.
      expect(exception.errorFor('password'), 'Password is required.');
      expect(exception.errorFor('unknownField'), isNull);
    });

    test('uses the detail of a plain problem-details body', () {
      final exception = ApiException.fromDioException(
        _responseError(401, {
          'title': 'Unauthorized',
          'status': 401,
          'detail': 'Invalid email or password.',
        }),
      );

      expect(exception.message, 'Invalid email or password.');
      expect(exception.isUnauthorized, isTrue);
      expect(exception.isValidationError, isFalse);
    });

    test('falls back to a status-specific message for an unparsable body', () {
      final exception = ApiException.fromDioException(_responseError(409, 'nope'));

      expect(exception.statusCode, 409);
      expect(exception.message, 'The action conflicts with the current data.');
    });

    test('reports server errors for any 5xx status', () {
      final exception = ApiException.fromDioException(_responseError(503, null));

      expect(
        exception.message,
        'The server encountered an error. Please try again later.',
      );
    });

    test('explains a connection failure without leaking transport detail', () {
      final exception = ApiException.fromDioException(
        DioException(
          requestOptions: RequestOptions(path: '/api/v1/auth/login'),
          type: DioExceptionType.connectionError,
        ),
      );

      expect(
        exception.message,
        'No connection to the server. Check your network and try again.',
      );
      expect(exception.statusCode, isNull);
    });

    test('explains a timeout', () {
      final exception = ApiException.fromDioException(
        DioException(
          requestOptions: RequestOptions(path: '/api/v1/auth/me'),
          type: DioExceptionType.receiveTimeout,
        ),
      );

      expect(
        exception.message,
        'The server took too long to respond. Please try again.',
      );
    });
  });
}
