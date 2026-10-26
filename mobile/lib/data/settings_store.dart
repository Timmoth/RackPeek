import 'dart:convert';

import 'key_value_storage.dart';

/// Server connection details and offline inventory are kept in the device
/// keychain/keystore.
class SettingsStore {
  SettingsStore({KeyValueStorage? storage})
    : _storage = storage ?? SecureKeyValueStorage();

  final KeyValueStorage _storage;

  static const _baseUrlKey = 'serverBaseUrl';
  static const _apiKeyKey = 'serverApiKey';
  static const _localModeKey = 'localMode';
  static const _inventoryKey = 'offlineInventoryState';

  Future<({String baseUrl, String apiKey})?> load() async {
    final baseUrl = await _storage.read(_baseUrlKey);
    final apiKey = await _storage.read(_apiKeyKey);
    if (baseUrl == null ||
        baseUrl.isEmpty ||
        apiKey == null ||
        apiKey.isEmpty) {
      return null;
    }
    return (baseUrl: baseUrl, apiKey: apiKey);
  }

  Future<bool> isLocalMode() async =>
      await _storage.read(_localModeKey) == 'true';

  Future<void> save(String baseUrl, String apiKey) async {
    await _storage.write(_baseUrlKey, baseUrl.trim());
    await _storage.write(_apiKeyKey, apiKey.trim());
    await _storage.delete(_localModeKey);
  }

  Future<void> useLocalMode() async {
    await _storage.delete(_baseUrlKey);
    await _storage.delete(_apiKeyKey);
    await _storage.write(_localModeKey, 'true');
  }

  Future<Map<String, dynamic>?> loadOfflineState() async {
    final encoded = await _storage.read(_inventoryKey);
    if (encoded == null) return null;

    try {
      final decoded = jsonDecode(encoded);
      return decoded is Map<String, dynamic> ? decoded : null;
    } on FormatException {
      return null;
    }
  }

  Future<void> saveOfflineState(Map<String, dynamic> state) =>
      _storage.write(_inventoryKey, jsonEncode(state));

  Future<void> clearOfflineState() => _storage.delete(_inventoryKey);

  Future<bool> hasPendingLocalChanges() async {
    final state = await loadOfflineState();
    final pending = state?['pendingChanges'];
    return pending is List && pending.isNotEmpty;
  }

  Future<void> clear() async {
    await _storage.delete(_baseUrlKey);
    await _storage.delete(_apiKeyKey);
    await _storage.delete(_localModeKey);
  }
}
