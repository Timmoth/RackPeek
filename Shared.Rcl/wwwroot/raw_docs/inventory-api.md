# Inventory API

RackPeek exposes a small HTTP API for upserting inventory from scripts and
external systems — useful for automatic ingestion from Ansible, Terraform,
discovery scanners, or any other tool that can produce a YAML or JSON
description of your infrastructure.

The API is served by the same process as the Web UI, on the same port.

## What you need

1. **The server URL** — wherever RackPeek is running, e.g.
   `http://localhost:8080`.
2. **An API key** — a shared secret you choose, set via the `RPK_API_KEY`
   environment variable on the server. Requests without a matching key
   are rejected.

## Configuring the API key

Set `RPK_API_KEY` on the RackPeek container or process. Until you set it,
the inventory endpoint returns `503 Service Unavailable` for every request.

| Variable | Required | Description |
|---|---|---|
| `RPK_API_KEY` | Yes (to use the API) | Shared secret. Clients must send this in the `X-Api-Key` header. |

Example with Docker Compose:

```yaml
version: "3.9"

services:
  rackpeek:
    image: aptacode/rackpeek:latest
    container_name: rackpeek
    ports:
      - "8080:8080"
    volumes:
      - rackpeek-config:/app/config
    environment:
      - RPK_API_KEY=change-me-to-a-long-random-string
    restart: unless-stopped

volumes:
  rackpeek-config:
```

Pick a long random value (e.g. `openssl rand -hex 32`). The server compares
keys in constant time, so timing-based guessing is not viable, but a weak
key still loses to brute force — treat it like a password.

## Endpoints

### `GET /health`

Liveness check. No authentication. Returns `200 OK` with body `rackpeek` as
`text/plain`. Used by the Docker healthcheck.

```bash
curl http://localhost:8080/health
# rackpeek
```

### `POST /api/inventory`

Upsert resources and connections. Requires the `X-Api-Key` header.

#### Request body

| Field | Type | Required | Description |
|---|---|---|---|
| `yaml` | string | one of `yaml` or `json` | Raw YAML payload, same schema as `config.yaml`. |
| `json` | object | one of `yaml` or `json` | Structured JSON payload (see shape below). |
| `mode` | `"Merge"` \| `"Replace"` | No (default `Merge`) | How incoming resources combine with existing ones. |
| `dryRun` | bool | No (default `false`) | Compute the diff and return it without writing to disk. |

Exactly one of `yaml` or `json` must be supplied — sending both is rejected.

#### Merge semantics

For resources whose `name` doesn't already exist, the resource is added.
For resources whose `name` already exists:

- **`Merge`** — fields present in the incoming payload overwrite the existing
  values; fields that are omitted, `null`, empty lists, or empty maps in the
  incoming payload are left untouched. Use this for partial updates.
- **`Replace`** — the incoming resource fully replaces the existing one,
  even for omitted fields. Use this when the incoming payload is the
  authoritative full picture.

If an incoming resource has a different `kind` than the existing one with
the same name (e.g. server → switch), the existing resource is fully
replaced regardless of mode.

#### JSON payload shape

The `json` field accepts the same shape as `config.yaml` — a root object
with `version`, `resources`, and optional `connections`. The canonical
schemas live in [`schemas/`](https://github.com/Timmoth/RackPeek/tree/main/schemas)
in the repository.

```json
{
  "version": 3,
  "resources": [
    {
      "kind": "Server",
      "name": "web-01",
      "tags": ["homelab", "prod"],
      "labels": { "env": "production" },
      "ipmi": true,
      "ram": { "size": 64 }
    }
  ],
  "connections": []
}
```

The `kind` discriminator is case-sensitive. Use the exact value for each
resource type: `Server`, `Switch`, `Router`, `Firewall`, `AccessPoint`,
`Ups`, `Desktop`, `Laptop`, `Other`, `Service`, `System`.

#### Response

`200 OK` with a diff summary:

| Field | Description |
|---|---|
| `added` | Names of resources that were newly created. |
| `updated` | Names of existing resources whose fields changed under `Merge` mode. |
| `replaced` | Names of resources fully overwritten (either `Replace` mode, or a kind mismatch). |
| `oldYaml` | YAML snapshot of each updated/replaced resource **before** the change, keyed by name. |
| `newYaml` | YAML snapshot of each incoming resource **after** the change, keyed by name. |

`oldYaml` is omitted for newly-added resources (nothing existed to snapshot).

#### Error responses

| Status | When |
|---|---|
| `400 Bad Request` | Validation failure — neither `yaml` nor `json` supplied, both supplied, malformed payload, duplicate resource names, etc. Body: `{ "error": "..." }`. |
| `401 Unauthorized` | `X-Api-Key` header missing or doesn't match `RPK_API_KEY`. |
| `503 Service Unavailable` | `RPK_API_KEY` is not configured on the server. |

### Read endpoints

All read endpoints require the same `X-Api-Key` header and return
`401`/`503` exactly as above.

| Endpoint | Returns |
|---|---|
| `GET /api/inventory` | The full inventory as a JSON document: `{ "version", "resources", "connections" }`, same shape the `json` field above accepts. Fetch once, render lists/details/trees client-side. |
| `GET /api/inventory/summary` | Counts: `hardware` (total + by kind), `systems` (total + by type + by OS), `services` (total + IP addresses), `tags`, `labels`. |
| `GET /api/inventory/search?q=<text>&max=<n>` | Ranked matches over names, IPs, tags and labels: `{ "matches": [{ "name", "kind", "url", "matchedField", "matchedValue", "score" }] }`. `max` defaults to 8. |
| `GET /api/inventory/tree` | The containment forest: `{ "hardware": [{ "hardwareName", "kind", "systems": [{ "systemName", "services" }] }] }`. |

The document from `GET /api/inventory` round-trips: send it back to
`POST /api/inventory` as the `json` field (optionally with `dryRun: true`)
and the server reports no changes. Every resource carries the correct
case-sensitive `kind` discriminator, so clients never have to know the
`AccessPoint`/`Accesspoint` spelling difference.

```bash
curl -s -H "X-Api-Key: $RPK_API_KEY" http://localhost:8080/api/inventory | jq '.resources | length'
```

## Examples

### Upsert from a YAML file with curl

```bash
curl -X POST http://localhost:8080/api/inventory \
  -H "X-Api-Key: $RPK_API_KEY" \
  -H "Content-Type: application/json" \
  -d "$(jq -Rs --arg mode Merge '{yaml: ., mode: $mode}' < inventory.yaml)"
```

### Upsert a single resource as JSON

```bash
curl -X POST http://localhost:8080/api/inventory \
  -H "X-Api-Key: $RPK_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "mode": "Merge",
    "json": {
      "version": 3,
      "resources": [
        {
          "kind": "Server",
          "name": "web-01",
          "tags": ["homelab"],
          "ram": { "size": 64 }
        }
      ]
    }
  }'
```

### Preview a change with `dryRun`

```bash
curl -X POST http://localhost:8080/api/inventory \
  -H "X-Api-Key: $RPK_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "dryRun": true,
    "mode": "Replace",
    "json": {
      "version": 3,
      "resources": [
        { "kind": "Server", "name": "web-01", "ram": { "size": 128 } }
      ]
    }
  }'
```

The response shows what would change without modifying `config.yaml`.

### Bash helper

```bash
rpk_ingest() {
  local file="$1"
  curl -fsS -X POST "${RPK_URL:-http://localhost:8080}/api/inventory" \
    -H "X-Api-Key: $RPK_API_KEY" \
    -H "Content-Type: application/json" \
    -d "$(jq -Rs '{yaml: ., mode: "Merge"}' < "$file")"
}

rpk_ingest inventory.yaml
```

## Tips

- Run with `dryRun: true` first when wiring up a new pipeline — the response
  tells you exactly what would change.
- Prefer `Merge` when your script only knows about a subset of fields (e.g.
  a discovery scanner that only sees CPU/RAM but doesn't know about
  labels/tags). Use `Replace` when the script owns the full resource.
- The `tags` array (a list) is replaced wholesale by any non-empty incoming
  value, even in `Merge` mode — if you want to add a single tag without
  overwriting the others, fetch the resource first or use the CLI
  (`rpk <kind> tag add ...`).
- The `labels` map (a dictionary) is merged key-wise in `Merge` mode —
  existing keys absent from the incoming payload are preserved; keys
  present in the incoming payload overwrite the existing value.
- The same YAML you commit to Git via the Git integration is what the API
  reads and writes — there is no separate API store.
