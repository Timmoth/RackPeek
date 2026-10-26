import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';

import 'data/settings_store.dart';
import 'design.dart';
import 'screens/connection_screen.dart';
import 'screens/home_shell.dart';

void main() {
  runApp(const RackPeekApp());
}

class RackPeekApp extends StatelessWidget {
  const RackPeekApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'RackPeek',
      debugShowCheckedModeBanner: false,
      theme: scribeTheme(),
      home: const _StartupGate(),
    );
  }
}

class _StartupGate extends StatefulWidget {
  const _StartupGate();

  @override
  State<_StartupGate> createState() => _StartupGateState();
}

class _StartupGateState extends State<_StartupGate> {
  final SettingsStore _settings = SettingsStore();
  bool _checking = true;

  @override
  void initState() {
    super.initState();
    _startup();
  }

  Future<void> _startup() async {
    final saved = await _settings.load();
    final localMode = await _settings.isLocalMode();
    if (!mounted) return;
    setState(() => _checking = false);
    if (saved != null) {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => HomeShell(
            settings: _settings,
            baseUrl: saved.baseUrl,
            apiKey: saved.apiKey,
          ),
        ),
      );
    } else if (localMode) {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => HomeShell(settings: _settings)),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_checking) {
      return const Scaffold(
        body: Center(child: CupertinoActivityIndicator(radius: 16)),
      );
    }
    return ConnectionScreen(settings: _settings);
  }
}
