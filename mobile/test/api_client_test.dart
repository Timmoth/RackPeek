import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:rackpeek_mobile/data/api_client.dart';

void main() {
  group('serverUrlCandidates', () {
    test('infers HTTPS first and HTTP as a fallback', () {
      expect(serverUrlCandidates('192.168.1.10:8080'), [
        'https://192.168.1.10:8080',
        'http://192.168.1.10:8080',
      ]);
    });

    test('preserves an explicitly supplied scheme', () {
      expect(serverUrlCandidates('http://server.local:8080'), [
        'http://server.local:8080',
      ]);
    });

    test('rejects malformed or non-HTTP addresses', () {
      expect(serverUrlCandidates(''), isEmpty);
      expect(serverUrlCandidates('ftp://server.local'), isEmpty);
      expect(serverUrlCandidates('https://'), isEmpty);
    });
  });

  test(
    'status request reads API and app versions without sending an API key',
    () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/status');
        expect(request.headers.containsKey('X-Api-Key'), isFalse);
        return http.Response(
          jsonEncode({'apiVersion': '1', 'appVersion': 'v2.2.0'}),
          200,
        );
      });

      final status = await RackPeekApi(
        baseUrl: 'https://server.example.test',
        apiKey: '',
        client: client,
      ).getStatus();

      expect(status.apiVersion, '1');
      expect(status.appVersion, 'v2.2.0');
    },
  );
}
