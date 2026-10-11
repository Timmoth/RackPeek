# Release Guide

How to cut a RackPeek release, end to end. The flow is:
features → `staging` (nightly Docker auto-publishes) → `main` → tagged release.

Everything below happens on `staging` until the "Merge & publish" step.

---

## 1. Pre-release checks

- [ ] `staging` CI is green.
- [ ] Full suite passes locally: `just ci` (CLI + discovery + MCP + E2E; rebuilds the Web image).
- [ ] Review the release diff: `git diff origin/main...origin/staging --stat`.

## 2. Bump the version

Pick the new version per [versioning.md](../../Shared.Rcl/wwwroot/raw_docs/versioning.md) (SemVer).

The version is declared in **three places** — all must agree:

| File | Format |
|------|--------|
| `RackPeek.Domain/RpkConstants.cs` (`Version`) | `v2.2.0` — feeds `rpk --version`, the web UI header chip, and MCP `ServerInfo` |
| `RackPeek/RackPeek.csproj` (`<AssemblyVersion>`) | `2.2.0` |
| `README.md` version badge | `2.2.0` |

Also update the version-adjacent extras:

- [ ] `Shared.Rcl/wwwroot/raw_docs/install-guide.md` — pinned release download URLs (tag `RackPeek-X.Y.Z`, asset `rackpeek_X_Y_Z_<rid>`).
- [ ] `.github/workflows/publish-cli.yml` and `publish-docker.yaml` — `workflow_dispatch` version defaults.

## 3. Regenerate the CLI docs

```bash
rm -rf RackPeek/publish   # REQUIRED: the script reuses a stale binary if one exists
./generate-docs.sh
```

> ⚠ `generate-docs.sh` only publishes the CLI when `RackPeek/publish/RackPeek` is
> missing. Skipping the `rm` regenerates docs from whatever binary is lying
> around — this has silently dropped new commands before.

Verify:

- [ ] `git diff Shared.Rcl/wwwroot/raw_docs/cli-commands.md` — only expected command changes.
- [ ] `grep -c '^## ' Shared.Rcl/wwwroot/raw_docs/cli-commands.md` matches the header count in `cli-commands-index.md`.

## 4. Docs freshness pass

- [ ] New user-facing features are mentioned in `README.md` and `Shared.Rcl/wwwroot/raw_docs/overview.md`.
- [ ] New doc pages are listed in `Shared.Rcl/wwwroot/raw_docs/docs-index.json` and linked from the README Docs section.
- [ ] `resource-levels.md` sub-resource matrix reflects any new kinds/sub-resources.

Commit everything to `staging` and let CI run.

## 5. Merge & publish

1. Open a PR `staging` → `main`, wait for CI, merge.
2. From `main`, dispatch the publish workflows (Actions tab → Run workflow). **The
   version input formats differ per workflow:**

| Workflow | Version input | Produces |
|----------|---------------|----------|
| `publish-cli.yml` | `2.2.0` (no `v`) | **Draft** GitHub Release tagged `RackPeek-2.2.0` with `rackpeek_2_2_0_{win-x64,linux-x64,linux-arm64,osx-x64,osx-arm64}` binaries |
| `publish-docker.yaml` | `v2.2.0` (`v` required, regex-validated) | `aptacode/rackpeek:v2.2.0` + `:latest` (multi-arch) |
| `publish-webui.yml` | — | Redeploys the GitHub Pages demo + docs viewer |

3. Edit the draft GitHub Release: write the release notes (features, fixes,
   breaking changes/migrations), then publish it.

## 6. Post-release verification

- [ ] `docker pull aptacode/rackpeek:v2.2.0 && docker run --rm aptacode/rackpeek:v2.2.0 rpk --version` → `v2.2.0`.
- [ ] Web UI header shows the new version; `/mcp` still returns 503 without `RPK_API_KEY`.
- [ ] Release-page binary download runs (`chmod +x`, `./rackpeek --version`).
- [ ] Docs site reflects the new pages: https://timmoth.github.io/RackPeek/docs/overview

## Gotchas

- The **nightly** Docker image (`:nightly`, `:nightly-<sha>`) builds automatically
  from every push to `staging` — it is not part of the release flow and is not
  gated on tests.
- `workflow_dispatch` runs against the branch you select in the Actions UI —
  make sure it's `main` for a release.
- The publish workflows do not read the in-repo version; the operator-typed
  input is authoritative. Double-check it matches `RpkConstants.Version`.
