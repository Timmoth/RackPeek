import 'package:flutter_test/flutter_test.dart';
import 'package:rackpeek_mobile/data/payload.dart';

void main() {
  group('resourcePatch', () {
    test('always carries kind and name', () {
      final patch = resourcePatch(kind: 'Server', name: 'srv01');
      expect(patch['kind'], 'Server');
      expect(patch['name'], 'srv01');
      expect(patch.length, 2);
    });

    test('omits null tags/labels/notes so merge leaves them untouched', () {
      final patch = resourcePatch(
        kind: 'System',
        name: 'pve01',
        fields: {'os': 'Debian 13'},
      );
      expect(patch['os'], 'Debian 13');
      expect(patch.containsKey('tags'), isFalse);
      expect(patch.containsKey('labels'), isFalse);
      expect(patch.containsKey('notes'), isFalse);
    });

    test('includes changed collections', () {
      final patch = resourcePatch(
        kind: 'Service',
        name: 'svc',
        tags: ['a', 'b'],
        labels: {'env': 'prod'},
        notes: 'hello',
      );
      expect(patch['tags'], ['a', 'b']);
      expect(patch['labels'], {'env': 'prod'});
      expect(patch['notes'], 'hello');
    });

    test('nested network patch passes through fields', () {
      final patch = resourcePatch(
        kind: 'Service',
        name: 'svc',
        fields: {
          'network': {'ip': '10.0.0.5', 'port': 8080}
        },
      );
      expect((patch['network'] as Map)['port'], 8080);
    });
  });

  group('buildUpsertPayload', () {
    test('wraps resources with the document version', () {
      final payload = buildUpsertPayload(version: 4, resources: [
        {'kind': 'Server', 'name': 'a'}
      ]);
      expect(payload['version'], 4);
      expect((payload['resources'] as List).length, 1);
      expect(payload.containsKey('connections'), isFalse);
    });
  });
}
