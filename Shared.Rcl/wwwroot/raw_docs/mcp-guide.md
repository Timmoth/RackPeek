# MCP Server Guide

RackPeek ships a built-in [Model Context Protocol](https://modelcontextprotocol.io) server, so
AI assistants (Claude Code, Claude Desktop, Cursor, VS Code, and anything else that speaks
MCP) can query, manage, maintain and build your inventory for you.

There is nothing extra to run: whenever the RackPeek web server is up, the MCP server is
listening at **`/mcp`** over streamable HTTP. It uses the same `X-Api-Key` gate as the
[Inventory API](/docs/inventory-api) — until you set `RPK_API_KEY` on the server the
endpoint answers `503` and stays shut.

---

## Quick start

```bash
# Run the server with an API key
docker run -d -p 8080:8080 \
  -v ./config:/config \
  -e RPK_API_KEY=your-shared-secret \
  aptacode/rackpeek:latest

# Connect Claude Code to it
claude mcp add --transport http rackpeek http://rack.lan:8080/mcp \
  --header "X-Api-Key: your-shared-secret"
```

Then just ask: *"what's running on my proxmox nodes?"*, *"add my new switch and cable it to
the rack server"*, *"generate an ssh config for everything tagged prod"*.

For clients that only speak stdio, put a stdio→HTTP proxy such as
[`mcp-remote`](https://www.npmjs.com/package/mcp-remote) in front of the same URL.

---

## The tools

### Query

| Tool | What it answers |
|---|---|
| `list_resources` | every resource, with optional `kind` / `tag` / `labelKey` filters |
| `get_resource` | one resource in full, as YAML that can be edited and upserted back |
| `search_resources` | free-text search over names, IPs, tags and labels |
| `get_summary` | counts of everything: hardware by kind, systems by type/OS, services, tags, labels |
| `get_tree` | the containment forest: hardware → systems → services |
| `list_connections` | physical port-to-port cabling |
| `get_subnets` | service IPs grouped into subnets, or filtered by a CIDR block |
| `get_schema` | the JSON schema and authoring rules for inventory YAML |

### Editing

| Tool | What it does |
|---|---|
| `upsert_resources` | bulk create/update from a YAML document — the main write path, with `dryRun` returning a per-resource diff before anything is written |
| `delete_resource` | removes a resource, detaches dependants, unplugs its connections |
| `rename_resource` | renames and rewrites every `runsOn` link and connection endpoint |
| `clone_resource` | copies a resource under a new name (never the discovery id) |
| `edit_tags` / `edit_labels` | add/remove tags and labels — merge mode can't remove, these can |
| `add_connection` / `remove_connection` | plug and unplug ports |

### Exporters

`export_ansible_inventory`, `export_ssh_config`, `export_hosts_file` and
`export_topology_mermaid` render the same outputs as the CLI exporters, straight into the
conversation.

### Git

`git_status` and `git_commit` version the config directory. They activate exactly like the
web UI's git integration — start the server with `GIT_TOKEN` (and optionally
`GIT_USERNAME`) — and explain that when they are off. See
[Git integration](/docs/git-integration).

### Discovery

| Tool | Reads |
|---|---|
| `discover_docker` | a Docker/Podman engine (`dockerHost`, e.g. `tcp://nas01:2375`) |
| `discover_proxmox` | a Proxmox VE cluster (`host`, e.g. `https://pve.lan:8006`) |

Both default to a **preview**: they return the discovered YAML for review and write
nothing. Pass `apply: true` to merge the result into the inventory — discovery merging
can add and update but never removes, and re-runs line resources up by
[discovery id](/docs/discovery-guide) so your renames stick.

Proxmox credentials come from the server's own `RPK_PVE_TOKEN_ID` /
`RPK_PVE_TOKEN_SECRET` configuration, never from the conversation, so tokens stay out of
AI context windows. There is no `discover system` tool on purpose: it probes the machine
it runs on, which for the server is its own container — run `rpk discover system` on the
machine being inventoried instead.

---

## The editing workflow an agent follows

1. `get_schema` — learn the document format once.
2. `get_resource` / `list_resources` — read the current state.
3. `upsert_resources` with `dryRun: true` — preview the exact per-resource diff.
4. `upsert_resources` — apply.
5. `git_commit` — snapshot the change (when git is configured).

Merge mode only adds and updates; anything destructive (deleting resources, removing
tags/labels/connections) goes through the dedicated tools, which are annotated as
destructive so well-behaved clients ask before calling them.

---

## Security notes

- The MCP endpoint is **off until `RPK_API_KEY` is set** — same behaviour as the
  inventory API.
- Anyone holding the key can read *and modify* the inventory through MCP. Treat the key
  accordingly, and put the server behind TLS (a reverse proxy) before exposing it beyond
  your LAN.
- The transport is stateless HTTP: no sessions, no sticky-session requirements behind a
  reverse proxy.
