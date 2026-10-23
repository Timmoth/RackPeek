import 'dart:async';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../data/api_client.dart';
import '../data/settings_store.dart';
import '../design.dart';
import 'home_shell.dart';
import 'privacy_policy_screen.dart';

class ConnectionScreen extends StatefulWidget {
  const ConnectionScreen({super.key, required this.settings});

  final SettingsStore settings;

  @override
  State<ConnectionScreen> createState() => _ConnectionScreenState();
}

class _ConnectionScreenState extends State<ConnectionScreen> {
  final _baseUrl = TextEditingController();
  final _apiKey = TextEditingController();
  final http.Client _probeClient = http.Client();
  Timer? _probeTimer;
  int _probeToken = 0;
  bool _busy = false;
  bool _probing = false;
  bool _showApiKey = false;
  bool _statusUnavailable = false;
  String? _error;
  String? _reachabilityError;
  ApiStatus? _serverStatus;

  @override
  void dispose() {
    _probeTimer?.cancel();
    _baseUrl.dispose();
    _apiKey.dispose();
    _probeClient.close();
    super.dispose();
  }

  void _onAddressChanged(String value) {
    _probeTimer?.cancel();
    final token = ++_probeToken;
    final canProbe = serverUrlCandidates(value).isNotEmpty;
    setState(() {
      _probing = canProbe;
      _serverStatus = null;
      _statusUnavailable = false;
      _reachabilityError = null;
      _error = null;
    });
    if (!canProbe) return;

    _probeTimer = Timer(const Duration(milliseconds: 600), () {
      _checkAddress(value, token);
    });
  }

  Future<({String baseUrl, ApiStatus? status})> _probeAddress(
    String address,
  ) async {
    final candidates = serverUrlCandidates(address);
    if (candidates.isEmpty) {
      throw ApiException('Enter a server address, optionally with a port.');
    }

    ApiException? lastError;
    for (final candidate in candidates) {
      try {
        final status = await RackPeekApi(
          baseUrl: candidate,
          apiKey: '',
          client: _probeClient,
        ).getStatus();
        return (baseUrl: candidate, status: status);
      } on ApiException catch (e) {
        if (e.statusCode == 404) {
          return (baseUrl: candidate, status: null);
        }
        lastError = e;
      }
    }
    throw lastError ?? ApiException('Could not reach a RackPeek server.');
  }

  Future<void> _checkAddress(String address, int token) async {
    try {
      final result = await _probeAddress(address);
      if (!mounted || token != _probeToken) return;
      setState(() {
        _serverStatus = result.status;
        _statusUnavailable = result.status == null;
        _reachabilityError = null;
        _probing = false;
      });
    } on ApiException catch (e) {
      if (!mounted || token != _probeToken) return;
      setState(() {
        _reachabilityError = e.message;
        _serverStatus = null;
        _statusUnavailable = false;
        _probing = false;
      });
    }
  }

  Future<void> _connect() async {
    final address = _baseUrl.text.trim();
    final apiKey = _apiKey.text.trim();

    if (serverUrlCandidates(address).isEmpty) {
      setState(
        () => _error = 'Enter a server address, optionally with a port.',
      );
      return;
    }
    if (apiKey.isEmpty) {
      setState(() => _error = 'Enter the API key (RPK_API_KEY on the server).');
      return;
    }

    _probeTimer?.cancel();
    final token = ++_probeToken;
    setState(() {
      _busy = true;
      _probing = true;
      _error = null;
      _reachabilityError = null;
    });

    try {
      final result = await _probeAddress(address);
      if (!mounted || token != _probeToken) return;
      setState(() {
        _serverStatus = result.status;
        _statusUnavailable = result.status == null;
        _probing = false;
      });

      await RackPeekApi(baseUrl: result.baseUrl, apiKey: apiKey).getSummary();
      await widget.settings.save(result.baseUrl, apiKey);
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => HomeShell(
            settings: widget.settings,
            baseUrl: result.baseUrl,
            apiKey: apiKey,
          ),
        ),
      );
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _probing = false;
      });
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Widget _reachabilityIndicator() {
    if (_probing) {
      return const Row(
        children: [
          SizedBox(
            width: 16,
            height: 16,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
          SizedBox(width: 10),
          Expanded(child: Text('Checking server…')),
        ],
      );
    }

    if (_serverStatus case final status?) {
      return Row(
        children: [
          const Icon(Icons.check_circle, color: ScribeColors.success, size: 18),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              'Server reachable · API v${status.apiVersion} · ${status.appVersion}',
              style: const TextStyle(color: ScribeColors.success),
            ),
          ),
        ],
      );
    }

    if (_statusUnavailable) {
      return const Row(
        children: [
          Icon(Icons.info_outline, color: ScribeColors.secondaryText, size: 18),
          SizedBox(width: 8),
          Expanded(child: Text('Server reachable · API version unavailable')),
        ],
      );
    }

    if (_reachabilityError case final message?) {
      return Row(
        children: [
          const Icon(Icons.error_outline, color: ScribeColors.danger, size: 18),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              message,
              style: const TextStyle(color: ScribeColors.danger),
            ),
          ),
        ],
      );
    }

    return const Text(
      'HTTPS is tried first; HTTP is used if TLS is unavailable.',
      style: ScribeType.metadata,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: ScribeSheet(
        children: [
          const SizedBox(height: 32),
          const Center(child: Wordmark()),
          const Text(
            'Your infrastructure, in your pocket. Connect to a RackPeek server '
            'to browse and edit your inventory.',
            textAlign: TextAlign.center,
            style: ScribeType.body,
          ),
          SurfacePanel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SectionLabel('Server'),
                const SizedBox(height: ScribeSpacing.relatedGap),
                TextField(
                  controller: _baseUrl,
                  enabled: !_busy,
                  keyboardType: TextInputType.url,
                  autocorrect: false,
                  onChanged: _onAddressChanged,
                  decoration: const InputDecoration(
                    hintText: '192.168.1.10:8080 or server.example.com',
                    labelText: 'Server address',
                  ),
                ),
                const SizedBox(height: ScribeSpacing.relatedGap),
                _reachabilityIndicator(),
                const SizedBox(height: ScribeSpacing.majorSectionGap),
                const SectionLabel('API key'),
                const SizedBox(height: ScribeSpacing.relatedGap),
                TextField(
                  controller: _apiKey,
                  enabled: !_busy,
                  keyboardType: TextInputType.visiblePassword,
                  obscureText: !_showApiKey,
                  autocorrect: false,
                  enableSuggestions: false,
                  decoration: InputDecoration(
                    labelText: 'API key',
                    helperText: 'Stored securely on this device.',
                    suffixIcon: IconButton(
                      tooltip: _showApiKey ? 'Hide API key' : 'Show API key',
                      onPressed: () =>
                          setState(() => _showApiKey = !_showApiKey),
                      icon: Icon(
                        _showApiKey ? Icons.visibility_off : Icons.visibility,
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: ScribeSpacing.majorSectionGap),
                ScribeAction(
                  label: 'Connect',
                  busy: _busy,
                  onPressed: _connect,
                ),
              ],
            ),
          ),
          if (_error != null) InlineNotice(message: _error!, isError: true),
          Center(
            child: TextButton(
              onPressed: () => Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const PrivacyPolicyScreen()),
              ),
              child: const Text('Privacy policy'),
            ),
          ),
        ],
      ),
    );
  }
}
