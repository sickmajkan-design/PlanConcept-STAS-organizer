import 'package:construction_mobile/core/telemetry/client_error_reporter.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

/// The reporter's job is to be the least important thing running.
///
/// Everything here is about it staying out of the way. A crash reporter that
/// throws, hangs, or floods is worse than none at all, because it also looks
/// like it is helping — and it does its damage at the exact moment the app is
/// already in trouble.
void main() {
  late List<RequestOptions> sent;
  late ClientErrorReporter reporter;

  /// A Dio that records what it was asked to send and answers 202, without a
  /// socket anywhere.
  ClientErrorReporter reporterThatRecords({Object? failWith}) {
    final dio = Dio(BaseOptions(baseUrl: 'http://api.test'));

    dio.httpClientAdapter = _RecordingAdapter(
      onRequest: sent.add,
      failWith: failWith,
    );

    return ClientErrorReporter(client: dio);
  }

  setUp(() {
    sent = [];
    reporter = reporterThatRecords();
  });

  test('sends the message, the kind and the stack', () async {
    await reporter.report(
      StateError('the clock-in screen has no shift'),
      stackTrace: StackTrace.fromString('#0 ClockInScreen.build'),
      screen: 'clock-in',
    );

    expect(sent, hasLength(1));

    final body = sent.single.data as Map<String, dynamic>;

    expect(sent.single.path, '/api/v1/client-errors');
    expect(body['app'], 'mobile');
    expect(body['kind'], 'StateError');
    expect(body['message'], contains('no shift'));
    expect(body['stack'], contains('ClockInScreen'));
    expect(body['route'], 'clock-in');
  });

  /// A widget that throws in `build` throws on every frame — sixty times a
  /// second, with the same stack. On a phone that is battery and data.
  test('the same fault is sent once, not once per frame', () async {
    for (var i = 0; i < 30; i++) {
      await reporter.report(StateError('the same broken widget'));
    }

    expect(sent, hasLength(1));
  });

  test('stops after ten reports in one launch', () async {
    for (var i = 0; i < 25; i++) {
      // Distinct each time, so de-duplication is not what stops it.
      await reporter.report(StateError('failure number $i'));
    }

    expect(sent, hasLength(ClientErrorReporter.maxPerLaunch));
  });

  /// The loop worth preventing: the API is the thing that is broken, so the
  /// report fails, and whatever catches that failure reports it.
  test('never throws when the report itself fails', () async {
    final failing = reporterThatRecords(
      failWith: DioException(
        requestOptions: RequestOptions(path: '/api/v1/client-errors'),
        type: DioExceptionType.connectionError,
      ),
    );

    await expectLater(
      failing.report(StateError('anything at all')),
      completes,
    );
  });

  test('a runaway stack is truncated rather than sent whole', () async {
    await reporter.report(
      StateError('recursed'),
      stackTrace: StackTrace.fromString('#0 loop\n' * 5000),
    );

    final stack = (sent.single.data as Map<String, dynamic>)['stack'] as String;

    // Under the API's own 10,000 limit, which would otherwise refuse the
    // report and lose it completely.
    expect(stack.length, lessThan(10000));
    expect(stack, contains('truncated'));
  });

  /// No Authorization header, and that is deliberate: the crash most worth
  /// hearing about is the one on a sign-in screen that will not load.
  test('reports without a token', () async {
    await reporter.report(StateError('before anybody signed in'));

    expect(sent.single.headers.containsKey('Authorization'), isFalse);
  });
}

/// Answers every request without a network, recording what was asked.
class _RecordingAdapter implements HttpClientAdapter {
  _RecordingAdapter({required this.onRequest, this.failWith});

  final void Function(RequestOptions) onRequest;
  final Object? failWith;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<List<int>>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    onRequest(options);

    if (failWith != null) {
      throw failWith!;
    }

    return ResponseBody.fromString('', 202);
  }

  @override
  void close({bool force = false}) {}
}
