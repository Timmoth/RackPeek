
# Dev Cheat Sheet


## Build CLI (macOS ARM64)

Publish a self-contained single binary:

```bash
dotnet publish -c Release -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

Install globally:

```bash
sudo mv bin/Release/net*/osx-arm64/publish/RackPeek /usr/local/bin/rpk
sudo chmod +x /usr/local/bin/rpk
```

Verify:

```bash
rpk --help
```

---

## Generate CLI Demo (VHS)

Install tools:

```bash
brew install vhs
brew install imagemagick
brew install --cask google-chrome # if not already installed
```

Run tape:

```bash
cd vhs
vhs ./rpk.tape
```

---

## Capture Web UI Demo

Make script executable:

```bash
chmod +x webui_capture.sh
```

Run:

```bash
cd vhs
npm install
node capture.js 
./webui_capture.sh
```

> Requires Chrome + ImageMagick.

---

## Build & Push Multi-Arch Docker Image

Manual release build:

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -f ./Dockerfile \
  -t aptacode/rackpeek:v0.0.11 \
  -t aptacode/rackpeek:latest \
  --push ..
```

Notes:

* Uses `buildx` for multi-arch
* Pushes directly to registry
* Update version tag before running

---

## Run E2E Tests (Playwright)

Install tooling (first time only):

```bash
cd Tests.E2e
dotnet tool install --global Microsoft.Playwright.CLI
dotnet build
playwright install
```

Build Web image (required before running tests):

```bash
docker build -t rackpeek:ci -f RackPeek.Web/Dockerfile .
```

---

## Common Workflows

### Rebuild everything quickly

```bash
docker build -t rackpeek:ci -f RackPeek.Web/Dockerfile .
cd Tests.E2e && dotnet test
```

---

## Debugging E2E Tests

Temporarily set in `PlaywrightFixture.cs`:

```csharp
Headless = false,
SlowMo = 500,
```

⚠ Revert to:

```csharp
Headless = true,
```

Before committing (CI requires headless mode).