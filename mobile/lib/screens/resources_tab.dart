import 'package:flutter/material.dart';

import '../design.dart';
import '../models/inventory.dart';
import '../state/inventory_store.dart';
import 'add_resource_screen.dart';
import 'resource_detail_screen.dart';

class ResourceFilter {
  const ResourceFilter({
    this.kind,
    this.hardwareOnly = false,
    this.systemType,
    this.systemOs,
    this.tag,
    this.labelKey,
  });

  final String? kind;
  final bool hardwareOnly;
  final String? systemType;
  final String? systemOs;
  final String? tag;
  final String? labelKey;
}

const _resourceKinds = [
  'Server',
  'Switch',
  'Router',
  'Firewall',
  'AccessPoint',
  'Desktop',
  'Laptop',
  'Ups',
  'Other',
  'System',
  'Service',
];

class ResourcesTab extends StatefulWidget {
  const ResourcesTab({
    super.key,
    required this.store,
    required this.filter,
    required this.filterRevision,
  });

  final InventoryStore store;
  final ResourceFilter filter;
  final int filterRevision;

  @override
  State<ResourcesTab> createState() => _ResourcesTabState();
}

class _ResourcesTabState extends State<ResourcesTab> {
  String _query = '';
  String? _kind;
  bool _hardwareOnly = false;
  String? _systemType;
  String? _systemOs;
  String? _tag;
  String? _labelKey;
  late final TextEditingController _searchController;

  static const _kinds = _resourceKinds;

  @override
  void initState() {
    super.initState();
    _searchController = TextEditingController();
    _applyFilter(widget.filter);
  }

  @override
  void didUpdateWidget(covariant ResourcesTab oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.filterRevision != widget.filterRevision) {
      _applyFilter(widget.filter);
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  void _applyFilter(ResourceFilter filter) {
    _kind = filter.kind;
    _hardwareOnly = filter.hardwareOnly;
    _systemType = filter.systemType;
    _systemOs = filter.systemOs;
    _tag = filter.tag;
    _labelKey = filter.labelKey;
    _query = '';
    _searchController.clear();
  }

  bool get _hasFilters =>
      _kind != null ||
      _hardwareOnly ||
      _systemType != null ||
      _systemOs != null ||
      _tag != null ||
      _labelKey != null ||
      _query.isNotEmpty;

  List<Resource> get _visible {
    final all = widget.store.document?.resources ?? [];
    final query = _query.trim().toLowerCase();
    return all.where((r) {
      if (_kind != null && r.kind != _kind) return false;
      if (_hardwareOnly && !r.isHardware) return false;
      if (_systemType != null && r.type != _systemType) return false;
      if (_systemOs != null && r.os != _systemOs) return false;
      if (_tag != null && !r.tags.contains(_tag)) return false;
      if (_labelKey != null && !r.labels.containsKey(_labelKey)) return false;
      if (query.isEmpty) return true;
      if (r.name.toLowerCase().contains(query)) return true;
      if (r.tags.any((t) => t.toLowerCase().contains(query))) return true;
      if (r.labels.entries.any(
        (e) => '${e.key}: ${e.value}'.toLowerCase().contains(query),
      )) {
        return true;
      }
      final ip = r.ip ?? r.network?.ip;
      return ip != null && ip.toLowerCase().contains(query);
    }).toList()..sort((a, b) {
      final byKind = a.kind.compareTo(b.kind);
      return byKind != 0 ? byKind : a.name.compareTo(b.name);
    });
  }

  List<String> get _systemOperatingSystems {
    final values =
        (widget.store.document?.resources ?? [])
            .where((resource) => resource.kind == 'System')
            .map((resource) => resource.os)
            .whereType<String>()
            .where((os) => os.trim().isNotEmpty)
            .toSet()
            .toList()
          ..sort();
    return values;
  }

  List<String> get _systemTypes {
    final values =
        (widget.store.document?.resources ?? [])
            .where((resource) => resource.kind == 'System')
            .map((resource) => resource.type)
            .whereType<String>()
            .where((type) => type.trim().isNotEmpty)
            .toSet()
            .toList()
          ..sort();
    return values;
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
        onPressed: () => Navigator.of(context).push(
          MaterialPageRoute(
            builder: (_) => AddResourceScreen(store: widget.store),
          ),
        ),
        icon: const Icon(Icons.add),
        label: const Text('Add'),
      ),
      body: RefreshIndicator(
        onRefresh: widget.store.refresh,
        child: ScribeSheet(
          horizontalPadding: 12,
          sectionGap: 10,
          children: [
            TextField(
              controller: _searchController,
              decoration: const InputDecoration(
                hintText: 'Search names, IPs, tags, labels',
                prefixIcon: Icon(Icons.search),
                isDense: true,
                contentPadding: EdgeInsets.symmetric(vertical: 10),
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
                    selected: !_hasFilters,
                    onTap: () => setState(() {
                      _applyFilter(const ResourceFilter());
                    }),
                  ),
                  const SizedBox(width: 8),
                  FilterPill(
                    label: 'Hardware',
                    selected: _hardwareOnly,
                    onTap: () => setState(() {
                      _applyFilter(const ResourceFilter(hardwareOnly: true));
                    }),
                  ),
                  for (final kind in _kinds) ...[
                    const SizedBox(width: 8),
                    FilterPill(
                      label: _plurals[kind] ?? kind,
                      selected: _kind == kind,
                      onTap: () => setState(() {
                        _kind = kind;
                        _hardwareOnly = false;
                        _systemType = null;
                        _systemOs = null;
                      }),
                    ),
                  ],
                ],
              ),
            ),
            if (_kind == 'System') ...[
              const SizedBox(height: ScribeSpacing.relatedGap),
              SizedBox(
                height: ScribeSpacing.minTarget,
                child: ListView(
                  scrollDirection: Axis.horizontal,
                  padding: EdgeInsets.zero,
                  children: [
                    for (final type in _systemTypes) ...[
                      FilterPill(
                        label: 'Type: $type',
                        selected: _systemType == type,
                        onTap: () => setState(() {
                          _systemType = _systemType == type ? null : type;
                        }),
                      ),
                      const SizedBox(width: 8),
                    ],
                    for (final os in _systemOperatingSystems) ...[
                      FilterPill(
                        label: 'OS: $os',
                        selected: _systemOs == os,
                        onTap: () => setState(() {
                          _systemOs = _systemOs == os ? null : os;
                        }),
                      ),
                      const SizedBox(width: 8),
                    ],
                  ],
                ),
              ),
            ],
            const SizedBox(height: ScribeSpacing.relatedGap),
            if (_tag != null || _labelKey != null)
              Wrap(
                spacing: 8,
                runSpacing: 6,
                children: [
                  if (_tag != null)
                    FilterPill(
                      label: 'Tag: $_tag',
                      selected: true,
                      onTap: () => setState(() => _tag = null),
                    ),
                  if (_labelKey != null)
                    FilterPill(
                      label: 'Label: $_labelKey',
                      selected: true,
                      onTap: () => setState(() => _labelKey = null),
                    ),
                ],
              ),
            SurfacePanel(padding: EdgeInsets.zero, child: _list(context)),
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
            const Divider(
              height: 1,
              thickness: 1,
              color: ScribeColors.separator,
            ),
          ListTile(
            contentPadding: const EdgeInsets.symmetric(
              horizontal: ScribeSpacing.panelPadding,
              vertical: 4,
            ),
            leading: CategoryMark(icon: visible[i].glyph),
            title: Text(visible[i].name, style: ScribeType.title),
            subtitle: Text(visible[i].subtitle, style: ScribeType.metadata),
            trailing: const Icon(
              Icons.chevron_right,
              color: ScribeColors.tertiaryText,
            ),
            onTap: () => Navigator.of(context).push(
              MaterialPageRoute(
                builder: (_) => ResourceDetailScreen(
                  store: widget.store,
                  name: visible[i].name,
                ),
              ),
            ),
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

String resourceKindForSummary(String kind) => _resourceKinds.firstWhere(
  (candidate) => candidate.toLowerCase() == kind.toLowerCase(),
  orElse: () => kind,
);

String resourcePluralForSummary(String kind) =>
    _plurals[resourceKindForSummary(kind)] ?? kind;
