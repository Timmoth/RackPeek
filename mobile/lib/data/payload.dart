/// Builds the minimal `POST /api/inventory` document for a partial update.
/// Merge mode treats omitted/null/empty fields as "don't touch", so only
/// changed values are sent.
Map<String, dynamic> buildUpsertPayload({
  required int version,
  required List<Map<String, dynamic>> resources,
}) {
  return {
    'version': version,
    'resources': resources,
  };
}

Map<String, dynamic> resourcePatch({
  required String kind,
  required String name,
  Map<String, Object?> fields = const {},
  List<String>? tags,
  Map<String, String>? labels,
  String? notes,
  List<String>? runsOn,
}) {
  final patch = <String, Object?>{'kind': kind, 'name': name};
  patch.addAll(fields);
  if (tags != null) patch['tags'] = tags;
  if (labels != null) patch['labels'] = labels;
  if (notes != null) patch['notes'] = notes;
  if (runsOn != null) patch['runsOn'] = runsOn;
  return patch;
}
