# Proposal: Add Key-Value Labels to Resources

## Why

The user would like the ability to add arbitrary labels to each resource. This helps the user associate key-value information on their resources (e.g., environment, owner, cost-center) for organization, filtering, and reporting.

## What Changes

- Addition of Labels (key-value section) to each resource
- Add/Remove label use cases for managing labels via CLI and Web UI
- YAML persistence of labels on resources
- Display and editing of labels in the Web UI

## Capabilities

### New Capabilities

- `resource-labels`: User can associate key-value attributes to their various resources in RackPeek. Labels are arbitrary string key-value pairs stored on each resource, with add/remove operations exposed via CLI and Web UI.

### Modified Capabilities

- None (no existing specs in `openspec/specs/`)

## Impact

- **Domain**: `Resource` base class gains `Labels` property (`Dictionary<string, string>`); new `AddLabelUseCase` and `RemoveLabelUseCase` in `UseCases/Labels/`
- **Persistence**: YAML schema must serialize/deserialize labels; existing resources without labels remain valid (empty dict)
- **CLI**: New commands for adding/removing labels per resource type (e.g., `rpk servers add-label <name> --key <key> --value <value>`, `rpk servers remove-label <name> --key <key>`)
- **Web UI**: Resource card and edit flows must display and allow editing of labels (e.g., `ResourceLabelEditor` component)
