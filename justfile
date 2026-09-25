# RackPeek Development Commands
# Run `just` or `just --list` to see available recipes.

# Environment Variables
# ---------------------

# Add .dotnet/tools to PATH for Playwright CLI
export PATH := env_var('HOME') + "/.dotnet/tools:" + env_var_or_default('PATH', '')


# Variables
# ---------
_dotnet := "dotnet"
_dockerfile := "RackPeek.Web/Dockerfile"
_image := "rackpeek:ci"
_setup_guide := "docs/development/dev-setup.md"

# ─── Helpers/Private ────────────────────────────────────────────────────────

[doc("Check if dotnet is installed, show setup guide redirect if not found.")]
[private]
_check-dotnet:
    @command -v {{ _dotnet }} >/dev/null 2>&1 || (echo "dotnet not found. See {{ _setup_guide }} for setup instructions." && exit 1)

# ─── Default ────────────────────────────────────────────────────────────────

[default]
[doc("List all recipes with documentation")]
[private]
default:
    @just --list --justfile {{ justfile() }}

# ─── Build ──────────────────────────────────────────────────────────────────

[doc("Build the full solution (Debug)")]
[group("build")]
build: _check-dotnet
    {{ _dotnet }} build RackPeek.sln

[doc("Build the full solution in Release mode")]
[group("build")]
build-release: _check-dotnet
    {{ _dotnet }} build RackPeek.sln -c Release

[doc("Publish CLI as self-contained single-file binary")]
[group("build")]
build-cli runtime="linux-x64": _check-dotnet
    {{ _dotnet }} publish RackPeek/RackPeek.csproj -c Release -r {{ runtime }} \
        --self-contained true \
        -p:PublishSingleFile=true

[doc("Build Web Docker image (required before E2E tests)")]
[group("build")]
build-web:
    docker build -t {{ _image }} -f {{ _dockerfile }} .

# ─── Test ───────────────────────────────────────────────────────────────────

[doc("Run CLI tests (fast; no Docker required)")]
[group("test")]
test-cli: _check-dotnet
    {{ _dotnet }} test Tests/Tests.csproj

[doc("Run discovery tests (fast; no Docker required; matches the discovery-tests CI job)")]
[group("test")]
test-discovery: _check-dotnet
    {{ _dotnet }} test Tests.Discovery

[doc("Install Playwright + browsers for E2E (first-time only)")]
[group("test")]
e2e-setup: _check-dotnet
    cd Tests.E2e && {{ _dotnet }} tool install --global Microsoft.Playwright.CLI
    cd Tests.E2e && {{ _dotnet }} build
    cd Tests.E2e && playwright install

[doc("Run E2E tests (depends on build-web; run e2e-setup once)")]
[group("test")]
test-e2e: _check-dotnet build-web
    cd Tests.E2e && {{ _dotnet }} test

[doc("Run CLI + discovery + E2E tests (rebuilds Web image)")]
[group("test")]
test-all: _check-dotnet build-web e2e-setup test-cli test-discovery test-e2e

[doc("Run full test suite (alias for test-all; matches CI / pre-PR checklist)")]
[group("test")]
ci: test-all

# ─── Demo ───────────────────────────────────────────────────────────────────

[doc("Generate CLI demo with VHS (needs: vhs, imagemagick, chrome)")]
[group("demo")]
build-cli-demo:
    cd vhs && vhs ./rpk.tape

[doc("Capture Web UI demo as GIF (needs: Chrome, ImageMagick)")]
[group("demo")]
build-web-demo:
    cd vhs && chmod +x webui_capture.sh && ./webui_capture.sh

# ─── Release ────────────────────────────────────────────────────────────────

[doc("Build and push multi-arch Docker image to registry")]
[group("release")]
docker-push version:
    docker buildx build \
        --platform linux/amd64,linux/arm64 \
        -f {{ _dockerfile }} \
        -t aptacode/rackpeek:{{ version }} \
        -t aptacode/rackpeek:latest \
        --push .

# ─── Run ────────────────────────────────────────────────────────────────────

[doc("Run the docker container")]
[group("run")]
run-docker: _check-dotnet
    docker build -t {{ _image }} -f {{ _dockerfile }} .
    docker run -d -p 8080:8080 {{ _image }}

[doc("Use the locally built CLI")]
[group("run")]
rpk *args: _check-dotnet
    ./RackPeek/bin/Debug/net10.0/RackPeek {{ args }}

# ─── Utility ────────────────────────────────────────────────────────────────

[doc("Clean build artifacts (bin, obj)")]
[group("utility")]
clean: _check-dotnet
    {{ _dotnet }} clean RackPeek.sln
