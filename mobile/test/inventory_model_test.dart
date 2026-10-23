import 'package:flutter_test/flutter_test.dart';
import 'package:rackpeek_mobile/models/inventory.dart';

const _document = {
  'version': 4,
  'resources': [
    {
      'kind': 'Server',
      'name': 'srv01',
      'tags': ['homelab'],
      'labels': {'env': 'prod'},
      'ram': {'size': 64, 'mts': 4800},
      'ipmi': true,
      'ports': [
        {'type': 'rj45', 'speed': 1, 'count': 2}
      ],
    },
    {'kind': 'AccessPoint', 'name': 'ap01', 'model': 'U6 Lite', 'speed': 4.8},
    {
      'kind': 'System',
      'name': 'pve01',
      'runsOn': ['srv01'],
      'type': 'hypervisor',
      'os': 'Proxmox VE 9',
      'cores': 16,
      'ram': 128,
      'ip': '10.0.0.10'
    },
    {
      'kind': 'Service',
      'name': 'jellyfin',
      'runsOn': ['pve01'],
      'network': {'ip': '10.0.0.11', 'port': 8096, 'protocol': 'TCP'}
    },
  ],
  'connections': [
    {
      'a': {'resource': 'srv01', 'portGroup': 0, 'portIndex': 0},
      'b': {'resource': 'ap01', 'portGroup': 0, 'portIndex': 0},
      'label': 'uplink'
    }
  ],
};

void main() {
  group('InventoryDocument', () {
    final doc = InventoryDocument.fromJson(_document);

    test('parses version, resources and connections', () {
      expect(doc.version, 4);
      expect(doc.resources.length, 4);
      expect(doc.connections.length, 1);
    });

    test('hardware ram is an object, system ram is a number', () {
      final server = doc.byName('srv01')!;
      expect(server.ram?.size, 64);
      expect(server.ram?.mts, 4800);

      final system = doc.byName('pve01')!;
      expect(system.ramGb, 128);
      expect(system.ram, isNull);
    });

    test('AccessPoint discriminator parses and carries its fields', () {
      final ap = doc.byName('ap01')!;
      expect(ap.kind, 'AccessPoint');
      expect(ap.model, 'U6 Lite');
      expect(ap.speed, 4.8);
    });

    test('name lookup is case-insensitive', () {
      expect(doc.byName('SRV01')?.name, 'srv01');
    });

    test('dependants follow runsOn', () {
      expect(doc.dependantsOf('srv01').map((r) => r.name), ['pve01']);
      expect(doc.dependantsOf('pve01').map((r) => r.name), ['jellyfin']);
    });

    test('connections touch both endpoints', () {
      expect(doc.connectionsFor('srv01').length, 1);
      expect(doc.connectionsFor('pve01'), isEmpty);
      expect(doc.connections.first.label, 'uplink');
    });

    test('service subtitle shows address', () {
      final service = doc.byName('jellyfin')!;
      expect(service.subtitle, '10.0.0.11:8096');
    });

    test('missing optional fields default safely', () {
      final sparse = Resource.fromJson({'kind': 'Other', 'name': 'x'});
      expect(sparse.tags, isEmpty);
      expect(sparse.labels, isEmpty);
      expect(sparse.runsOn, isEmpty);
      expect(sparse.notes, isNull);
      expect(sparse.subtitle, 'Other');
    });
  });
}
