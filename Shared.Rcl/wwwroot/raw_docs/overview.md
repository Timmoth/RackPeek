# RackPeek Overview

RackPeek is a self-hosted CLI and web app for documenting home labs and small IT environments. It keeps inventory in a portable YAML file, with no database or telemetry.

## What you can do

- Record hardware, systems, services, and physical connections.
- Edit the same inventory from the CLI or web app.
- Discover systems, Docker, Proxmox, OPNsense, and network services with the CLI, then review and import the results.
- Export inventory for Ansible, SSH, hosts files, and topology diagrams.
- Connect an MCP client to query and update inventory through the authenticated `/mcp` endpoint.
- Use the mobile app to edit a local inventory offline and sync it to a server later.

## How inventory works

RackPeek stores the inventory in `config.yaml`. Each resource has a kind and name; systems and services can be connected with `runsOn`, and hardware ports can be linked with physical connections. The YAML format is human-readable and remains yours to inspect, version, or reuse.

Start with the [installation guide](/docs/install-guide), then see [resource levels](/docs/resource-levels) for modeling guidance. The [CLI reference](/docs/cli-commands) and guides for [discovery](/docs/discovery-guide), the [Inventory API](/docs/inventory-api), [MCP](/docs/mcp-guide), and [exports](/docs/ansible-generator-guide) cover specific workflows.

The mobile app is in early access. Join the [RackPeek Discord](https://discord.gg/egXRPdesee) for updates and testing information.
