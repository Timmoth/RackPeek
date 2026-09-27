# RackPeek — Agent Guide

This document is the entry point for AI agents (Claude Code, OpenCode, etc.) working in this repo. It captures everything needed to understand the codebase, make a focused change, validate it, and open a PR without re-reading the whole tree.

---

## 1. What RackPeek is

RackPeek is a **CLI + Web UI for documenting and managing home-lab / small-scale IT infrastructure** (servers, switches, routers, firewalls, access points, UPS units, desktops, laptops, other hardware, systems, and services).

- All state is persisted to a **single YAML file** (`config/config.yaml`) — no database.
- Same domain code powers the CLI binary (`rpk`) and the Blazor Server Web UI.
- Distributed as a Docker image (`aptacode/rackpeek`) and a self-contained CLI binary.
- Live demo: <https://timmoth.github.io/RackPeek/> · Docs: <https://timmoth.github.io/RackPeek/docs/overview>

### Core values (these shape design decisions)

- **Simplicity** — narrow scope, no enterprise CMDB features.
- **Openness** — open YAML format, user owns their data.
- **Privacy** — no telemetry, no tracking.
- **Dogfooding** — features must be useful to real home-labs.
- **Opinionated** — built for home labs, not corporate documentation.

If a proposed change conflicts with these values, push back before implementing.

---

## 2. Tech stack

| Layer | Tech |
|---|---|
| Language | C# (.NET **10.0**, `net10.0` TFM) |
| CLI | [Spectre.Console.Cli](https://spectreconsole.net/) |
| Web UI | Blazor Server (`Microsoft.NET.Sdk.Web`) + a WASM viewer (`RackPeek.Web.Viewer`) for the live demo |
| Persistence | YAML (`YamlDotNet`, `DocMigrator.Yaml`) — single file |
| Git integration | `LibGit2Sharp` (optional, used when `GIT_TOKEN` is set) |
| CLI tests | xUnit + `Spectre.Console.Testing` + `JsonSchema.Net` |
| E2E tests | xUnit + `Microsoft.Playwright` + `Testcontainers` (spins up the real Docker image) |
| Build runner | [`just`](https://github.com/casey/just) |
| Container | `mcr.microsoft.com/dotnet/aspnet:10.0` — exposes port 8080 |
| Code style | `dotnet format` (CI gate) + `.editorconfig` |
| Analysis | `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, latest analyzers (see `Directory.Build.props`) |

**.NET 10 is required.** If `dotnet --version` shows < 10, see `docs/development/dev-setup.md`. A devcontainer is included (`.devcontainer/`).

---

## 3. Solution layout

```
RackPeek.sln
├── RackPeek/                  CLI entry point (Spectre.Console.Cli) → produces `rpk`
├── RackPeek.Domain/           Core domain: resources, use-cases, persistence, git
│   ├── Resources/             Resource models (Server, Switch, System, Service, …)
│   ├── UseCases/              Generic use-cases (Add, Delete, Rename, Cpus, Drives, Gpus, Ports, Labels, Tags, Ansible, SSH, Hosts)
│   ├── Persistence/           IResourceCollection, Yaml repositories, migrations
│   │   └── Yaml/              YamlResourceCollection, RackPeekConfigMigrationDeserializer, ResourceYamlMigrationService
│   ├── Git/                   Optional LibGit2Sharp integration (NullGitRepository when disabled)
│   ├── Api/                   InventoryRequest/Response + UpsertInventoryUseCase (used by Web API)
│   └── ServiceCollectionExtensions.cs   DI: AddUseCases / AddYamlRepos / AddGitServices
├── Shared.Rcl/                Razor Class Library: Blazor components AND CLI command wiring shared between Web + CLI
│   ├── Commands/              Spectre.Console.Cli command classes (one folder per resource kind)
│   ├── Components/            Shared Razor components
│   ├── Modals/, Layout/, Services/, Console/
│   ├── CliBootstrap.cs        Registers all CLI commands + DI internals (single source of truth for the `rpk` command tree)
│   └── ConsoleRunner.cs       Lets the Web UI execute CLI commands in-process
├── RackPeek.Web/              Blazor Server host (Dockerfile lives here)
├── RackPeek.Web.Viewer/       Blazor WebAssembly viewer (powers the github-pages demo)
├── Tests/                     CLI integration tests (xUnit) — fast, no Docker
│   ├── EndToEnd/              Per-resource workflow tests using the real CLI
│   ├── Api/                   Web API endpoint tests (Microsoft.AspNetCore.Mvc.Testing)
│   ├── TestConfigs/v1,v2,v3/  Fixture YAML files for migration tests
│   └── schemas/               JSON schemas validated against output
└── Tests.E2e/                 Playwright + Testcontainers — spins up the Docker image and drives the Web UI
    ├── PageObjectModels/      One POM per page/component (required pattern)
    └── Infra/PlaywrightFixture.cs   Container + browser lifecycle
```

### Where to put new code

| You're adding… | Goes in… |
|---|---|
| A new CLI subcommand | `Shared.Rcl/Commands/<ResourceKind>/…` + register it in `Shared.Rcl/CliBootstrap.cs` |
| A new resource kind | `RackPeek.Domain/Resources/<Kind>/` model, register in `Resource.cs` maps, add YAML migration, wire repos in `ServiceCollectionExtensions.cs`, add Razor pages under `Shared.Rcl/<Kind>/`, add Web routing |
| A new use-case | `RackPeek.Domain/UseCases/` — implement `IUseCase` (auto-registered by reflection in `AddUseCases`) or the generic `IResourceUseCase<T>` |
| A new Razor component used by CLI+Web | `Shared.Rcl/Components/` |
| A new Web page only | `RackPeek.Web/Components/` |
| A YAML schema change | Bump schema version under `schemas/vN/` + add migration in `RackPeek.Domain/Persistence/Yaml/` + add migration test in `Tests/TestConfigs/vN/` |

---

## 4. Build, test, run

All workflow commands go through `justfile`. Prefer `just <target>` over running `dotnet` directly so behaviour stays consistent with CI.

### Build

```bash
just build              # dotnet build RackPeek.sln (Debug)
just build-release      # Release
just build-cli          # publish self-contained single-file binary (default linux-x64)
just build-cli linux-arm64    # cross-target
just build-web          # docker build -t rackpeek:ci -f RackPeek.Web/Dockerfile .
```

### Test

```bash
just test-cli           # fast CLI tests, no Docker required
just e2e-setup          # ONCE: installs Playwright CLI + browsers
just test-e2e           # implies build-web; runs Playwright suite
just test-all           # = build-web + e2e-setup + test-cli + test-e2e
just ci                 # alias for test-all — matches the pre-PR checklist
```

CI order (`.github/workflows/test.yml`):

1. **`format`** → `dotnet format --verify-no-changes` (runs on `ubuntu-latest`)
2. **`cli-tests`** → `dotnet test Tests` (runs on `ubuntu-latest`, depends on format)
3. **`webui-tests`** → builds the docker image then runs `dotnet test Tests.E2e` (runs on `ubuntu-24.04`, depends on cli-tests)

Always run `dotnet format` before commit — formatting breaks CI first.

### Run

```bash
just run-docker         # build + run container on http://localhost:8080
just rpk [args]         # run CLI directly from Debug build
just clean              # dotnet clean
```

### Release

```bash
just docker-push 1.3.2  # multi-arch (linux/amd64, linux/arm64) push to aptacode/rackpeek
```

CLI binary version is bumped in `RackPeek/RackPeek.csproj` (`<AssemblyVersion>`).

### Demos (rarely needed by agents)

```bash
just build-cli-demo     # VHS recording — needs vhs, imagemagick, chrome
just build-web-demo     # GIF capture — needs Chrome, ImageMagick
```

---

## 5. Code style

Enforced by CI via `dotnet format --verify-no-changes`. From `.editorconfig` + `Directory.Build.props`:

- 4-space indent, LF line endings, final newline, UTF-8.
- `var` for built-in types and when the type is apparent; explicit type otherwise.
- Expression-bodied members only when on a single line.
- Private fields are `_camelCase` (underscore prefix, error severity).
- Open braces on a new line (Allman) — `csharp_new_line_before_open_brace = all:error`.
- **Warnings are errors** repo-wide. Don't introduce nullable warnings or analyzer warnings.
- Nullable reference types enabled in every project (`<Nullable>enable</Nullable>`).

Default to writing no comments. The project favours readable names + tests-as-documentation.

---

## 6. Persistence model (important)

There is **one YAML file**: `config/config.yaml` (or the path given by `RPK_YAML_DIR` env var; the Docker image sets it to `/app/config`).

Top-level shape:

```yaml
resources:
  - kind: Server | Switch | Firewall | Router | Accesspoint | Desktop | Laptop | Ups | Other | System | Service
    name: <unique name within kind>
    tags: [...]
    labels: { key: value }
    notes: |
      free-form markdown
    runsOn: [<parent-resource-name>, ...]   # only meaningful for System / Service
    # kind-specific fields follow (e.g. ports[], cpus[], drives[], gpus[], nics[], network, ram, …)
```

Key invariants (see `RackPeek.Domain/Resources/Resource.cs`):

- `name` is the identity within a `kind`. Don't introduce numeric IDs.
- `runsOn` relationships are validated by `Resource.CanRunOn<T>`:
  - `Service` may run on a `System`.
  - `System` may run on hardware (`Server`, `Switch`, `Firewall`, `Router`, `Accesspoint`, `Desktop`, `Laptop`, `Ups`, `Other`) or on another `System`.
- "Hardware" is the umbrella term for the nine physical kinds above (`Resource.IsHardware`).
- Anything that mutates the YAML must go through an `IResourceUseCase<T>` → `IResourceCollection` → repository, never direct file writes.

### YAML migrations

Schemas are versioned under `schemas/v1`, `schemas/v2`, `schemas/v3`. Migration code lives in `RackPeek.Domain/Persistence/Yaml/`:

- `RackPeekConfigMigrationDeserializer.cs` — deserialisation entry point
- `ResourceYamlMigrationService.cs` — applies the version chain

When you change persisted YAML shape, the PR **must** include:

1. A new `schemas/vN+1/schema.vN+1.json`.
2. A forward migration that reads vN and emits vN+1.
3. Test fixtures under `Tests/TestConfigs/vN+1/` (note the explicit `<None Update>` entries in `Tests/Tests.csproj` if you add new files).
4. Backwards compatibility for at least vN, OR a clearly documented breaking change.

---

## 7. CLI surface

The full command tree is documented in `Shared.Rcl/wwwroot/raw_docs/cli-commands.md` and `Shared.Rcl/wwwroot/raw_docs/cli-commands-index.md` (auto-generated by `generate-docs.sh`). At a glance:

```
rpk <kind> <verb> [name] [flags]

kinds:   summary, servers, switches, routers, firewalls, systems,
         accesspoints, ups, desktops, laptops, other, services
verbs:   summary, add, list, get, describe, set, del, tree
sub:     cpu, drive, gpu, nic, port, subnets, labels, tags, rename, …
```

When adding/altering commands, regenerate the docs (`./generate-docs.sh`) so the published reference stays in sync.

---

## 8. Environment variables

| Var | Default | Purpose |
|---|---|---|
| `RPK_YAML_DIR` | `config` (CLI) / `/app/config` (Docker) | Directory containing `config.yaml` |
| `GIT_TOKEN` | unset | If set, enables `LibGit2GitRepository` for the config dir |
| `GIT_USERNAME` | `git` | Username paired with `GIT_TOKEN` |
| `ASPNETCORE_URLS` | `http://+:8080` (Docker) | Web UI bind |

---

## 9. Testing principles

Read `docs/development/testing-guidelines.md` in full before touching tests. Highlights:

- **Test at the edges.** Black-box integration tests over micro-mocked unit tests. If a refactor breaks a test without changing observable behaviour, the test was too coupled.
- **CLI tests** (`Tests/`) drive the real `CommandApp`, assert exact stdout, and inspect the YAML written to disk. Use the `ExecuteAsync(...)` helper pattern.
- **E2E tests** (`Tests.E2e/`) use Testcontainers to run the real Docker image then drive the Web UI via Playwright. Every page has a Page Object Model (POM) in `Tests.E2e/PageObjectModels/`. Tests should read like workflows, not browser scripts.
- E2E tests must be **independent, idempotent, and self-cleaning** — generate unique names with `Guid.NewGuid()` and delete what you create.
- Treat every bug as a missing test: reproduce with a failing test, then fix.
- Fix flakiness immediately; don't retry.

### Adding a feature checklist

- [ ] CLI test covering happy + at least one unhappy path (output + YAML side-effect)
- [ ] E2E test for the corresponding Web UI flow (if there is one)
- [ ] YAML migration + migration test (if persisted shape changed)
- [ ] `dotnet format` clean
- [ ] `just ci` green locally

---

## 10. Pull-request workflow

From `docs/development/contribution-guidelines.md`:

1. **Find / open a GitHub issue first.** Validate approach with maintainers before coding (issues > Discord for design discussion).
2. Keep PRs **small and focused** — one concern per PR.
3. Open as **Draft**; move to Ready only when:
   - All tests pass locally (`just ci`)
   - Scope is complete
   - No debug code left in (especially `Headless = false` in `PlaywrightFixture.cs`)
4. Pre-PR checklist (mirror in PR body):
   - [ ] Linked GitHub issue
   - [ ] Approach validated
   - [ ] Small, focused PR
   - [ ] CLI tests passing locally
   - [ ] E2E tests passing locally
   - [ ] Behaviour covered by tests
   - [ ] YAML migration defined (if persisted shape changed)

Default branches: feature work targets `staging`; releases flow `staging → main`.

---

## 11. Gotchas

- **E2E tests require the Docker image.** `just test-e2e` rebuilds it via `just build-web`. If you change anything in `RackPeek.Web`, `RackPeek.Domain`, or `Shared.Rcl`, the image must be rebuilt before E2E runs.
- **Playwright browsers** are installed once via `just e2e-setup`. In CI they're cached under `~/.cache/ms-playwright`.
- **Bumping the `Microsoft.Playwright` package invalidates the browser cache.** Each Playwright version pins a specific Chromium build (e.g. 1.58 → `chromium_headless_shell-1208`, 1.59 → `-1217`). After bumping, every E2E test fails fast with `PlaywrightException : Executable doesn't exist at .../chromium_headless_shell-NNNN`. Re-run `just e2e-setup` (or `~/.dotnet/tools/playwright install chromium`) to download the matching build before running the suite.
- **Docker image tag** is `rackpeek:ci` locally (referenced by `Tests.E2e/Infra/PlaywrightFixture.cs:9`); the registry tag is `aptacode/rackpeek`.
- **Debugging E2E**: temporarily set `Headless = false, SlowMo = 1500` in `Tests.E2e/Infra/PlaywrightFixture.cs`. **Always revert before commit** — CI requires headless.
- **TreatWarningsAsErrors** — a stray `unused-variable` warning fails the whole build. Don't add `#pragma warning disable` to push through; fix the warning.
- **Git integration** is optional and silently no-ops when `GIT_TOKEN` is absent (`NullGitRepository`). Don't assume git is wired up.
- **Single YAML file**: concurrent writes from CLI + Web are not coordinated beyond file replacement. Treat the Web UI as the source of truth while it's running.
- The `RackPeek.Web/config copy/` directory looks like cruft but is checked-in — leave it alone unless cleaning up is the explicit goal.
- The Web Docker image bundles **both** the Web app and the CLI binary (`rpk` is placed in `/usr/local/bin`). You can `docker exec rackpeek rpk ...` against a running container.

---

## 12. Reference

| Path | What |
|---|---|
| `justfile` | Single source of truth for developer commands |
| `RackPeek.sln` | Solution root |
| `Directory.Build.props` | Repo-wide MSBuild props (analyzers, warnings-as-errors) |
| `.editorconfig` | Formatting + naming rules |
| `.github/workflows/test.yml` | CI pipeline (format → cli-tests → webui-tests) |
| `.github/workflows/publish-*.yml` | Release pipelines |
| `RackPeek.Web/Dockerfile` | Multi-stage build for the runtime image |
| `RackPeek/Program.cs` | CLI entry point |
| `RackPeek.Web/Program.cs` | Web entry point + DI wiring |
| `Shared.Rcl/CliBootstrap.cs` | Master CLI command registration |
| `RackPeek.Domain/ServiceCollectionExtensions.cs` | Domain DI registration |
| `RackPeek.Domain/Resources/Resource.cs` | Resource base + kind/relationship rules |
| `docs/development/contribution-guidelines.md` | PR process |
| `docs/development/dev-cheat-sheet.md` | Build / release / Docker / Playwright details |
| `docs/development/dev-setup.md` | First-time environment setup |
| `docs/development/release-guide.md` | End-to-end release process (version bump, docs regen, publish workflows) |
| `docs/development/testing-guidelines.md` | Testing philosophy + examples |
| `Shared.Rcl/wwwroot/raw_docs/cli-commands.md` / `cli-commands-index.md` | Auto-generated CLI reference |
| `schemas/v1,v2,v3/` | Versioned YAML schemas |
| `README.md` | User-facing overview, Docker install, links |
| `LICENSE` | License terms |
