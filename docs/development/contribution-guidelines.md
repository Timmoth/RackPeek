# Contribution Guidelines

## Before you code

1. Search the [issue tracker](https://github.com/Timmoth/RackPeek/issues) for existing work.
2. For a new feature or behavior change, open an issue and agree on the approach before implementation.
3. Keep pull requests focused. Open them as drafts until the work and its tests are ready.

## Before marking a PR ready

- Run `dotnet format RackPeek.sln --verify-no-changes`.
- Run the relevant CLI, Discovery, MCP, and E2E tests. Use `just ci` for the full suite.
- Add regression coverage for changed behavior.
- For YAML changes, include a forward migration, schema update, and migration tests.
- Remove temporary diagnostics and verify Playwright runs headless.

Target feature work at `staging`; releases go from `staging` to `main`. See the [release guide](release-guide.md) for publishing.
