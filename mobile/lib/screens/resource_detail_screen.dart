import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';

import '../design.dart';
import '../models/inventory.dart';
import '../state/inventory_store.dart';
import 'edit_resource_screen.dart';

class ResourceDetailScreen extends StatelessWidget {
  const ResourceDetailScreen({
    super.key,
    required this.store,
    required this.name,
  });

  final InventoryStore store;
  final String name;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: store,
      builder: (context, _) {
        final document = store.document;
        final resource = document?.byName(name);

        if (document == null || resource == null) {
          return Scaffold(
            appBar: AppBar(title: const Text('Resource')),
            body: const ScribeSpinner(),
          );
        }

        final dependants = document.dependantsOf(resource.name);
        final connections = document.connectionsFor(resource.name);

        return Scaffold(
          appBar: AppBar(
            backgroundColor: ScribeColors.canvas,
            elevation: 0,
            scrolledUnderElevation: 0,
            title: Text(resource.name),
          ),
          body: RefreshIndicator(
            onRefresh: store.refresh,
            child: ScribeSheet(
              children: [
                Row(
                  children: [
                    CategoryMark(icon: resource.glyph),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(resource.name, style: ScribeType.hero),
                          Text(resource.kind, style: ScribeType.metadata),
                        ],
                      ),
                    ),
                  ],
                ),
                SurfacePanel(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const SectionLabel('Details'),
                      const SizedBox(height: ScribeSpacing.relatedGap),
                      _facts(context, resource),
                    ],
                  ),
                ),
                if (resource.runsOn.isNotEmpty)
                  _linksPanel('Runs on', resource.runsOn, context),
                if (dependants.isNotEmpty)
                  _linksPanel(
                    'Dependants',
                    dependants.map((r) => r.name).toList(),
                    context,
                  ),
                if (connections.isNotEmpty)
                  SurfacePanel(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const SectionLabel('Connections'),
                        const SizedBox(height: ScribeSpacing.relatedGap),
                        for (final connection in connections)
                          Padding(
                            padding: const EdgeInsets.only(bottom: 6),
                            child: Text(
                              connection.describe(),
                              style: ScribeType.body,
                            ),
                          ),
                      ],
                    ),
                  ),
                if (resource.tags.isNotEmpty || resource.labels.isNotEmpty)
                  SurfacePanel(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (resource.tags.isNotEmpty) ...[
                          const SectionLabel('Tags'),
                          const SizedBox(height: 8),
                          Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children: [
                              for (final tag in resource.tags)
                                NoteFact(text: tag),
                            ],
                          ),
                        ],
                        if (resource.labels.isNotEmpty) ...[
                          const SizedBox(height: ScribeSpacing.relatedGap),
                          const SectionLabel('Labels'),
                          const SizedBox(height: 8),
                          Wrap(
                            spacing: 8,
                            runSpacing: 8,
                            children: [
                              for (final entry in resource.labels.entries)
                                NoteFact(text: '${entry.key}: ${entry.value}'),
                            ],
                          ),
                        ],
                      ],
                    ),
                  ),
                if (resource.notes != null && resource.notes!.isNotEmpty)
                  SurfacePanel(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const SectionLabel('Notes'),
                        const SizedBox(height: 8),
                        SelectableText(resource.notes!, style: ScribeType.body),
                      ],
                    ),
                  ),
                ScribeAction(
                  label: 'Edit',
                  secondary: true,
                  onPressed: () => Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) =>
                          EditResourceScreen(store: store, resource: resource),
                    ),
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _facts(BuildContext context, Resource resource) {
    final facts = <String>[];

    void add(String? value) {
      if (value != null && value.isNotEmpty) facts.add(value);
    }

    add(resource.model);
    if (resource.ram?.size != null) {
      final mts = resource.ram!.mts;
      add(
        'RAM ${resource.ram!.size!.toStringAsFixed(0)} GB'
        '${mts != null ? ' ($mts)' : ''}',
      );
    }
    if (resource.ipmi == true) add('IPMI');
    if (resource.managed != null) {
      add(resource.managed! ? 'Managed' : 'Unmanaged');
    }
    if (resource.poe != null) add(resource.poe! ? 'PoE' : 'No PoE');
    if (resource.speed != null) {
      add('${resource.speed!.toStringAsFixed(0)} Gbps');
    }
    if (resource.va != null) add('${resource.va} VA');
    add(resource.description);
    add(resource.type);
    add(resource.os);
    if (resource.cores != null) add('${resource.cores} cores');
    if (resource.ramGb != null) {
      add('RAM ${resource.ramGb!.toStringAsFixed(0)} GB');
    }
    add(resource.ip);
    final network = resource.network?.describe();
    if (resource.endpoints.isNotEmpty) {
      for (final endpoint in resource.endpoints) {
        final description = endpoint.describe();
        if (description.isNotEmpty) {
          final protocol = endpoint.protocol;
          final protocolSuffix = protocol == null ? '' : ' ($protocol)';
          add('Published endpoint: $description$protocolSuffix');
        }
      }
    } else if (network != null && network.isNotEmpty) {
      add(network);
    }

    for (final cpu in resource.cpus) {
      add('CPU: ${cpu.describe()}');
    }
    for (final drive in resource.drives) {
      add('Drive: ${drive.describe()}');
    }
    for (final gpu in resource.gpus) {
      add('GPU: ${gpu.describe()}');
    }
    for (final port in resource.ports) {
      add('Ports: ${port.describe()}');
    }

    if (facts.isEmpty) {
      return const Text('No details yet.', style: ScribeType.metadata);
    }

    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [for (final fact in facts) NoteFact(text: fact)],
    );
  }

  Widget _linksPanel(String title, List<String> names, BuildContext context) {
    return SurfacePanel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SectionLabel(title),
          const SizedBox(height: 8),
          for (final linkName in names)
            InkWell(
              onTap: () => Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) =>
                      ResourceDetailScreen(store: store, name: linkName),
                ),
              ),
              child: Padding(
                padding: const EdgeInsets.symmetric(vertical: 6),
                child: Row(
                  children: [
                    const Icon(
                      CupertinoIcons.link,
                      size: 16,
                      color: ScribeColors.accentText,
                    ),
                    const SizedBox(width: 8),
                    Text(
                      linkName,
                      style: ScribeType.body.copyWith(
                        color: ScribeColors.accentText,
                      ),
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }
}
