import 'package:flutter/material.dart';

import '../design.dart';
import '../models/inventory.dart';
import '../state/inventory_store.dart';
import 'resource_detail_screen.dart';

class TreeTab extends StatelessWidget {
  const TreeTab({super.key, required this.store});

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
    final document = store.document;
    if (document == null) return const ScribeSpinner();

    final roots = document.resources
        .where((r) => r.isHardware || (r.runsOn.isEmpty))
        .toList()
      ..sort((a, b) => a.name.compareTo(b.name));

    if (roots.isEmpty) {
      return const ScribeSpinner();
    }

    return RefreshIndicator(
      onRefresh: store.refresh,
      child: ScribeSheet(
        children: [
          const Text('Containment', style: ScribeType.hero),
          SurfacePanel(
            padding: const EdgeInsets.symmetric(
                vertical: 8, horizontal: ScribeSpacing.panelPadding),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                for (final root in roots)
                  _Branch(
                    store: store,
                    resource: root,
                    depth: 0,
                    ancestors: const {},
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Branch extends StatelessWidget {
  const _Branch({
    required this.store,
    required this.resource,
    required this.depth,
    required this.ancestors,
  });

  final InventoryStore store;
  final Resource resource;
  final int depth;
  final Set<String> ancestors;

  @override
  Widget build(BuildContext context) {
    final nextAncestors = {...ancestors, resource.name.toLowerCase()};
    final children = (store.document?.dependantsOf(resource.name) ?? [])
        .where((c) => !nextAncestors.contains(c.name.toLowerCase()))
        .toList()
      ..sort((a, b) => a.name.compareTo(b.name));

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        InkWell(
          onTap: () => Navigator.of(context).push(MaterialPageRoute(
            builder: (_) =>
                ResourceDetailScreen(store: store, name: resource.name),
          )),
          child: Padding(
            padding: EdgeInsets.only(left: depth * 16.0, top: 8, bottom: 8),
            child: Row(
              children: [
                CategoryMark(icon: resource.glyph),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(resource.name, style: ScribeType.title),
                ),
              ],
            ),
          ),
        ),
        for (final child in children)
          _Branch(
            store: store,
            resource: child,
            depth: depth + 1,
            ancestors: nextAncestors,
          ),
      ],
    );
  }
}
