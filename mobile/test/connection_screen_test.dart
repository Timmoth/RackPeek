import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rackpeek_mobile/data/settings_store.dart';
import 'package:rackpeek_mobile/screens/connection_screen.dart';

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
}
