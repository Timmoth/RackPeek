import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Server connection details, kept in the platform keychain/keystore.
class SettingsStore {
  SettingsStore({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _storage;

  static const _baseUrlKey = 'serverBaseUrl';
  static const _apiKeyKey = 'serverApiKey';

  Future<({String baseUrl, String apiKey})?> load() async {
    final baseUrl = await _storage.read(key: _baseUrlKey);
    final apiKey = await _storage.read(key: _apiKeyKey);
    if (baseUrl == null || baseUrl.isEmpty || apiKey == null || apiKey.isEmpty) {
      return null;
    }
    return (baseUrl: baseUrl, apiKey: apiKey);
  }

  Future<void> save(String baseUrl, String apiKey) async {
    await _storage.write(key: _baseUrlKey, value: baseUrl.trim());
    await _storage.write(key: _apiKeyKey, value: apiKey.trim());
  }

  Future<void> clear() async {
    await _storage.delete(key: _baseUrlKey);
    await _storage.delete(key: _apiKeyKey);
  }
}
