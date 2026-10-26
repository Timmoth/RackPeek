# Developer Setup

RackPeek requires the .NET 10 SDK. Docker is required for web UI end-to-end tests. The mobile app also requires Flutter; build iOS apps on macOS with Xcode.

## Install tools

- Install [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) for your operating system.
- Install [Docker Desktop](https://docs.docker.com/get-started/get-docker/) or Docker Engine. Start the daemon before building images or running E2E tests.
- Install [`just`](https://github.com/casey/just#installation) to use the repository's build and test recipes.
- For mobile work, install [Flutter](https://docs.flutter.dev/get-started/install) and the platform tools for your target device.

The repository includes a [devcontainer](../../.devcontainer) with the .NET and Docker development tools configured.

## Check the setup

From the repository root:

```sh
dotnet --version
docker info
just --list
just build
```

Run the fast test projects with `just test-cli`, `just test-discovery`, and `just test-mcp`. Run browser tests with `just test-e2e`; the first run also needs Playwright's browser install:

```sh
just e2e-setup
```

Repeat `just e2e-setup` after upgrading Microsoft.Playwright so the installed browser matches its pinned version.

## Mobile app

From `mobile/`:

```sh
flutter pub get
flutter run
flutter test
flutter analyze
```

Use Xcode for iOS signing and device builds. See the [mobile README](../../mobile/README.md) for store build commands.
