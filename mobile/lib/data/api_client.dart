import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/inventory.dart';

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

  Future<Map<String, dynamic>> getSummary() async {
    return _getJson('/api/inventory/summary');
  }

  Future<List<Map<String, dynamic>>> search(String query, {int max = 20}) async {
    final json = await _getJson('/api/inventory/search', {'q': query, 'max': '$max'});
    return (json['matches'] as List<dynamic>? ?? []).cast<Map<String, dynamic>>();
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

    final response = await _send(() => _client.post(
          _uri('/api/inventory'),
          headers: {..._headers, 'Content-Type': 'application/json'},
          body: body,
        ));

    return _decode(response);
  }

  Future<Map<String, dynamic>> _getJson(String path,
      [Map<String, String>? query]) async {
    final response =
        await _send(() => _client.get(_uri(path, query), headers: _headers));
    return _decode(response);
  }

  Future<http.Response> _send(Future<http.Response> Function() send) async {
    try {
      return await send();
    } on http.ClientException catch (e) {
      throw ApiException('Cannot reach the server: ${e.message}');
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
        throw ApiException('Unauthorized — check the API key.',
            statusCode: 401);
      case 503:
        throw ApiException(
            'The server has no API key configured (RPK_API_KEY).',
            statusCode: 503);
      default:
        throw ApiException(serverError ?? 'Request failed (${response.statusCode}).',
            statusCode: response.statusCode);
    }
  }
}
