import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

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
    this.baseUrl,
    this.apiKey,
    this.httpClient,
  });

  final SettingsStore settings;
  final String? baseUrl;
  final String? apiKey;
  final http.Client? httpClient;

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> with WidgetsBindingObserver {
  late final InventoryStore _store;
  int _index = 0;
  ResourceFilter _resourceFilter = const ResourceFilter();
  int _filterRevision = 0;

  void _openResources(ResourceFilter filter) {
    setState(() {
      _resourceFilter = filter;
      _filterRevision++;
      _index = 1;
    });
  }

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _store = InventoryStore(
      widget.baseUrl != null && widget.apiKey != null
          ? RackPeekApi(
              baseUrl: widget.baseUrl!,
              apiKey: widget.apiKey!,
              client: widget.httpClient,
            )
          : null,
      settings: widget.settings,
    );
    _store.refresh();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _store.dispose();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed && widget.baseUrl != null) {
      _store.refresh();
    }
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
                  Text(
                    widget.baseUrl == null ? 'Offline mode' : 'Connected to',
                    style: ScribeType.sectionLabel,
                  ),
                  const SizedBox(height: 4),
                  Text(
                    widget.baseUrl ?? 'Inventory stored on this device',
                    style: ScribeType.body,
                  ),
                  if (widget.baseUrl != null && !_store.isOnline) ...[
                    const SizedBox(height: 6),
                    const Text(
                      'Server unavailable. Edits will be saved here and synced when it returns.',
                      style: ScribeType.metadata,
                    ),
                  ],
                  if (_store.hasPendingChanges) ...[
                    const SizedBox(height: 6),
                    Text(
                      '${_store.pendingChangeCount} local change(s) waiting to sync',
                      style: ScribeType.metadata,
                    ),
                  ],
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
                  if (_store.hasPendingChanges && widget.baseUrl != null)
                    ScribeAction(
                      label: 'Sync changes now',
                      secondary: true,
                      onPressed: () {
                        _store.refresh();
                        Navigator.of(sheetContext).pop();
                      },
                    ),
                  if (widget.baseUrl == null)
                    ScribeAction(
                      label: 'Connect to a server',
                      onPressed: () {
                        Navigator.of(sheetContext).pop();
                        Navigator.of(context).push(
                          MaterialPageRoute(
                            builder: (_) =>
                                ConnectionScreen(settings: widget.settings),
                          ),
                        );
                      },
                    )
                  else
                    ScribeAction(
                      label: 'Use offline mode',
                      danger: true,
                      onPressed: () async {
                        await widget.settings.useLocalMode();
                        if (!sheetContext.mounted) return;
                        Navigator.of(sheetContext).pop();
                        if (!mounted) return;
                        Navigator.of(context).pushAndRemoveUntil(
                          MaterialPageRoute(
                            builder: (_) =>
                                HomeShell(settings: widget.settings),
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
          body: Column(
            children: [
              if (_store.hasPendingChanges ||
                  (widget.baseUrl != null && !_store.isOnline))
                _syncBanner(),
              Expanded(
                child: IndexedStack(
                  index: _index,
                  children: [
                    SummaryTab(store: _store, onOpenResources: _openResources),
                    ResourcesTab(
                      store: _store,
                      filter: _resourceFilter,
                      filterRevision: _filterRevision,
                    ),
                    TreeTab(store: _store),
                  ],
                ),
              ),
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

  Widget _syncBanner() {
    final message = widget.baseUrl == null
        ? 'Offline · ${_store.pendingChangeCount} change(s) saved on this device'
        : !_store.isOnline
        ? 'Server unavailable · changes save here and sync when it returns'
        : '${_store.pendingChangeCount} change(s) syncing to server';
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      color: ScribeColors.surface,
      child: Text(message, style: ScribeType.metadata),
    );
  }
}
