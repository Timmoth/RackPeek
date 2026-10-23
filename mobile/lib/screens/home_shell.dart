import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';

import '../data/api_client.dart';
import '../data/settings_store.dart';
import '../design.dart';
import '../state/inventory_store.dart';
import 'connection_screen.dart';
import 'privacy_policy_screen.dart';
import 'resources_tab.dart';
import 'summary_tab.dart';
import 'tree_tab.dart';

class HomeShell extends StatefulWidget {
  const HomeShell({
    super.key,
    required this.settings,
    required this.baseUrl,
    required this.apiKey,
  });

  final SettingsStore settings;
  final String baseUrl;
  final String apiKey;

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  late final InventoryStore _store;
  int _index = 0;

  @override
  void initState() {
    super.initState();
    _store = InventoryStore(
      RackPeekApi(baseUrl: widget.baseUrl, apiKey: widget.apiKey),
    );
    _store.refresh();
  }

  @override
  void dispose() {
    _store.dispose();
    super.dispose();
  }

  Future<void> _openSettings() async {
    await showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: ScribeSpacing.panelPadding,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('Connected to', style: ScribeType.sectionLabel),
                  const SizedBox(height: 4),
                  Text(widget.baseUrl, style: ScribeType.body),
                  const SizedBox(height: ScribeSpacing.majorSectionGap),
                  ScribeAction(
                    label: 'Privacy policy',
                    secondary: true,
                    onPressed: () {
                      Navigator.of(sheetContext).pop();
                      Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (_) => const PrivacyPolicyScreen(),
                        ),
                      );
                    },
                  ),
                  const SizedBox(height: ScribeSpacing.relatedGap),
                  ScribeAction(
                    label: 'Refresh inventory',
                    secondary: true,
                    onPressed: () {
                      _store.refresh();
                      Navigator.of(sheetContext).pop();
                    },
                  ),
                  const SizedBox(height: ScribeSpacing.relatedGap),
                  ScribeAction(
                    label: 'Disconnect',
                    danger: true,
                    onPressed: () async {
                      await widget.settings.clear();
                      if (!sheetContext.mounted) return;
                      Navigator.of(sheetContext).pop();
                      if (!mounted) return;
                      Navigator.of(context).pushAndRemoveUntil(
                        MaterialPageRoute(
                          builder: (_) =>
                              ConnectionScreen(settings: widget.settings),
                        ),
                        (route) => false,
                      );
                    },
                  ),
                ],
              ),
            ),
            const SizedBox(height: ScribeSpacing.majorSectionGap),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _store,
      builder: (context, _) {
        return Scaffold(
          appBar: AppBar(
            backgroundColor: ScribeColors.canvas,
            elevation: 0,
            scrolledUnderElevation: 0,
            title: const Wordmark(),
            actions: [
              IconButton(
                tooltip: 'Settings',
                icon: const Icon(CupertinoIcons.gear),
                onPressed: _openSettings,
              ),
            ],
          ),
          body: IndexedStack(
            index: _index,
            children: [
              SummaryTab(store: _store),
              ResourcesTab(store: _store),
              TreeTab(store: _store),
            ],
          ),
          bottomNavigationBar: NavigationBar(
            backgroundColor: ScribeColors.surface,
            selectedIndex: _index,
            onDestinationSelected: (i) => setState(() => _index = i),
            destinations: const [
              NavigationDestination(
                icon: Icon(CupertinoIcons.chart_bar),
                label: 'Summary',
              ),
              NavigationDestination(
                icon: Icon(CupertinoIcons.list_bullet),
                label: 'Resources',
              ),
              NavigationDestination(
                icon: Icon(CupertinoIcons.tree),
                label: 'Tree',
              ),
            ],
          ),
        );
      },
    );
  }
}
