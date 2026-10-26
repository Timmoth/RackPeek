# Versioning

RackPeek uses [Semantic Versioning](https://semver.org/): `MAJOR.MINOR.PATCH`.

- **Major** (`X.0.0`): incompatible CLI, API, or inventory format changes.
- **Minor** (`2.X.0`): backward-compatible features.
- **Patch** (`2.3.X`): backward-compatible fixes.

The CLI, web app, and Docker image share a release version. The mobile app uses the same marketing version; its build number is incremented for each store upload.

## Docker tags

- `aptacode/rackpeek:latest` points to the newest stable release.
- `aptacode/rackpeek:vX.Y.Z` pins an exact release and is recommended for production.
- `aptacode/rackpeek:nightly` follows the `staging` branch for testing.

Use a versioned tag when you need repeatable deployments. Nightly builds can change without notice and are intended for testing.
