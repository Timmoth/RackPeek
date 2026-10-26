import 'dart:convert';

import 'package:flutter/material.dart';

/// Parsed `GET /api/inventory` document. One flat [Resource] shape carries
/// every kind's optional fields; kind-specific parsing decides which are set.
class InventoryDocument {
  InventoryDocument({
    required this.version,
    required this.resources,
    required this.connections,
    Map<String, dynamic>? raw,
  }) : _raw = raw ?? {'version': version, 'resources': [], 'connections': []};

  final int version;
  final List<Resource> resources;
  final List<Connection> connections;
  final Map<String, dynamic> _raw;

  Map<String, dynamic> toJson() => _copyMap(_raw);

  InventoryDocument withResourcePatch(Map<String, dynamic> patch) {
    final next = toJson();
    final resources = (next['resources'] as List<dynamic>? ?? [])
        .map((resource) => Map<String, dynamic>.from(resource as Map))
        .toList();
    final kind = patch['kind']?.toString();
    final name = patch['name']?.toString();
    final index = resources.indexWhere(
      (resource) =>
          resource['kind'] == kind &&
          resource['name'].toString().toLowerCase() == name?.toLowerCase(),
    );

    if (index < 0) {
      resources.add(_copyMap(patch));
    } else {
      _mergeMap(resources[index], patch);
    }
    next['resources'] = resources;
    return InventoryDocument.fromJson(next);
  }

  factory InventoryDocument.fromJson(Map<String, dynamic> json) {
    return InventoryDocument(
      version: json['version'] as int? ?? 0,
      resources: (json['resources'] as List<dynamic>? ?? [])
          .map((e) => Resource.fromJson(e as Map<String, dynamic>))
          .toList(),
      connections: (json['connections'] as List<dynamic>? ?? [])
          .map((e) => Connection.fromJson(e as Map<String, dynamic>))
          .toList(),
      raw: _copyMap(json),
    );
  }

  static Map<String, dynamic> _copyMap(Map source) =>
      Map<String, dynamic>.from(jsonDecode(jsonEncode(source)) as Map);

  static void _mergeMap(
    Map<String, dynamic> target,
    Map<String, dynamic> patch,
  ) {
    for (final entry in patch.entries) {
      final current = target[entry.key];
      final value = entry.value;
      if (current is Map && value is Map) {
        final nested = Map<String, dynamic>.from(current);
        _mergeMap(nested, Map<String, dynamic>.from(value));
        target[entry.key] = nested;
      } else {
        target[entry.key] = value;
      }
    }
  }

  Resource? byName(String name) {
    for (final resource in resources) {
      if (resource.name.toLowerCase() == name.toLowerCase()) return resource;
    }
    return null;
  }

  List<Resource> dependantsOf(String name) {
    return resources
        .where(
          (r) => r.runsOn.any(
            (parent) => parent.toLowerCase() == name.toLowerCase(),
          ),
        )
        .toList();
  }

  List<Connection> connectionsFor(String name) {
    return connections
        .where(
          (c) =>
              c.a.resource.toLowerCase() == name.toLowerCase() ||
              c.b.resource.toLowerCase() == name.toLowerCase(),
        )
        .toList();
  }
}

class Resource {
  Resource({
    required this.kind,
    required this.name,
    this.tags = const [],
    this.labels = const {},
    this.notes,
    this.runsOn = const [],
    this.discoveryId,
    // hardware
    this.ram,
    this.ipmi,
    this.cpus = const [],
    this.drives = const [],
    this.gpus = const [],
    this.ports = const [],
    this.model,
    this.managed,
    this.poe,
    this.speed,
    this.va,
    this.description,
    // system
    this.type,
    this.os,
    this.cores,
    this.ramGb,
    this.ip,
    // service
    this.network,
    this.endpoints = const [],
  });

  final String kind;
  final String name;
  final List<String> tags;
  final Map<String, String> labels;
  final String? notes;
  final List<String> runsOn;
  final String? discoveryId;

  final Ram? ram;
  final bool? ipmi;
  final List<Cpu> cpus;
  final List<Drive> drives;
  final List<Gpu> gpus;
  final List<PortGroup> ports;
  final String? model;
  final bool? managed;
  final bool? poe;
  final double? speed;
  final int? va;
  final String? description;

  final String? type;
  final String? os;
  final int? cores;
  final double? ramGb;
  final String? ip;

  final Network? network;
  final List<Network> endpoints;

  bool get isHardware => const [
    'Server',
    'Switch',
    'Firewall',
    'Router',
    'AccessPoint',
    'Desktop',
    'Laptop',
    'Ups',
    'Other',
  ].contains(kind);

  factory Resource.fromJson(Map<String, dynamic> json) {
    final kind = json['kind'] as String? ?? '';
    final isSystem = kind == 'System';

    List<T> list<T>(String key, T Function(Map<String, dynamic>) parse) =>
        (json[key] as List<dynamic>? ?? [])
            .map((e) => parse(e as Map<String, dynamic>))
            .toList();

    return Resource(
      kind: kind,
      name: json['name'] as String? ?? '',
      tags: (json['tags'] as List<dynamic>? ?? []).cast<String>(),
      labels: (json['labels'] as Map<String, dynamic>? ?? {}).map(
        (k, v) => MapEntry(k, v.toString()),
      ),
      notes: json['notes'] as String?,
      runsOn: (json['runsOn'] as List<dynamic>? ?? []).cast<String>(),
      discoveryId: json['discoveryId'] as String?,
      ram: isSystem ? null : _parse(json['ram'], (m) => Ram.fromJson(m)),
      ipmi: json['ipmi'] as bool?,
      cpus: list('cpus', Cpu.fromJson),
      drives: list('drives', Drive.fromJson),
      gpus: list('gpus', Gpu.fromJson),
      ports: list('ports', PortGroup.fromJson),
      model: json['model'] as String?,
      managed: json['managed'] as bool?,
      poe: json['poe'] as bool?,
      speed: (json['speed'] as num?)?.toDouble(),
      va: json['va'] as int?,
      description: json['description'] as String?,
      type: json['type'] as String?,
      os: json['os'] as String?,
      cores: json['cores'] as int?,
      ramGb: isSystem ? (json['ram'] as num?)?.toDouble() : null,
      ip: json['ip'] as String?,
      network: _parse(json['network'], (m) => Network.fromJson(m)),
      endpoints: list('endpoints', Network.fromJson),
    );
  }

  static T? _parse<T>(dynamic value, T Function(Map<String, dynamic>) parse) {
    if (value is Map<String, dynamic>) return parse(value);
    return null;
  }

  /// A short human line for list rows.
  String get subtitle {
    if (kind == 'Service') {
      final net = network?.describe();
      if (net != null && net.isNotEmpty) return net;
    }
    if (kind == 'System') {
      final parts = [type, os, ip].where((p) => p != null && p.isNotEmpty);
      if (parts.isNotEmpty) return parts.join(' · ');
    }
    if (model != null && model!.isNotEmpty) return model!;
    if (runsOn.isNotEmpty) return 'on ${runsOn.join(', ')}';
    return kind;
  }

  IconData get glyph => switch (kind) {
    'Server' => Icons.dns,
    'Switch' => Icons.swap_horiz,
    'Router' => Icons.router,
    'Firewall' => Icons.lock,
    'AccessPoint' => Icons.wifi,
    'Desktop' => Icons.desktop_windows,
    'Laptop' => Icons.laptop,
    'Ups' => Icons.bolt,
    'Other' => Icons.apps,
    'System' => Icons.memory,
    'Service' => Icons.public,
    _ => Icons.help_outline,
  };
}

class Ram {
  Ram({this.size, this.mts});
  final double? size;
  final int? mts;

  factory Ram.fromJson(Map<String, dynamic> json) =>
      Ram(size: (json['size'] as num?)?.toDouble(), mts: json['mts'] as int?);
}

class Cpu {
  Cpu({this.model, this.cores, this.threads});
  final String? model;
  final int? cores;
  final int? threads;

  factory Cpu.fromJson(Map<String, dynamic> json) => Cpu(
    model: json['model'] as String?,
    cores: json['cores'] as int?,
    threads: json['threads'] as int?,
  );

  String describe() => [
    model,
    if (cores != null) '$cores cores',
    if (threads != null) '$threads threads',
  ].where((p) => p != null).join(' · ');
}

class Drive {
  Drive({this.type, this.size});
  final String? type;
  final int? size;

  factory Drive.fromJson(Map<String, dynamic> json) =>
      Drive(type: json['type'] as String?, size: json['size'] as int?);

  String describe() =>
      [if (size != null) '$size GB', type].where((p) => p != null).join(' ');
}

class Gpu {
  Gpu({this.model, this.vram});
  final String? model;
  final int? vram;

  factory Gpu.fromJson(Map<String, dynamic> json) =>
      Gpu(model: json['model'] as String?, vram: json['vram'] as int?);

  String describe() => [
    model,
    if (vram != null) '${vram}GB VRAM',
  ].where((p) => p != null).join(' · ');
}

class PortGroup {
  PortGroup({this.type, this.speed, this.count});
  final String? type;
  final double? speed;
  final int? count;

  factory PortGroup.fromJson(Map<String, dynamic> json) => PortGroup(
    type: json['type'] as String?,
    speed: (json['speed'] as num?)?.toDouble(),
    count: json['count'] as int?,
  );

  String describe() => [
    '${count ?? 0} × ${type ?? 'port'}',
    if (speed != null) '${speed!.toStringAsFixed(0)} Gbps',
  ].join(' · ');
}

class Network {
  Network({this.ip, this.port, this.protocol, this.url});
  final String? ip;
  final int? port;
  final String? protocol;
  final String? url;

  factory Network.fromJson(Map<String, dynamic> json) => Network(
    ip: json['ip'] as String?,
    port: json['port'] as int?,
    protocol: json['protocol'] as String?,
    url: json['url'] as String?,
  );

  String describe() {
    if (url != null && url!.isNotEmpty) return url!;
    if (ip == null) return '';
    return port != null ? '$ip:$port' : ip!;
  }
}

class Connection {
  Connection({required this.a, required this.b, this.label, this.notes});
  final PortReference a;
  final PortReference b;
  final String? label;
  final String? notes;

  factory Connection.fromJson(Map<String, dynamic> json) => Connection(
    a: PortReference.fromJson(json['a'] as Map<String, dynamic>),
    b: PortReference.fromJson(json['b'] as Map<String, dynamic>),
    label: json['label'] as String?,
    notes: json['notes'] as String?,
  );

  String describe() {
    final ends = [
      '${a.resource}:${a.portGroup}.$a.portIndex',
      '${b.resource}:${b.portGroup}.$b.portIndex',
    ];
    final link = ends.join(' ↔ ');
    return label != null && label!.isNotEmpty ? '$link  ($label)' : link;
  }
}

class PortReference {
  PortReference({
    required this.resource,
    required this.portGroup,
    required this.portIndex,
  });
  final String resource;
  final int portGroup;
  final int portIndex;

  factory PortReference.fromJson(Map<String, dynamic> json) => PortReference(
    resource: json['resource'] as String? ?? '',
    portGroup: json['portGroup'] as int? ?? 0,
    portIndex: json['portIndex'] as int? ?? 0,
  );
}
