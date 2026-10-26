import 'dart:async';

import 'package:flutter/foundation.dart';

import '../data/api_client.dart';
import '../data/settings_store.dart';
import '../models/inventory.dart';

/// Maintains a durable local inventory and retries queued changes when the
/// configured server becomes reachable again.
class InventoryStore extends ChangeNotifier {
  InventoryStore(this.api, {SettingsStore? settings})
    : settings = settings ?? SettingsStore();

  final RackPeekApi? api;
  final SettingsStore settings;

  InventoryDocument? document;
  Map<String, dynamic>? summary;
  Object? loadError;
  Object? connectionError;
  bool busy = false;
  bool isOnline = false;
  bool _restored = false;
  Future<void>? _restoring;
  bool _syncing = false;
  Timer? _retryTimer;
  final List<Map<String, dynamic>> _pendingChanges = [];

  bool get loaded => document != null;
  bool get isLocalMode => api == null;
  int get pendingChangeCount => _pendingChanges.length;
  bool get hasPendingChanges => _pendingChanges.isNotEmpty;

  Future<void> refresh() async {
    loadError = null;
    await _restoreLocalState();

    if (api == null) {
      document ??= InventoryDocument.fromJson({
        'version': 5,
        'resources': <dynamic>[],
        'connections': <dynamic>[],
      });
      summary = _buildSummary(document!);
      isOnline = false;
      notifyListeners();
      return;
    }

    try {
      await _syncPendingChanges();
      document = await api!.getDocument();
      summary = await api!.getSummary();
      isOnline = true;
      connectionError = null;
      await _persistLocalState();
    } catch (error) {
      isOnline = false;
      connectionError = error;
      document ??= InventoryDocument.fromJson({
        'version': 5,
        'resources': <dynamic>[],
        'connections': <dynamic>[],
      });
      summary = _buildSummary(document!);
      loadError = null;
      _scheduleRetry();
    }

    notifyListeners();
  }

  Future<void> applyPatch(Map<String, dynamic> patch) async {
    await _restoreLocalState();
    document ??= InventoryDocument.fromJson({
      'version': 5,
      'resources': <dynamic>[],
      'connections': <dynamic>[],
    });
    document = document!.withResourcePatch(patch);
    _pendingChanges.add(_copyMap(patch));
    summary = _buildSummary(document!);
    busy = true;
    await _persistLocalState();
    _scheduleRetry();
    notifyListeners();

    try {
      await _syncPendingChanges();
    } catch (error) {
      connectionError = error;
      isOnline = false;
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  Future<void> addResource(Map<String, dynamic> resource) =>
      applyPatch(resource);

  Future<void> _restoreLocalState() {
    if (_restored) return Future<void>.value();
    return _restoring ??= _loadLocalState();
  }

  Future<void> _loadLocalState() async {
    final state = await settings.loadOfflineState();
    _restored = true;
    if (state == null) return;

    final savedDocument = state['document'];
    if (savedDocument is Map<String, dynamic>) {
      document = InventoryDocument.fromJson(savedDocument);
      summary = _buildSummary(document!);
    }
    final pending = state['pendingChanges'];
    if (pending is List) {
      _pendingChanges.addAll(
        pending.whereType<Map>().map((change) => _copyMap(change)),
      );
    }
    if (_pendingChanges.isNotEmpty) _scheduleRetry();
  }

  Future<void> _persistLocalState() => settings.saveOfflineState({
    'document': document?.toJson(),
    'pendingChanges': _pendingChanges,
  });

  Future<void> _syncPendingChanges() async {
    if (api == null || _syncing || _pendingChanges.isEmpty) return;
    _syncing = true;
    try {
      while (_pendingChanges.isNotEmpty) {
        await api!.upsert({
          'version': document?.version ?? 5,
          'resources': [_pendingChanges.first],
        });
        _pendingChanges.removeAt(0);
        await _persistLocalState();
      }
      isOnline = true;
      connectionError = null;
      _retryTimer?.cancel();
      _retryTimer = null;
    } catch (error) {
      isOnline = false;
      connectionError = error;
      _scheduleRetry();
      rethrow;
    } finally {
      _syncing = false;
    }
  }

  void _scheduleRetry() {
    if (api == null || _retryTimer != null) return;
    _retryTimer = Timer.periodic(const Duration(seconds: 20), (_) async {
      if (_pendingChanges.isEmpty) {
        _retryTimer?.cancel();
        _retryTimer = null;
        return;
      }
      try {
        await _syncPendingChanges();
        notifyListeners();
      } on ApiException {
        notifyListeners();
      }
    });
  }

  @override
  void dispose() {
    _retryTimer?.cancel();
    super.dispose();
  }

  static Map<String, dynamic> _copyMap(Map source) => Map<String, dynamic>.from(
    source.map((key, value) => MapEntry('$key', value)),
  );

  static Map<String, dynamic> _buildSummary(InventoryDocument document) {
    final hardware = document.resources
        .where((resource) => resource.isHardware)
        .toList();
    final systems = document.resources
        .where((resource) => resource.kind == 'System')
        .toList();
    final services = document.resources
        .where((resource) => resource.kind == 'Service')
        .toList();
    final countBy = <String, int>{};
    for (final resource in hardware) {
      countBy.update(resource.kind, (count) => count + 1, ifAbsent: () => 1);
    }
    final systemsByType = <String, int>{};
    final systemsByOs = <String, int>{};
    for (final system in systems) {
      final type = system.type;
      if (type != null && type.isNotEmpty) {
        systemsByType.update(type, (count) => count + 1, ifAbsent: () => 1);
      }
      final os = system.os;
      if (os != null && os.isNotEmpty) {
        systemsByOs.update(os, (count) => count + 1, ifAbsent: () => 1);
      }
    }
    final tags = <String, int>{};
    final labels = <String, int>{};
    for (final resource in document.resources) {
      for (final tag in resource.tags) {
        tags.update(tag, (count) => count + 1, ifAbsent: () => 1);
      }
      for (final key in resource.labels.keys) {
        labels.update(key, (count) => count + 1, ifAbsent: () => 1);
      }
    }
    final addresses = <String>{};
    for (final service in services) {
      final primaryIp = service.network?.ip;
      if (primaryIp != null && primaryIp.isNotEmpty) addresses.add(primaryIp);
      for (final endpoint in service.endpoints) {
        final endpointIp = endpoint.ip;
        if (endpointIp != null && endpointIp.isNotEmpty) {
          addresses.add(endpointIp);
        }
      }
    }

    return {
      'hardware': {'totalHardware': hardware.length, 'hardwareByKind': countBy},
      'systems': {
        'totalSystems': systems.length,
        'systemsByType': systemsByType,
        'systemsByOs': systemsByOs,
      },
      'services': {
        'totalServices': services.length,
        'totalIpAddresses': addresses.length,
      },
      'tags': tags,
      'labels': labels,
    };
  }
}
