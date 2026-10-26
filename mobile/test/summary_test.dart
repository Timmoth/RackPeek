import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rackpeek_mobile/data/api_client.dart';
import 'package:rackpeek_mobile/design.dart';
import 'package:rackpeek_mobile/screens/resources_tab.dart';
import 'package:rackpeek_mobile/screens/summary_tab.dart';
import 'package:rackpeek_mobile/state/inventory_store.dart';

void main() {
  testWidgets('hardware summary item opens its resource kind filter', (
    tester,
  ) async {
    final store =
        InventoryStore(
            RackPeekApi(
              baseUrl: 'https://server.example.test',
              apiKey: 'test-key',
            ),
          )
          ..summary = {
            'hardware': {
              'totalHardware': 1,
              'hardwareByKind': {'Server': 1},
            },
            'systems': {
              'totalSystems': 0,
              'systemsByType': <String, int>{},
              'systemsByOs': <String, int>{},
            },
            'services': {'totalServices': 0, 'totalIpAddresses': 0},
            'tags': <String, int>{},
            'labels': <String, int>{},
          };
    ResourceFilter? selectedFilter;

    await tester.pumpWidget(
      MaterialApp(
        theme: scribeTheme(),
        home: Scaffold(
          body: SummaryTab(
            store: store,
            onOpenResources: (filter) => selectedFilter = filter,
          ),
        ),
      ),
    );

    await tester.tap(find.text('1 Servers'));
    expect(selectedFilter?.kind, 'Server');
  });
}
