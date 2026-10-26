# Testing Guidelines

Test observable behavior at system boundaries. Prefer integration and end-to-end tests over tests coupled to private implementation details. Every bug fix should include a regression test when practical.

## Test projects

| Project | Coverage |
|---|---|
| `Tests/` | CLI workflows, API endpoints, YAML schemas and migrations, persistence, and domain integration |
| `Tests.Discovery/` | Discovery parsers, clients, and resource mapping |
| `Tests.Mcp/` | MCP protocol and tool behavior against the server |
| `Tests.E2e/` | Browser workflows against the Docker web app using Playwright |
| `mobile/` | Flutter widget, model, storage, sync, and offline behavior |

## Run tests

From the repository root:

```sh
just test-cli
just test-discovery
just test-mcp
just test-e2e
just ci
```

E2E tests need Docker and Playwright browsers. Run `just e2e-setup` once and repeat after upgrading Microsoft.Playwright. For mobile tests, run `flutter test` and `flutter analyze` from `mobile/`.

## Test design

- Assert results users can observe: output, HTTP responses, UI behavior, and persisted YAML.
- Make tests independent, repeatable, and self-cleaning. Generate unique names for shared test data.
- Keep browser tests headless. Use page objects for selectors and user-level actions.
- Cover valid migrations and failure cases. A migration test should verify preserved data, the resulting schema version, and the original backup where the app migrates a file.
- Test offline mobile edits across restart, then verify upload, discard, and retry behavior when connectivity returns.
- Avoid brittle assertions on styling or private implementation details.

## Ready for review

Before marking a PR ready, run formatting and the relevant test projects. Use `just ci` for the full backend suite, plus `flutter test` and `flutter analyze` when mobile code changes. Confirm no debug settings remain enabled and CI passes.
