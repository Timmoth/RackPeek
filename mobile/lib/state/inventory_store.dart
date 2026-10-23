import 'package:flutter/foundation.dart';

import '../data/api_client.dart';
import '../models/inventory.dart';

/// Holds the fetched inventory document for the UI and performs writes.
class InventoryStore extends ChangeNotifier {
  InventoryStore(this.api);

  final RackPeekApi api;

  InventoryDocument? document;
  Map<String, dynamic>? summary;
  Object? loadError;
  bool busy = false;

  bool get loaded => document != null;

  Future<void> refresh() async {
    loadError = null;
    try {
      final results = await Future.wait([
        api.getDocument(),
        api.getSummary(),
      ]);
      document = results[0] as InventoryDocument;
      summary = results[1] as Map<String, dynamic>;
    } catch (e) {
      loadError = e;
    }
    notifyListeners();
  }

  /// Sends a partial update (Merge) and refetches. Returns the server's
  /// diff summary, or throws [ApiException].
  Future<Map<String, dynamic>> applyPatch(Map<String, dynamic> patch) async {
    busy = true;
    notifyListeners();
    try {
      final result = await api.upsert({
        'version': document?.version ?? 4,
        'resources': [patch],
      });
      await refresh();
      return result;
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  Future<Map<String, dynamic>> addResource(Map<String, dynamic> resource) async {
    busy = true;
    notifyListeners();
    try {
      final result = await api.upsert({
        'version': document?.version ?? 4,
        'resources': [resource],
      });
      await refresh();
      return result;
    } finally {
      busy = false;
      notifyListeners();
    }
  }
}
