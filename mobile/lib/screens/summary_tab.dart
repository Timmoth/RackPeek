import 'package:flutter/material.dart';

import '../design.dart';
import '../state/inventory_store.dart';
import 'resources_tab.dart';

class SummaryTab extends StatelessWidget {
  const SummaryTab({
    super.key,
    required this.store,
    required this.onOpenResources,
  });

  final InventoryStore store;
  final ValueChanged<ResourceFilter> onOpenResources;

  @override
  Widget build(BuildContext context) {
    if (store.loadError != null) {
      return ScribeSheet(
        children: [
          InlineNotice(
            message: store.loadError.toString(),
            isError: true,
            onRetry: store.refresh,
          ),
        ],
      );
    }

    final summary = store.summary;
    if (summary == null) return const ScribeSpinner();

    final hardware = summary['hardware'] as Map<String, dynamic>;
    final systems = summary['systems'] as Map<String, dynamic>;
    final services = summary['services'] as Map<String, dynamic>;

    return RefreshIndicator(
      onRefresh: store.refresh,
      child: ScribeSheet(
        horizontalPadding: 14,
        sectionGap: 12,
        children: [
          const Text('Infrastructure', style: ScribeType.hero),
          _countPanel(
            'Hardware',
            hardware,
            'totalHardware',
            'hardwareByKind',
            totalFilter: const ResourceFilter(hardwareOnly: true),
            itemFilter: (kind) =>
                ResourceFilter(kind: resourceKindForSummary(kind)),
          ),
          _countPanel(
            'Systems',
            systems,
            'totalSystems',
            'systemsByType',
            totalFilter: const ResourceFilter(kind: 'System'),
            itemFilter: (type) =>
                ResourceFilter(kind: 'System', systemType: type),
          ),
          _breakdownPanel(
            'Operating systems',
            systems['systemsByOs'] as Map<String, dynamic>? ?? {},
            (os) => ResourceFilter(kind: 'System', systemOs: os),
          ),
          _servicesPanel(services),
          _tagPanel(
            'Tags',
            summary['tags'] as Map<String, dynamic>? ?? {},
            (tag) => ResourceFilter(tag: tag),
          ),
          _tagPanel(
            'Labels',
            summary['labels'] as Map<String, dynamic>? ?? {},
            (key) => ResourceFilter(labelKey: key),
          ),
        ],
      ),
    );
  }

  Widget _countPanel(
    String title,
    Map<String, dynamic> section,
    String totalKey,
    String byKey, {
    required ResourceFilter totalFilter,
    required ResourceFilter Function(String key) itemFilter,
  }) {
    final total = section[totalKey] as int? ?? 0;
    final breakdown = (section[byKey] as Map<String, dynamic>? ?? {}).map(
      (k, v) => MapEntry(k, (v as num).toInt()),
    );

    return SurfacePanel(
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionLabel(title),
          const SizedBox(height: ScribeSpacing.relatedGap),
          InkWell(
            borderRadius: BorderRadius.circular(ScribeSpacing.controlRadius),
            onTap: () => onOpenResources(totalFilter),
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text('$total', style: ScribeType.section),
                  Row(
                    children: [
                      Text('$total total', style: ScribeType.metadata),
                      const SizedBox(width: 4),
                      const Icon(Icons.chevron_right, size: 18),
                    ],
                  ),
                ],
              ),
            ),
          ),
          if (breakdown.isNotEmpty) ...[
            const SizedBox(height: ScribeSpacing.relatedGap),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                for (final entry in breakdown.entries)
                  FilterPill(
                    label:
                        '${entry.value} ${resourcePluralForSummary(entry.key)}',
                    selected: false,
                    onTap: () => onOpenResources(itemFilter(entry.key)),
                  ),
              ],
            ),
          ],
        ],
      ),
    );
  }

  Widget _breakdownPanel(
    String title,
    Map<String, dynamic> counts,
    ResourceFilter Function(String key) filterFor,
  ) {
    if (counts.isEmpty) return const SizedBox.shrink();
    return SurfacePanel(
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionLabel(title),
          const SizedBox(height: ScribeSpacing.relatedGap),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (final entry in counts.entries)
                FilterPill(
                  label: '${entry.value} ${entry.key}',
                  selected: false,
                  onTap: () => onOpenResources(filterFor(entry.key)),
                ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _servicesPanel(Map<String, dynamic> services) {
    final total = services['totalServices'] as int? ?? 0;
    final totalIpAddresses = services['totalIpAddresses'] as int? ?? 0;
    return SurfacePanel(
      padding: const EdgeInsets.all(12),
      child: Row(
        children: [
          Expanded(
            child: _serviceCount(
              total,
              'services',
              const ResourceFilter(kind: 'Service'),
            ),
          ),
          Expanded(child: _serviceCount(totalIpAddresses, 'IP addresses')),
        ],
      ),
    );
  }

  Widget _serviceCount(int count, String label, [ResourceFilter? filter]) {
    return InkWell(
      borderRadius: BorderRadius.circular(ScribeSpacing.controlRadius),
      onTap: filter == null ? null : () => onOpenResources(filter),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(
          children: [
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('$count', style: ScribeType.section),
                Text(label, style: ScribeType.metadata),
              ],
            ),
            const SizedBox(width: 4),
            if (filter != null) const Icon(Icons.chevron_right, size: 18),
          ],
        ),
      ),
    );
  }

  Widget _tagPanel(
    String title,
    Map<String, dynamic> counts,
    ResourceFilter Function(String key) filterFor,
  ) {
    if (counts.isEmpty) return const SizedBox.shrink();
    return SurfacePanel(
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionLabel(title),
          const SizedBox(height: ScribeSpacing.relatedGap),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (final entry in counts.entries)
                FilterPill(
                  label: '${entry.key} (${entry.value})',
                  selected: false,
                  onTap: () => onOpenResources(filterFor(entry.key)),
                ),
            ],
          ),
        ],
      ),
    );
  }
}
