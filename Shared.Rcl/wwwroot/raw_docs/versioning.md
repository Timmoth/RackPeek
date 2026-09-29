## Versioning

RackPeek follows **Semantic Versioning (SemVer)** for both the CLI and Docker images:

```
MAJOR.MINOR.PATCH
Example: 1.2.3
```

### MAJOR (X.0.0)

Breaking changes:

* CLI command/flag changes
* `config.yaml` structure changes
* Removed/renamed fields
* Behavior requiring migration

### MINOR (1.X.0)

Backward-compatible features:

* New CLI commands or flags
* New WebUI features
* New non-breaking config options
* Performance improvements

### PATCH (1.0.X)

Backward-compatible fixes:

* Bug fixes
* Security patches
* Docs / minor UX improvements

## CLI & Docker

The CLI binary and Docker image share the **same version number**.

Docker tags:

* `latest` → newest stable release
* `v1.2.3` → exact immutable version (recommended for production)

For production, pin to a specific version instead of `latest`.

## Nightly Docker Builds

In addition to stable releases, RackPeek publishes a **nightly Docker image** from the `staging` branch.

* Triggered on every push to `staging`
* Built for `linux/amd64`
* Tagged as:

    * `aptacode/rackpeek:nightly`
    * `aptacode/rackpeek:nightly-<short-sha>`

Nightly images provide early access to upcoming changes and are intended for testing and development (not production
use).
