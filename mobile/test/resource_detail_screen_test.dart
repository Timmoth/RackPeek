import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rackpeek_mobile/data/api_client.dart';
import 'package:rackpeek_mobile/data/settings_store.dart';
import 'package:rackpeek_mobile/models/inventory.dart';
import 'package:rackpeek_mobile/screens/resource_detail_screen.dart';
import 'package:rackpeek_mobile/state/inventory_store.dart';

import 'support/memory_storage.dart';

void main() {
  testWidgets('service details show every published endpoint', (tester) async {
    final store = InventoryStore(
      RackPeekApi(baseUrl: 'https://server.example.test', apiKey: 'test-key'),
    );
    store.document = InventoryDocument(
      version: 5,
      resources: [
        Resource.fromJson({
          'kind': 'Service',
          'name': 'media',
          'network': {'ip': '192.168.1.10', 'port': 8096, 'protocol': 'TCP'},
          'endpoints': [
            {'ip': '192.168.1.10', 'port': 8096, 'protocol': 'TCP'},
            {'ip': '192.168.1.11', 'port': 8920, 'protocol': 'UDP'},
          ],
        }),
      ],
      connections: [],
    );

    await tester.pumpWidget(
      MaterialApp(
        home: ResourceDetailScreen(store: store, name: 'media'),
      ),
    );

    expect(
      find.text('Published endpoint: 192.168.1.10:8096 (TCP)'),
      findsOneWidget,
    );
    expect(
      find.text('Published endpoint: 192.168.1.11:8920 (UDP)'),
      findsOneWidget,
    );

    store.dispose();
  });

  testWidgets('details refresh immediately after an offline edit', (
    tester,
  ) async {
    final store = InventoryStore(
      null,
      settings: SettingsStore(storage: MemoryKeyValueStorage()),
    );
    store.document = InventoryDocument.fromJson({
      'version': 5,
      'resources': [
        {'kind': 'Server', 'name': 'nas01'},
      ],
      'connections': [],
    });

    await tester.pumpWidget(
      MaterialApp(
        home: ResourceDetailScreen(store: store, name: 'nas01'),
      ),
    );

    await store.applyPatch({
      'kind': 'Server',
      'name': 'nas01',
      'notes': 'Saved offline',
    });
    await tester.pump();

    expect(find.text('Saved offline'), findsOneWidget);
    store.dispose();
  });
}
