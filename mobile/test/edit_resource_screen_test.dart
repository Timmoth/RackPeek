import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rackpeek_mobile/data/api_client.dart';
import 'package:rackpeek_mobile/design.dart';
import 'package:rackpeek_mobile/models/inventory.dart';
import 'package:rackpeek_mobile/screens/edit_resource_screen.dart';
import 'package:rackpeek_mobile/state/inventory_store.dart';

void main() {
  testWidgets('edit title indicates pending changes and clears when reverted', (
    tester,
  ) async {
    final resource = Resource(kind: 'Other', name: 'lab-device', model: 'Old');
    final store = InventoryStore(
      RackPeekApi(baseUrl: 'https://server.example.test', apiKey: 'test-key'),
    );

    await tester.pumpWidget(
      MaterialApp(
        theme: scribeTheme(),
        home: Scaffold(
          body: EditResourceScreen(store: store, resource: resource),
        ),
      ),
    );

    expect(find.text('Not saved'), findsNothing);
    final modelField = find.byType(TextField).first;
    await tester.enterText(modelField, 'New model');
    await tester.pump();
    expect(find.text('Not saved'), findsOneWidget);

    await tester.enterText(modelField, 'Old');
    await tester.pump();
    expect(find.text('Not saved'), findsNothing);

    store.dispose();
  });
}
