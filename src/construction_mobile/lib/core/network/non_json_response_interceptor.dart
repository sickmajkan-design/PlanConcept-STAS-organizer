import 'package:dio/dio.dart';

/// Turns "a web page came back instead of the API" into "no connection".
///
/// A site wireless network is often connected and yet leads nowhere: it holds
/// every request until somebody accepts its terms, and meanwhile answers them
/// with a login page — usually after a redirect, so the status is a cheerful
/// 200. The phone reports itself online, Dio reports success, and the app then
/// fails on the first line that expects an object, with an error that says
/// nothing about the network.
///
/// The API only ever answers in JSON, so anything else on a call that asked
/// for JSON is not from the API. Rejecting it as a connection error is what
/// lets everything already built for "no signal" — the offline cache, the
/// clock and outbox queues, the offline wording — apply to this state too,
/// without any of them having to know it exists.
class NonJsonResponseInterceptor extends Interceptor {
  const NonJsonResponseInterceptor();

  @override
  void onResponse(Response<dynamic> response, ResponseInterceptorHandler handler) {
    if (_isForeignPage(response)) {
      return handler.reject(
        DioException(
          requestOptions: response.requestOptions,
          response: response,
          type: DioExceptionType.connectionError,
          message: 'Received a web page instead of an API response '
              '(captive portal or proxy).',
        ),
        // Not resolved onwards: later interceptors see an error, so the cache
        // interceptor can answer from its copy exactly as it does offline.
        true,
      );
    }

    handler.next(response);
  }

  static bool _isForeignPage(Response<dynamic> response) {
    if (response.requestOptions.responseType != ResponseType.json) {
      return false;
    }

    final data = response.data;

    if (data is! String || data.trim().isEmpty) {
      return false;
    }

    final contentType = response.headers.value(Headers.contentTypeHeader) ?? '';

    return !contentType.toLowerCase().contains('json');
  }
}
