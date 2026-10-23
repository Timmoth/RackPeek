import 'package:flutter/material.dart';

import '../design.dart';
import '../models/inventory.dart';
import '../state/inventory_store.dart';

class AddResourceScreen extends StatefulWidget {
  const AddResourceScreen({super.key, required this.store});

  final InventoryStore store;

  @override
  State<AddResourceScreen> createState() => _AddResourceScreenState();
}

class _AddResourceScreenState extends State<AddResourceScreen> {
  static const _kinds = [
    'Server', 'Switch', 'Router', 'Firewall', 'AccessPoint', 'Desktop',
    'Laptop', 'Ups', 'Other', 'System', 'Service',
  ];

  String _kind = 'Server';
  final _name = TextEditingController();
  String? _parent;
  String? _error;

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  List<Resource> get _parentCandidates {
    final all = widget.store.document?.resources ?? [];
    if (_kind == 'System') {
      return all.where((c) => c.isHardware || c.kind == 'System').toList();
    }
    if (_kind == 'Service') {
      return all.where((c) => c.kind == 'System').toList();
    }
    return const [];
  }

  bool get _needsParent => _kind == 'System' || _kind == 'Service';

  Future<void> _create() async {
    final name = _name.text.trim();
    setState(() => _error = null);

    if (name.isEmpty) {
      setState(() => _error = 'A name is required.');
      return;
    }
    if (name.length > 50) {
      setState(() => _error = 'Names are limited to 50 characters.');
      return;
    }
    if (widget.store.document?.byName(name) != null) {
      setState(() => _error = 'A resource named "$name" already exists.');
      return;
    }
    if (_needsParent && _parent == null) {
      setState(() => _error = 'Pick what this runs on.');
      return;
    }

    final resource = <String, Object?>{
      'kind': _kind,
      'name': name,
    };
    if (_needsParent && _parent != null) resource['runsOn'] = [_parent];

    try {
      await widget.store.addResource(resource);
      if (!mounted) return;
      Navigator.of(context).pop();
    } catch (e) {
      setState(() => _error = e.toString());
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: ScribeColors.canvas,
        elevation: 0,
        scrolledUnderElevation: 0,
        title: const Text('Add resource'),
      ),
      body: ScribeSheet(
        children: [
          SurfacePanel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SectionLabel('Kind'),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 12,
                  runSpacing: 12,
                  children: [
                    for (final kind in _kinds)
                      InkWell(
                        onTap: () => setState(() => _kind = kind),
                        borderRadius: BorderRadius.circular(10),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            CategoryMark(
                              icon: Resource(kind: kind, name: '').glyph,
                              tint: _kind == kind
                                  ? Colors.white
                                  : ScribeColors.accent,
                              soft: _kind == kind
                                  ? ScribeColors.accent
                                  : ScribeColors.accentSoft,
                            ),
                            const SizedBox(height: 4),
                            Text(kind, style: ScribeType.metadata),
                          ],
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),
          SurfacePanel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SectionLabel('Name'),
                const SizedBox(height: 8),
                TextField(
                  controller: _name,
                  autocorrect: false,
                  decoration: const InputDecoration(hintText: 'nas01'),
                ),
                if (_needsParent) ...[
                  const SizedBox(height: ScribeSpacing.majorSectionGap),
                  SectionLabel(_kind == 'Service' ? 'Runs on (system)' : 'Runs on'),
                  const SizedBox(height: 8),
                  DropdownButtonFormField<String>(
                    initialValue: _parent,
                    decoration: const InputDecoration(hintText: 'Choose a parent'),
                    items: [
                      for (final candidate in _parentCandidates)
                        DropdownMenuItem(
                          value: candidate.name,
                          child: Text(candidate.name),
                        ),
                    ],
                    onChanged: (v) => setState(() => _parent = v),
                  ),
                ],
              ],
            ),
          ),
          if (_error != null) InlineNotice(message: _error!, isError: true),
          ScribeAction(
            label: 'Create',
            busy: widget.store.busy,
            onPressed: _create,
          ),
        ],
      ),
    );
  }
}
