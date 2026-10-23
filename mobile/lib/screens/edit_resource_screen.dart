import 'package:flutter/material.dart';

import '../data/payload.dart';
import '../design.dart';
import '../models/inventory.dart';
import '../state/inventory_store.dart';

class EditResourceScreen extends StatefulWidget {
  const EditResourceScreen({
    super.key,
    required this.store,
    required this.resource,
  });

  final InventoryStore store;
  final Resource resource;

  @override
  State<EditResourceScreen> createState() => _EditResourceScreenState();
}

class _EditResourceScreenState extends State<EditResourceScreen> {
  late final TextEditingController _model;
  late final TextEditingController _description;
  late final TextEditingController _ramSize;
  late final TextEditingController _ramMts;
  late final TextEditingController _os;
  late final TextEditingController _cores;
  late final TextEditingController _systemRam;
  late final TextEditingController _ip;
  late final TextEditingController _port;
  late final TextEditingController _protocol;
  late final TextEditingController _url;
  late final TextEditingController _speed;
  late final TextEditingController _va;
  late final TextEditingController _notes;
  late final TextEditingController _newTag;

  late bool? _ipmi;
  late bool? _managed;
  late bool? _poe;
  late String? _type;
  late List<String> _tags;
  late Map<String, String> _labels;
  late List<String> _runsOn;

  String? _error;

  static const _systemTypes = [
    'baremetal', 'hypervisor', 'vm', 'container',
    'embedded', 'cloud', 'cluster', 'other',
  ];

  @override
  void initState() {
    super.initState();
    final r = widget.resource;
    _model = TextEditingController(text: r.model ?? '');
    _description = TextEditingController(text: r.description ?? '');
    _ramSize = TextEditingController(
        text: r.ram?.size != null ? r.ram!.size!.toStringAsFixed(0) : '');
    _ramMts = TextEditingController(text: r.ram?.mts?.toString() ?? '');
    _os = TextEditingController(text: r.os ?? '');
    _cores = TextEditingController(text: r.cores?.toString() ?? '');
    _systemRam = TextEditingController(
        text: r.ramGb != null ? r.ramGb!.toStringAsFixed(0) : '');
    _ip = TextEditingController(text: r.ip ?? r.network?.ip ?? '');
    _port = TextEditingController(text: r.network?.port?.toString() ?? '');
    _protocol = TextEditingController(text: r.network?.protocol ?? '');
    _url = TextEditingController(text: r.network?.url ?? '');
    _speed = TextEditingController(
        text: r.speed != null ? r.speed!.toStringAsFixed(0) : '');
    _va = TextEditingController(text: r.va?.toString() ?? '');
    _notes = TextEditingController(text: r.notes ?? '');
    _newTag = TextEditingController();

    _ipmi = r.ipmi;
    _managed = r.managed;
    _poe = r.poe;
    _type = r.type;
    _tags = List.of(r.tags);
    _labels = Map.of(r.labels);
    _runsOn = List.of(r.runsOn);
  }

  @override
  void dispose() {
    for (final controller in [
      _model, _description, _ramSize, _ramMts, _os, _cores, _systemRam,
      _ip, _port, _protocol, _url, _speed, _va, _notes, _newTag,
      _newLabelKey, _newLabelValue,
    ]) {
      controller.dispose();
    }
    super.dispose();
  }

  Map<String, Object?> _scalarFields() {
    final r = widget.resource;
    final fields = <String, Object?>{};

    void maybeString(String key, TextEditingController controller, String? original) {
      final value = controller.text.trim();
      if (value.isEmpty) return;
      if (value != (original ?? '')) fields[key] = value;
    }

    void maybeInt(String key, TextEditingController controller, int? original) {
      final value = int.tryParse(controller.text.trim());
      if (value == null || value == original) return;
      fields[key] = value;
    }

    void maybeDouble(String key, TextEditingController controller, double? original) {
      final value = double.tryParse(controller.text.trim());
      if (value == null || value == original) return;
      fields[key] = value;
    }

    switch (r.kind) {
      case 'Server':
        maybeString('model', _model, r.model);
        final serverRam = _ramPatch();
        if (serverRam != null) fields['ram'] = serverRam;
        if (_ipmi != r.ipmi && _ipmi != null) fields['ipmi'] = _ipmi;
      case 'Desktop' || 'Laptop':
        maybeString('model', _model, r.model);
        final portableRam = _ramPatch();
        if (portableRam != null) fields['ram'] = portableRam;
      case 'Switch' || 'Router' || 'Firewall':
        maybeString('model', _model, r.model);
        if (_managed != r.managed && _managed != null) fields['managed'] = _managed;
        if (_poe != r.poe && _poe != null) fields['poe'] = _poe;
      case 'AccessPoint':
        maybeString('model', _model, r.model);
        maybeDouble('speed', _speed, r.speed);
      case 'Ups':
        maybeString('model', _model, r.model);
        maybeInt('va', _va, r.va);
      case 'Other':
        maybeString('model', _model, r.model);
        maybeString('description', _description, r.description);
      case 'System':
        if (_type != null && _type != r.type) fields['type'] = _type;
        maybeString('os', _os, r.os);
        maybeInt('cores', _cores, r.cores);
        maybeDouble('ram', _systemRam, r.ramGb);
        maybeString('ip', _ip, r.ip);
        if (!_listEquals(_runsOn, r.runsOn)) fields['runsOn'] = _runsOn;
      case 'Service':
        final network = <String, Object?>{};
        maybeNetwork(network);
        if (network.isNotEmpty) fields['network'] = network;
        if (!_listEquals(_runsOn, r.runsOn)) fields['runsOn'] = _runsOn;
    }
    return fields;
  }

  Map<String, Object?>? _ramPatch() {
    final original = widget.resource.ram;
    final size = double.tryParse(_ramSize.text.trim());
    final mts = int.tryParse(_ramMts.text.trim());
    final ram = <String, Object?>{};
    if (size != null && size != original?.size) ram['size'] = size;
    if (mts != null && mts != original?.mts) ram['mts'] = mts;
    return ram.isEmpty ? null : ram;
  }

  void maybeNetwork(Map<String, Object?> network) {
    final original = widget.resource.network;
    final ip = _ip.text.trim();
    final port = int.tryParse(_port.text.trim());
    final protocol = _protocol.text.trim();
    final url = _url.text.trim();
    if (ip.isNotEmpty && ip != original?.ip) network['ip'] = ip;
    if (port != null && port != original?.port) network['port'] = port;
    if (protocol.isNotEmpty && protocol != original?.protocol) {
      network['protocol'] = protocol;
    }
    if (url.isNotEmpty && url != original?.url) network['url'] = url;
  }

  static bool _listEquals(List<String> a, List<String> b) {
    if (a.length != b.length) return false;
    for (var i = 0; i < a.length; i++) {
      if (a[i] != b[i]) return false;
    }
    return true;
  }

  Future<void> _save() async {
    setState(() => _error = null);

    final fields = _scalarFields();
    final tagsChanged = !_listEquals(_tags, widget.resource.tags);
    final labelsChanged = !_mapsEqual(_labels, widget.resource.labels);
    final notesChanged = _notes.text != (widget.resource.notes ?? '');

    if (fields.isEmpty && !tagsChanged && !labelsChanged && !notesChanged) {
      Navigator.of(context).pop();
      return;
    }

    try {
      await widget.store.applyPatch(resourcePatch(
        kind: widget.resource.kind,
        name: widget.resource.name,
        fields: fields,
        tags: tagsChanged ? _tags : null,
        labels: labelsChanged ? _labels : null,
        notes: notesChanged ? _notes.text : null,
      ));
      if (!mounted) return;
      Navigator.of(context).pop();
    } catch (e) {
      setState(() => _error = e.toString());
    }
  }

  static bool _mapsEqual(Map<String, String> a, Map<String, String> b) {
    if (a.length != b.length) return false;
    for (final entry in a.entries) {
      if (b[entry.key] != entry.value) return false;
    }
    return true;
  }

  @override
  Widget build(BuildContext context) {
    final r = widget.resource;

    return Scaffold(
      appBar: AppBar(
        backgroundColor: ScribeColors.canvas,
        elevation: 0,
        scrolledUnderElevation: 0,
        title: Text('Edit ${r.name}'),
      ),
      body: ScribeSheet(
        children: [
          if (_hasScalars) ...[
            SurfacePanel(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const SectionLabel('Details'),
                  const SizedBox(height: ScribeSpacing.relatedGap),
                  ..._scalarInputs(),
                ],
              ),
            ),
          ],
          _tagsPanel(),
          _labelsPanel(),
          SurfacePanel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SectionLabel('Notes'),
                const SizedBox(height: ScribeSpacing.relatedGap),
                TextField(
                  controller: _notes,
                  minLines: 3,
                  maxLines: 10,
                  decoration: const InputDecoration(
                    hintText: 'Markdown, as in the web UI',
                  ),
                ),
              ],
            ),
          ),
          if (_error != null) InlineNotice(message: _error!, isError: true),
          ScribeAction(
            label: 'Save',
            busy: widget.store.busy,
            onPressed: _save,
          ),
          const Text(
            'Merge semantics: empty fields are left untouched on the server. '
            'Deleting a resource or clearing all tags is done from the web UI.',
            style: ScribeType.metadata,
          ),
        ],
      ),
    );
  }

  bool get _hasScalars => const {
        'Server', 'Desktop', 'Laptop', 'Switch', 'Router', 'Firewall',
        'AccessPoint', 'Ups', 'Other', 'System', 'Service',
      }.contains(widget.resource.kind);

  List<Widget> _scalarInputs() {
    switch (widget.resource.kind) {
      case 'Server':
        return [
          _field(_ramSize, 'RAM (GB)', TextInputType.number),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_ramMts, 'RAM speed (MT/s)', TextInputType.number),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _triSwitch('IPMI', _ipmi, (v) => setState(() => _ipmi = v)),
        ];
      case 'Desktop' || 'Laptop':
        return [
          _field(_model, 'Model'),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_ramSize, 'RAM (GB)', TextInputType.number),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_ramMts, 'RAM speed (MT/s)', TextInputType.number),
        ];
      case 'Switch' || 'Router' || 'Firewall':
        return [
          _field(_model, 'Model'),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _triSwitch('Managed', _managed, (v) => setState(() => _managed = v)),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _triSwitch('PoE', _poe, (v) => setState(() => _poe = v)),
        ];
      case 'AccessPoint':
        return [
          _field(_model, 'Model'),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_speed, 'Speed (Gbps)', TextInputType.number),
        ];
      case 'Ups':
        return [
          _field(_model, 'Model'),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_va, 'VA', TextInputType.number),
        ];
      case 'Other':
        return [
          _field(_model, 'Model'),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_description, 'Description'),
        ];
      case 'System':
        return [
          DropdownButtonFormField<String>(
            initialValue: _type,
            decoration: const InputDecoration(labelText: 'Type'),
            items: [
              for (final t in _systemTypes)
                DropdownMenuItem(value: t, child: Text(t)),
            ],
            onChanged: (v) => setState(() => _type = v),
          ),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_os, 'OS'),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_cores, 'Cores', TextInputType.number),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_systemRam, 'RAM (GB)', TextInputType.number),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_ip, 'IP'),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _runsOnPicker(),
        ];
      case 'Service':
        return [
          _field(_ip, 'IP'),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_port, 'Port', TextInputType.number),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_protocol, 'Protocol'),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _field(_url, 'URL'),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _runsOnPicker(),
        ];
      default:
        return [];
    }
  }

  Widget _runsOnPicker() {
    final candidates = (widget.store.document?.resources ?? [])
        .where((c) {
          if (c.name == widget.resource.name) return false;
          if (widget.resource.kind == 'System') {
            return c.isHardware || c.kind == 'System';
          }
          return c.kind == 'System';
        })
        .toList();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const SectionLabel('Runs on'),
        const SizedBox(height: 8),
        for (final candidate in candidates)
          CheckboxListTile(
            contentPadding: EdgeInsets.zero,
            controlAffinity: ListTileControlAffinity.leading,
            value: _runsOn.contains(candidate.name),
            title: Text(candidate.name, style: ScribeType.body),
            onChanged: (checked) => setState(() {
              if (checked == true) {
                _runsOn = [candidate.name];
              } else {
                _runsOn = [];
              }
            }),
          ),
      ],
    );
  }

  Widget _tagsPanel() {
    return SurfacePanel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const SectionLabel('Tags'),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (final tag in _tags)
                InputChip(
                  label: Text(tag),
                  onDeleted: () => setState(() => _tags.remove(tag)),
                ),
            ],
          ),
          const SizedBox(height: ScribeSpacing.relatedGap),
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _newTag,
                  decoration: const InputDecoration(hintText: 'Add tag'),
                  onSubmitted: (_) => _addTag(),
                ),
              ),
              const SizedBox(width: 8),
              IconButton(
                tooltip: 'Add tag',
                onPressed: _addTag,
                icon: const Icon(Icons.add_circle),
                color: ScribeColors.accent,
              ),
            ],
          ),
        ],
      ),
    );
  }

  void _addTag() {
    final tag = _newTag.text.trim();
    if (tag.isEmpty || _tags.contains(tag)) return;
    setState(() {
      _tags.add(tag);
      _newTag.clear();
    });
  }

  Widget _labelsPanel() {
    return SurfacePanel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const SectionLabel('Labels'),
          const SizedBox(height: 8),
          for (final entry in _labels.entries)
            Row(
              children: [
                Expanded(child: Text('${entry.key}: ${entry.value}', style: ScribeType.body)),
                IconButton(
                  tooltip: 'Remove label ${entry.key}',
                  icon: const Icon(Icons.close),
                  color: ScribeColors.tertiaryText,
                  onPressed: () => setState(() => _labels.remove(entry.key)),
                ),
              ],
            ),
          const SizedBox(height: ScribeSpacing.relatedGap),
          _labelAdder(),
        ],
      ),
    );
  }

  final TextEditingController _newLabelKey = TextEditingController();
  final TextEditingController _newLabelValue = TextEditingController();

  Widget _labelAdder() {
    return Row(
      children: [
        Expanded(
          child: TextField(
            controller: _newLabelKey,
            decoration: const InputDecoration(hintText: 'Key'),
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: TextField(
            controller: _newLabelValue,
            decoration: const InputDecoration(hintText: 'Value'),
          ),
        ),
        const SizedBox(width: 8),
        IconButton(
          tooltip: 'Add label',
          icon: const Icon(Icons.add_circle),
          color: ScribeColors.accent,
          onPressed: () {
            final key = _newLabelKey.text.trim();
            final value = _newLabelValue.text.trim();
            if (key.isEmpty || value.isEmpty) return;
            setState(() {
              _labels[key] = value;
              _newLabelKey.clear();
              _newLabelValue.clear();
            });
          },
        ),
      ],
    );
  }

  Widget _field(TextEditingController controller, String label,
      [TextInputType type = TextInputType.text]) {
    return TextField(
      controller: controller,
      keyboardType: type,
      decoration: InputDecoration(labelText: label),
    );
  }

  Widget _triSwitch(String label, bool? value, ValueChanged<bool?> onChanged) {
    return SwitchListTile(
      contentPadding: EdgeInsets.zero,
      title: Text(label, style: ScribeType.body),
      value: value ?? false,
      onChanged: onChanged,
      activeThumbColor: ScribeColors.accent,
    );
  }
}
