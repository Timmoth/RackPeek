import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:rackpeek_mobile/data/settings_store.dart';
import 'package:rackpeek_mobile/screens/connection_screen.dart';

import 'support/memory_storage.dart';

void main() {
  testWidgets('API key field can be shown and hidden', (tester) async {
    await tester.pumpWidget(
      MaterialApp(home: ConnectionScreen(settings: SettingsStore())),
    );

    final apiKeyField = find.byType(TextField).at(1);
    expect(tester.widget<TextField>(apiKeyField).obscureText, isTrue);

    await tester.tap(find.byTooltip('Show API key'));
    await tester.pump();
    expect(tester.widget<TextField>(apiKeyField).obscureText, isFalse);

    await tester.tap(find.byTooltip('Hide API key'));
    await tester.pump();
    expect(tester.widget<TextField>(apiKeyField).obscureText, isTrue);
  });

  testWidgets('skip opens a locally stored inventory without a server', (
    tester,
  ) async {
    final settings = SettingsStore(storage: MemoryKeyValueStorage());
    await tester.pumpWidget(
      MaterialApp(home: ConnectionScreen(settings: settings)),
    );

    await tester.tap(find.text('Skip and use offline'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));

    expect(
      find.text('Offline mode · changes stay on this device'),
      findsNothing,
    );
    expect(find.text('Infrastructure'), findsOneWidget);
    expect(await settings.isLocalMode(), isTrue);
  });

  for (final upload in [true, false]) {
    testWidgets(
      upload
          ? 'connecting offers to upload local changes'
          : 'connecting offers to discard local changes',
      (tester) async {
        final settings = SettingsStore(storage: MemoryKeyValueStorage());
        await settings.saveOfflineState({
          'document': {
            'version': 5,
            'resources': [
              {'kind': 'Server', 'name': 'nas01', 'model': 'Local NAS'},
            ],
            'connections': [],
          },
          'pendingChanges': [
            {'kind': 'Server', 'name': 'nas01', 'model': 'Local NAS'},
          ],
        });
        final posted = <Map<String, dynamic>>[];
        final serverDocument = <String, dynamic>{
          'version': 5,
          'resources': <Map<String, dynamic>>[
            {'kind': 'Server', 'name': 'server-copy', 'model': 'Remote NAS'},
          ],
          'connections': [],
        };
        final client = MockClient((request) async {
          if (request.url.path == '/api/status') {
            return http.Response(
              jsonEncode({'apiVersion': '1', 'appVersion': 'test'}),
              200,
            );
          }
          if (request.method == 'POST' &&
              request.url.path == '/api/inventory') {
            final payload = jsonDecode(request.body) as Map<String, dynamic>;
            posted.add(payload);
            final document = payload['json'] as Map<String, dynamic>;
            (serverDocument['resources'] as List).addAll(
              (document['resources'] as List).cast<Map<String, dynamic>>(),
            );
            return http.Response('{}', 200);
          }
          if (request.url.path == '/api/inventory/summary') {
            return http.Response(
              jsonEncode({
                'hardware': {'totalHardware': 0, 'hardwareByKind': {}},
                'systems': {
                  'totalSystems': 0,
                  'systemsByType': {},
                  'systemsByOs': {},
                },
                'services': {'totalServices': 0, 'totalIpAddresses': 0},
                'tags': {},
                'labels': {},
              }),
              200,
            );
          }
          if (request.url.path == '/api/inventory') {
            return http.Response(jsonEncode(serverDocument), 200);
          }
          return http.Response('{}', 404);
        });

        await tester.pumpWidget(
          MaterialApp(
            home: ConnectionScreen(settings: settings, httpClient: client),
          ),
        );
        await tester.enterText(
          find.byType(TextField).first,
          'http://rackpeek.test',
        );
        await tester.enterText(find.byType(TextField).at(1), 'test-key');
        await tester.tap(find.text('Connect'));
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 500));

        expect(find.text('Local changes found'), findsOneWidget);
        await tester.tap(
          find.text(upload ? 'Upload to server' : 'Discard local changes'),
        );
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 500));

        expect(posted.length, upload ? 1 : 0);
        final offlineState = await settings.loadOfflineState();
        expect(offlineState!['pendingChanges'], isEmpty);
        final resources = offlineState['document']['resources'] as List;
        expect(
          resources.any(
            (resource) =>
                resource['name'] == (upload ? 'nas01' : 'server-copy'),
          ),
          isTrue,
        );
        expect((await settings.load())!.baseUrl, 'http://rackpeek.test');

        await tester.pumpWidget(const SizedBox.shrink());
        client.close();
      },
    );
  }
}
