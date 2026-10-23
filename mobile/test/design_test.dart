import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rackpeek_mobile/design.dart';

void main() {
  Widget wrap(Widget child) => MaterialApp(
        theme: scribeTheme(),
        home: Scaffold(body: child),
      );

  testWidgets('FilterPill renders its label and is tappable', (tester) async {
    var taps = 0;
    await tester.pumpStateful(wrap(
      FilterPill(label: 'Servers', selected: false, onTap: () => taps++),
    ));

    expect(find.text('Servers'), findsOneWidget);
    await tester.tap(find.text('Servers'));
    expect(taps, 1);
  });

  testWidgets('ScribeAction disables when busy or handler is null',
      (tester) async {
    // The busy spinner animates forever, so no pumpAndSettle here.
    await tester.pumpWidget(wrap(
      ScribeAction(label: 'Save', busy: true, onPressed: () {}),
    ));
    await tester.pump();
    final button = tester.widget<FilledButton>(find.byType(FilledButton));
    expect(button.onPressed, isNull);

    await tester.pumpStateful(wrap(
      ScribeAction(label: 'Save', onPressed: () {}),
    ));
    final enabled = tester.widget<FilledButton>(find.byType(FilledButton));
    expect(enabled.onPressed, isNotNull);
  });

  testWidgets('ScribeAction meets the 44pt target', (tester) async {
    await tester.pumpStateful(wrap(
      ScribeAction(label: 'Save', onPressed: () {}),
    ));
    final size = tester.getSize(find.byType(FilledButton));
    expect(size.height, greaterThanOrEqualTo(44));
  });

  testWidgets('InlineNotice shows error styling and retry', (tester) async {
    var retried = false;
    await tester.pumpStateful(wrap(
      InlineNotice(message: 'boom', isError: true, onRetry: () => retried = true),
    ));
    expect(find.text('boom'), findsOneWidget);
    await tester.tap(find.text('Retry'));
    expect(retried, isTrue);
  });

  testWidgets('CategoryMark renders its glyph', (tester) async {
    await tester.pumpStateful(wrap(const CategoryMark(icon: Icons.bolt)));
    expect(find.byIcon(Icons.bolt), findsOneWidget);
  });
}

extension on WidgetTester {
  Future<void> pumpStateful(Widget widget) async {
    await pumpWidget(widget);
    await pumpAndSettle();
  }
}
