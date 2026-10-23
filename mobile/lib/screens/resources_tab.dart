import 'package:flutter/material.dart';

import '../design.dart';
import '../models/inventory.dart';
import '../state/inventory_store.dart';
import 'add_resource_screen.dart';
import 'resource_detail_screen.dart';

class ResourcesTab extends StatefulWidget {
  const ResourcesTab({super.key, required this.store});

  final InventoryStore store;

  @override
  State<ResourcesTab> createState() => _ResourcesTabState();
}

class _ResourcesTabState extends State<ResourcesTab> {
  String _query = '';
  String? _kind;

  static const _kinds = [
    'Server', 'Switch', 'Router', 'Firewall', 'AccessPoint', 'Desktop',
    'Laptop', 'Ups', 'Other', 'System', 'Service',
  ];

  List<Resource> get _visible {
    final all = widget.store.document?.resources ?? [];
    final query = _query.trim().toLowerCase();
    return all.where((r) {
      if (_kind != null && r.kind != _kind) return false;
      if (query.isEmpty) return true;
      if (r.name.toLowerCase().contains(query)) return true;
      if (r.tags.any((t) => t.toLowerCase().contains(query))) return true;
      if (r.labels.entries
          .any((e) => '${e.key}: ${e.value}'.toLowerCase().contains(query))) {
        return true;
      }
      final ip = r.ip ?? r.network?.ip;
      return ip != null && ip.toLowerCase().contains(query);
    }).toList()
      ..sort((a, b) {
        final byKind = a.kind.compareTo(b.kind);
        return byKind != 0 ? byKind : a.name.compareTo(b.name);
      });
  }

  @override
  Widget build(BuildContext context) {
    if (widget.store.loadError != null) {
      return ScribeSheet(
        children: [
          InlineNotice(
            message: widget.store.loadError.toString(),
            isError: true,
            onRetry: widget.store.refresh,
          ),
        ],
      );
    }
    if (!widget.store.loaded) return const ScribeSpinner();

    return Scaffold(
      floatingActionButton: FloatingActionButton.extended(
        heroTag: 'add-resource',
        backgroundColor: ScribeColors.accent,
        onPressed: () => Navigator.of(context).push(MaterialPageRoute(
          builder: (_) => AddResourceScreen(store: widget.store),
        )),
        icon: const Icon(Icons.add),
        label: const Text('Add'),
      ),
      body: RefreshIndicator(
        onRefresh: widget.store.refresh,
        child: ScribeSheet(
          children: [
            TextField(
              decoration: const InputDecoration(
                hintText: 'Search names, IPs, tags, labels',
                prefixIcon: Icon(Icons.search),
              ),
              onChanged: (value) => setState(() => _query = value),
            ),
            SizedBox(
              height: ScribeSpacing.minTarget,
              child: ListView(
                scrollDirection: Axis.horizontal,
                padding: EdgeInsets.zero,
                children: [
                  FilterPill(
                    label: 'All',
                    selected: _kind == null,
                    onTap: () => setState(() => _kind = null),
                  ),
                  for (final kind in _kinds) ...[
                    const SizedBox(width: 8),
                    FilterPill(
                      label: _plurals[kind] ?? kind,
                      selected: _kind == kind,
                      onTap: () => setState(() => _kind = kind),
                    ),
                  ],
                ],
              ),
            ),
            SurfacePanel(
              padding: EdgeInsets.zero,
              child: _list(context),
            ),
          ],
        ),
      ),
    );
  }

  Widget _list(BuildContext context) {
    final visible = _visible;
    if (visible.isEmpty) {
      return const Padding(
        padding: EdgeInsets.all(ScribeSpacing.panelPadding),
        child: Text('Nothing matches.', style: ScribeType.metadata),
      );
    }

    return Column(
      children: [
        for (var i = 0; i < visible.length; i++) ...[
          if (i > 0)
            const Divider(height: 1, thickness: 1, color: ScribeColors.separator),
          ListTile(
            contentPadding: const EdgeInsets.symmetric(
                horizontal: ScribeSpacing.panelPadding, vertical: 4),
            leading: CategoryMark(icon: visible[i].glyph),
            title: Text(visible[i].name, style: ScribeType.title),
            subtitle: Text(visible[i].subtitle, style: ScribeType.metadata),
            trailing: const Icon(Icons.chevron_right,
                color: ScribeColors.tertiaryText),
            onTap: () => Navigator.of(context).push(MaterialPageRoute(
              builder: (_) =>
                  ResourceDetailScreen(store: widget.store, name: visible[i].name),
            )),
          ),
        ],
      ],
    );
  }
}

const _plurals = {
  'Server': 'Servers',
  'Switch': 'Switches',
  'Router': 'Routers',
  'Firewall': 'Firewalls',
  'AccessPoint': 'APs',
  'Desktop': 'Desktops',
  'Laptop': 'Laptops',
  'Ups': 'UPS',
  'Other': 'Other',
  'System': 'Systems',
  'Service': 'Services',
};
