import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/inventory.dart';

List<String> serverUrlCandidates(String address) {
  final input = address.trim();
  if (input.isEmpty) return const [];

  final hasScheme = RegExp(r'^[a-zA-Z][a-zA-Z0-9+.-]*://').hasMatch(input);
  final prefixes = hasScheme ? const [''] : const ['https://', 'http://'];
  final candidates = <String>[];

  for (final prefix in prefixes) {
    final uri = Uri.tryParse('$prefix$input');
    if (uri == null ||
        !{'http', 'https'}.contains(uri.scheme.toLowerCase()) ||
        uri.host.isEmpty ||
        uri.userInfo.isNotEmpty ||
        uri.hasQuery ||
        uri.hasFragment) {
      continue;
    }

    final normalized = uri
        .replace(path: uri.path.replaceAll(RegExp(r'/+$'), ''))
        .toString()
        .replaceAll(RegExp(r'/+$'), '');
    if (!candidates.contains(normalized)) candidates.add(normalized);
  }

  return candidates;
}

class ApiStatus {
  const ApiStatus({required this.apiVersion, required this.appVersion});

  final String apiVersion;
  final String appVersion;

  factory ApiStatus.fromJson(Map<String, dynamic> json) => ApiStatus(
    apiVersion: json['apiVersion']?.toString() ?? '',
    appVersion: json['appVersion']?.toString() ?? '',
  );
}

class ApiException implements Exception {
  ApiException(this.message, {this.statusCode = 0});
  final String message;
  final int statusCode;

  @override
  String toString() => message;
}

/// Talks to a RackPeek server's REST API. Reads are the GET endpoints;
/// writes go through `POST /api/inventory` in Merge mode, which never
/// removes anything — partial updates send only the changed fields.
class RackPeekApi {
  RackPeekApi({
    required this.baseUrl,
    required this.apiKey,
    http.Client? client,
  }) : _client = client ?? http.Client();

  final String baseUrl;
  final String apiKey;
  final http.Client _client;

  Map<String, String> get _headers => {'X-Api-Key': apiKey};

  Uri _uri(String path, [Map<String, String>? query]) =>
      Uri.parse('$baseUrl$path').replace(queryParameters: query);

  Future<InventoryDocument> getDocument() async {
    final json = await _getJson('/api/inventory');
    return InventoryDocument.fromJson(json);
  }

  Future<ApiStatus> getStatus() async {
    final response = await _send(
      () =>
          _client.get(_uri('/api/status')).timeout(const Duration(seconds: 5)),
    );
    if (response.statusCode != 200) {
      throw ApiException(
        'No RackPeek API found at this address (${response.statusCode}).',
        statusCode: response.statusCode,
      );
    }

    try {
      final json = jsonDecode(response.body) as Map<String, dynamic>;
      final status = ApiStatus.fromJson(json);
      if (status.apiVersion.isEmpty) {
        throw const FormatException('Missing API version.');
      }
      return status;
    } on FormatException {
      throw ApiException('The server returned an invalid API status response.');
    } on TypeError {
      throw ApiException('The server returned an invalid API status response.');
    }
  }

  Future<Map<String, dynamic>> getSummary() async {
    return _getJson('/api/inventory/summary');
  }

  Future<List<Map<String, dynamic>>> search(
    String query, {
    int max = 20,
  }) async {
    final json = await _getJson('/api/inventory/search', {
      'q': query,
      'max': '$max',
    });
    return (json['matches'] as List<dynamic>? ?? [])
        .cast<Map<String, dynamic>>();
  }

  Future<Map<String, dynamic>> upsert(
    Map<String, dynamic> document, {
    bool dryRun = false,
  }) async {
    final body = jsonEncode({
      'json': document,
      'mode': 'Merge',
      'dryRun': dryRun,
    });

    final response = await _send(
      () => _client.post(
        _uri('/api/inventory'),
        headers: {..._headers, 'Content-Type': 'application/json'},
        body: body,
      ),
    );

    return _decode(response);
  }

  Future<Map<String, dynamic>> _getJson(
    String path, [
    Map<String, String>? query,
  ]) async {
    final response = await _send(
      () => _client.get(_uri(path, query), headers: _headers),
    );
    return _decode(response);
  }

  Future<http.Response> _send(Future<http.Response> Function() send) async {
    try {
      return await send();
    } on Exception catch (e) {
      throw ApiException('Cannot reach the server: $e');
    }
  }

  Map<String, dynamic> _decode(http.Response response) {
    Map<String, dynamic>? parsed;
    try {
      final body = jsonDecode(response.body);
      if (body is Map<String, dynamic>) parsed = body;
    } on FormatException {
      // fall through to status-based message
    }

    if (response.statusCode == 200 && parsed != null) return parsed;

    final serverError = parsed?['error'] as String?;
    switch (response.statusCode) {
      case 401:
        throw ApiException(
          'Unauthorized — check the API key.',
          statusCode: 401,
        );
      case 503:
        throw ApiException(
          'The server has no API key configured (RPK_API_KEY).',
          statusCode: 503,
        );
      default:
        throw ApiException(
          serverError ?? 'Request failed (${response.statusCode}).',
          statusCode: response.statusCode,
        );
    }
  }
}
