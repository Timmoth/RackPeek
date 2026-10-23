import 'package:flutter/material.dart';

import '../design.dart';
import '../state/inventory_store.dart';

class SummaryTab extends StatelessWidget {
  const SummaryTab({super.key, required this.store});

  final InventoryStore store;

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

    return RefreshIndicator(
      onRefresh: store.refresh,
      child: ScribeSheet(
        children: [
          const Text('Infrastructure', style: ScribeType.hero),
          _countPanel('Hardware', summary['hardware'] as Map<String, dynamic>,
              'totalHardware', 'hardwareByKind'),
          _countPanel('Systems', summary['systems'] as Map<String, dynamic>,
              'totalSystems', 'systemsByType'),
          _servicesPanel(summary['services'] as Map<String, dynamic>),
          _tagPanel('Tags', summary['tags'] as Map<String, dynamic>? ?? {}),
          _tagPanel('Labels', summary['labels'] as Map<String, dynamic>? ?? {}),
        ],
      ),
    );
  }

  Widget _countPanel(String title, Map<String, dynamic> section, String totalKey,
      String byKey) {
    final total = section[totalKey] as int? ?? 0;
    final breakdown = (section[byKey] as Map<String, dynamic>? ?? {})
        .map((k, v) => MapEntry(k, (v as num).toInt()));

    return SurfacePanel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionLabel(title),
          const SizedBox(height: ScribeSpacing.relatedGap),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('$total', style: ScribeType.section),
              Text('$total total', style: ScribeType.metadata),
            ],
          ),
          const SizedBox(height: ScribeSpacing.relatedGap),
          Wrap(
            spacing: ScribeSpacing.relatedGap,
            runSpacing: 8,
            children: [
              for (final entry in breakdown.entries)
                NoteFact(text: '${entry.value} ${entry.key}'),
            ],
          ),
        ],
      ),
    );
  }

  Widget _servicesPanel(Map<String, dynamic> services) {
    return SurfacePanel(
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('${services['totalServices'] ?? 0}', style: ScribeType.section),
                const Text('services', style: ScribeType.metadata),
              ],
            ),
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('${services['totalIpAddresses'] ?? 0}', style: ScribeType.section),
                const Text('IP addresses', style: ScribeType.metadata),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _tagPanel(String title, Map<String, dynamic> counts) {
    if (counts.isEmpty) return const SizedBox.shrink();
    return SurfacePanel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionLabel(title),
          const SizedBox(height: ScribeSpacing.relatedGap),
          Wrap(
            spacing: ScribeSpacing.relatedGap,
            runSpacing: 8,
            children: [
              for (final entry in counts.entries)
                NoteFact(text: '${entry.key} (${entry.value})'),
            ],
          ),
        ],
      ),
    );
  }
}
