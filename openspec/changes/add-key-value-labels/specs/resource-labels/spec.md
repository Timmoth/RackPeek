# Spec: Resource Labels

## ADDED Requirements

### Requirement: Resource stores labels as key-value pairs

Each resource SHALL have a Labels property that stores arbitrary string key-value pairs. Labels SHALL be initialized to an empty collection and MUST NOT be null.

#### Scenario: New resource has empty labels

- **WHEN** a resource is created
- **THEN** the resource has an empty Labels collection

#### Scenario: Labels are stored per resource

- **WHEN** a label with key "env" and value "production" is added to resource "web-01"
- **THEN** resource "web-01" has Labels containing "env" -> "production"
- **AND** other resources are unaffected

### Requirement: User can add a label to a resource

The system SHALL allow users to add a label (key-value pair) to any resource via CLI and Web UI.

#### Scenario: Add label via CLI

- **WHEN** user runs `rpk servers label add web-01 --key env --value production`
- **THEN** resource "web-01" has label "env" with value "production"
- **AND** the change is persisted to YAML

#### Scenario: Add label via Web UI

- **WHEN** user opens the resource card, clicks add label, enters key "owner" and value "team-a", and submits
- **THEN** the resource has label "owner" with value "team-a"
- **AND** the change is persisted

#### Scenario: Add label to existing key overwrites value

- **WHEN** resource "web-01" has label "env" with value "staging"
- **AND** user adds label "env" with value "production"
- **THEN** resource "web-01" has label "env" with value "production"

### Requirement: User can remove a label from a resource

The system SHALL allow users to remove a label by key from any resource via CLI and Web UI.

#### Scenario: Remove label via CLI

- **WHEN** resource "web-01" has label "env" with value "production"
- **AND** user runs `rpk servers label remove web-01 --key env`
- **THEN** resource "web-01" no longer has label "env"
- **AND** the change is persisted to YAML

#### Scenario: Remove label via Web UI

- **WHEN** resource has label "owner" with value "team-a"
- **AND** user clicks remove on that label in the resource card
- **THEN** the resource no longer has label "owner"
- **AND** the change is persisted

#### Scenario: Remove nonexistent key is no-op

- **WHEN** resource "web-01" does not have label "env"
- **AND** user runs `rpk servers label remove web-01 --key env`
- **THEN** no error occurs
- **AND** the resource is unchanged

### Requirement: Labels persist in YAML

Labels SHALL be serialized and deserialized from the YAML resource file. Existing resources without a labels section SHALL deserialize with an empty Labels collection.

#### Scenario: Labels are written to YAML

- **WHEN** resource "web-01" has labels "env: production" and "owner: team-a"
- **AND** the resource is saved
- **THEN** the YAML file contains a labels section for that resource with the key-value pairs

#### Scenario: Legacy YAML without labels deserializes correctly

- **WHEN** resource in YAML has no labels section
- **THEN** the resource deserializes with an empty Labels collection

### Requirement: Labels are displayed in resource views

The system SHALL display labels in resource describe output (CLI) and resource cards (Web UI).

#### Scenario: Describe shows labels

- **WHEN** resource "web-01" has labels "env: production" and "owner: team-a"
- **AND** user runs `rpk servers describe web-01`
- **THEN** the output includes the labels in key-value format

#### Scenario: Resource card shows labels

- **WHEN** resource has label "env" with value "production"
- **AND** user views the resource card in Web UI
- **THEN** the label is displayed

### Requirement: Label key and value are validated

The system SHALL validate label key and value before adding. Key and value MUST be non-empty after trimming. Key length MUST NOT exceed 50 characters. Value length MUST NOT exceed 200 characters.

#### Scenario: Empty key is rejected

- **WHEN** user attempts to add a label with empty or whitespace-only key
- **THEN** a validation error is returned
- **AND** the resource is not updated

#### Scenario: Empty value is rejected

- **WHEN** user attempts to add a label with empty or whitespace-only value
- **THEN** a validation error is returned
- **AND** the resource is not updated

#### Scenario: Key exceeds length limit is rejected

- **WHEN** user attempts to add a label with key longer than 50 characters
- **THEN** a validation error is returned
- **AND** the resource is not updated

#### Scenario: Value exceeds length limit is rejected

- **WHEN** user attempts to add a label with value longer than 200 characters
- **THEN** a validation error is returned
- **AND** the resource is not updated

#### Scenario: Add label for nonexistent resource fails

- **WHEN** user attempts to add a label to a resource that does not exist
- **THEN** a not-found error is returned
- **AND** no change is persisted
