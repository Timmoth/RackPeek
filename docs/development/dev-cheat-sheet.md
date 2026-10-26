# Developer Cheat Sheet

Run these commands from the repository root.

## Build and run

```sh
just build                 # Build the solution
just run-docker            # Build and run the web app at localhost:8080
just build-cli osx-arm64   # Publish a macOS Apple Silicon CLI binary
```

Use `just build-cli linux-x64`, `linux-arm64`, `osx-x64`, or `win-x64` for other CLI targets.

## Tests

```sh
just test-cli
just test-discovery
just test-mcp
just test-e2e              # Builds rackpeek:ci and runs Playwright tests
just ci                    # Full CI test workflow
```

Install Playwright and its browser before the first E2E run with `just e2e-setup`. Repeat after upgrading Microsoft.Playwright.

## Mobile

```sh
cd mobile
flutter pub get
flutter run
flutter test
flutter analyze
```

## Demos

`just build-cli-demo` records the CLI with VHS. `just build-web-demo` captures the web UI. Both require their external capture tools; see the recipe output for missing dependencies.

For versioning, release builds, and publishing steps, follow the [release guide](release-guide.md). `just docker-push <version>` publishes directly to Docker Hub.
