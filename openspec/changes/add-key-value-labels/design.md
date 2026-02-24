# Design: Add Key-Value Labels to Resources

## Context

RackPeek models IT infrastructure as a hierarchy of Resources (Hardware → System → Service). Resources currently have `Name`, `Tags` (string array), `Notes`, and `RunsOn`. Tags are single-value strings used for categorization (e.g., "production", "staging"); they are managed via `AddTagUseCase` / `RemoveTagUseCase` and displayed in `ResourceTagEditor`. Tags have no CLI commands today—they are Web UI only.

Labels differ from Tags: they are key-value pairs (e.g., `env: production`, `owner: team-a`). Users need arbitrary metadata for organization, filtering, and reporting. The project overview and code-style rules already anticipate Labels (`Dictionary<string, string>` on `Resource`), but the implementation does not exist yet. `KeyValueModal` exists in Shared.Rcl and can be reused for label add/edit in the Web UI.

## Goals / Non-Goals

**Goals:**

- Add `Labels` property (`Dictionary<string, string>`) to `Resource` base class.
- Implement `AddLabelUseCase` and `RemoveLabelUseCase` following the Tags pattern.
- Persist labels in YAML; existing resources without labels deserialize as empty dict.
- Expose add/remove label commands in CLI for each resource branch (servers, switches, etc.).
- Display and edit labels in the Web UI via a `ResourceLabelEditor` component (reusing `KeyValueModal`).

**Non-Goals:**

- Label-based filtering or querying in CLI/Web (future enhancement).
- Label validation beyond key/value presence and length limits.
- Bulk label operations or import/export.

## Decisions

### 1. Data model: `Dictionary<string, string>` on Resource

**Decision:** Add `Labels` as `Dictionary<string, string>` on `Resource`, initialized to `new()` (never null).

**Rationale:** Matches Kubernetes-style labels and the project's code-style rules. Simple, serializable, and supports arbitrary key-value pairs.

**Alternatives considered:** `List<(string, string)>` — rejected because lookup by key is common and Dictionary is more ergonomic. `Dictionary<string, string?>` — rejected; empty string is acceptable for value.

### 2. Use case pattern: Mirror Tags

**Decision:** Create `AddLabelUseCase<T>` and `RemoveLabelUseCase<T>` in `UseCases/Labels/`, following the same structure as `AddTagUseCase` / `RemoveTagUseCase`.

**Rationale:** Consistency with existing patterns; DI registration via open generics; single responsibility per use case.

**Alternatives considered:** Single `LabelUseCase` with Add/Remove methods — rejected per SRP. Reusing Tag use cases with a "mode" — rejected; labels have different semantics (key-value vs single value).

### 3. Normalization and validation

**Decision:** Normalize key and value with `Trim()`. Validate: key and value non-empty, key length ≤ 50, value length ≤ 200. Add `ThrowIfInvalid.LabelKey` and `ThrowIfInvalid.LabelValue` (or similar) helpers.

**Rationale:** Prevents whitespace-only labels; keeps YAML readable; avoids unbounded storage.

**Alternatives considered:** No length limits — rejected for YAML size and UX. Lowercase keys — rejected; user may want case-sensitive keys (e.g., `Env` vs `env`).

### 4. CLI structure: `label add` / `label remove` per resource branch

**Decision:** Add a `label` sub-branch under each resource branch (servers, switches, systems, etc.) with `add` and `remove` commands. Example: `rpk servers label add web-01 --key env --value prod`, `rpk servers label remove web-01 --key env`.

**Rationale:** Matches the E2E example workflow; consistent with `cpu add`, `drive add` sub-branch pattern.

**Alternatives considered:** Global `label add <resource-type> <name> ...` — rejected; less discoverable. Positional args only — rejected; `--key`/`--value` improve clarity for key-value semantics.

### 5. YAML serialization

**Decision:** Rely on YamlDotNet default serialization for `Dictionary<string, string>`. No custom converter unless ordering or format requires it.

**Rationale:** YamlDotNet serializes `Dictionary<string, string>` as YAML mapping. Existing resources without `Labels` will deserialize with default `new()`; migration not required if property is initialized.

**Alternatives considered:** Custom converter for key ordering — deferred; can add later if needed. Schema version bump — only if migration logic is required; adding a new optional property typically does not require it.

### 6. Web UI: ResourceLabelEditor component

**Decision:** Create `ResourceLabelEditor` in Shared.Rcl, mirroring `ResourceTagEditor`. Use `KeyValueModal` for add/edit. Display labels as key-value chips with remove button; add button opens modal.

**Rationale:** Reuses existing `KeyValueModal`; consistent UX with Tags; shared across all resource types via `@typeparam TResource`.

## Risks / Trade-offs

| Risk | Mitigation |
|------|-------------|
| YAML files from older app versions lack `Labels` | Initialize to empty dict in Resource; YamlDotNet will populate or leave default. Verify deserialization of legacy YAML. |
| Duplicate label commands across 10+ resource branches | Use shared command base or factory; CliBootstrap will have repetitive but explicit registration (consistent with existing pattern). |
| Key/value injection in YAML (e.g., special chars) | YamlDotNet handles escaping; validate key/value to reject control characters if needed. |
| Labels bloat YAML file size | Length limits (50/200) mitigate; bulk operations out of scope. |

## Migration Plan

- **Deploy:** No schema version bump required if `Labels` is an additive, optional property. Existing YAML without `labels:` will deserialize with empty dictionary.
- **Rollback:** Revert code; YAML with `labels:` will be ignored by older app (YamlDotNet typically ignores unknown properties). If strict compatibility is required, add migration step to strip labels when downgrading.

## Open Questions

- Should `describe` output format labels differently from tags (e.g., `Labels: env=prod, owner=team-a` vs `Tags: production, staging`)? **Recommendation:** Yes; use `key: value` format for labels.
- Should we support label filtering in `list`/`get` in this change? **Recommendation:** No; keep scope to add/remove and display.
