import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:rackpeek_mobile/data/api_client.dart';
import 'package:rackpeek_mobile/data/settings_store.dart';
import 'package:rackpeek_mobile/state/inventory_store.dart';

import 'support/memory_storage.dart';

void main() {
  test(
    'offline edits persist across store restarts and preserve unknown fields',
    () async {
      final storage = MemoryKeyValueStorage();
      final settings = SettingsStore(storage: storage);
      await settings.saveOfflineState({
        'document': {
          'version': 5,
          'resources': [
            {
              'kind': 'Service',
              'name': 'media',
              'customField': {'owner': 'lab'},
              'network': {
                'ip': '192.168.1.20',
                'port': 8096,
                'protocol': 'TCP',
              },
            },
          ],
          'connections': [],
        },
        'pendingChanges': <Map<String, dynamic>>[],
      });

      final first = InventoryStore(null, settings: settings);
      await first.refresh();
      await first.applyPatch({
        'kind': 'Service',
        'name': 'media',
        'network': {'port': 8920},
      });
      expect(first.document!.byName('media')!.network!.ip, '192.168.1.20');
      expect(
        first.document!.toJson()['resources'][0]['customField']['owner'],
        'lab',
      );
      expect(first.pendingChangeCount, 1);
      first.dispose();

      final reopened = InventoryStore(null, settings: settings);
      await reopened.refresh();
      expect(reopened.document!.byName('media')!.network!.port, 8920);
      expect(reopened.pendingChangeCount, 1);
      reopened.dispose();
    },
  );

  test(
    'a pending offline change uploads when the configured server returns',
    () async {
      final storage = MemoryKeyValueStorage();
      final settings = SettingsStore(storage: storage);
      await settings.saveOfflineState({
        'document': {
          'version': 5,
          'resources': [
            {'kind': 'Server', 'name': 'nas01'},
          ],
          'connections': [],
        },
        'pendingChanges': [
          {'kind': 'Server', 'name': 'nas01', 'model': 'NAS'},
        ],
      });
      var uploaded = false;
      final client = MockClient((request) async {
        if (request.method == 'POST' && request.url.path == '/api/inventory') {
          uploaded = true;
          return http.Response('{}', 200);
        }
        if (request.url.path == '/api/inventory') {
          return http.Response(
            jsonEncode({
              'version': 5,
              'resources': [
                {'kind': 'Server', 'name': 'nas01', 'model': 'NAS'},
              ],
              'connections': [],
            }),
            200,
          );
        }
        if (request.url.path == '/api/inventory/summary') {
          return http.Response('{}', 200);
        }
        return http.Response('{}', 404);
      });

      final store = InventoryStore(
        RackPeekApi(
          baseUrl: 'https://server.example.test',
          apiKey: 'key',
          client: client,
        ),
        settings: settings,
      );
      await store.refresh();

      expect(uploaded, isTrue);
      expect(store.isOnline, isTrue);
      expect(store.hasPendingChanges, isFalse);
      expect(store.document!.byName('nas01')!.model, 'NAS');
      store.dispose();
    },
  );

  test('a failed upload keeps local edits and refresh retries them', () async {
    final storage = MemoryKeyValueStorage();
    final settings = SettingsStore(storage: storage);
    var failFirstPost = true;
    var postCount = 0;
    final client = MockClient((request) async {
      if (request.method == 'POST') {
        postCount++;
        if (failFirstPost) {
          failFirstPost = false;
          return http.Response('{"error":"offline"}', 503);
        }
        return http.Response('{}', 200);
      }
      if (request.url.path == '/api/inventory') {
        return http.Response(
          jsonEncode({
            'version': 5,
            'resources': [
              {'kind': 'Server', 'name': 'nas01', 'model': 'NAS'},
            ],
            'connections': [],
          }),
          200,
        );
      }
      return http.Response('{}', 200);
    });
    final store = InventoryStore(
      RackPeekApi(
        baseUrl: 'https://server.example.test',
        apiKey: 'key',
        client: client,
      ),
      settings: settings,
    );

    await store.refresh();
    await store.addResource({
      'kind': 'Server',
      'name': 'nas01',
      'model': 'NAS',
    });
    expect(store.pendingChangeCount, 1);
    expect(store.document!.byName('nas01')!.model, 'NAS');
    expect(store.isOnline, isFalse);

    await store.refresh();
    expect(postCount, 2);
    expect(store.pendingChangeCount, 0);
    expect(store.isOnline, isTrue);
    store.dispose();
  });
}
