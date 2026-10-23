import 'package:flutter/material.dart';

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
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _baseUrl.dispose();
    _apiKey.dispose();
    super.dispose();
  }

  Future<void> _connect() async {
    final baseUrl = _baseUrl.text.trim().replaceAll(RegExp(r'/+$'), '');
    final apiKey = _apiKey.text.trim();

    if (baseUrl.isEmpty || !baseUrl.startsWith('http')) {
      setState(
        () => _error = 'Enter the server URL, e.g. http://192.168.1.10:8080',
      );
      return;
    }
    if (apiKey.isEmpty) {
      setState(() => _error = 'Enter the API key (RPK_API_KEY on the server).');
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      await RackPeekApi(baseUrl: baseUrl, apiKey: apiKey).getSummary();
      await widget.settings.save(baseUrl, apiKey);
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => HomeShell(
            settings: widget.settings,
            baseUrl: baseUrl,
            apiKey: apiKey,
          ),
        ),
      );
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
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
                  keyboardType: TextInputType.url,
                  autocorrect: false,
                  decoration: const InputDecoration(
                    hintText: 'http://192.168.1.10:8080',
                    labelText: 'Server URL',
                  ),
                ),
                const SizedBox(height: ScribeSpacing.relatedGap),
                const SectionLabel('API key'),
                const SizedBox(height: ScribeSpacing.relatedGap),
                TextField(
                  controller: _apiKey,
                  obscureText: true,
                  autocorrect: false,
                  decoration: const InputDecoration(
                    labelText: 'API key',
                    helperText:
                        'Stored in your device keychain. Never leaves your network.',
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
