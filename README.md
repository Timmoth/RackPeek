[![RackPeek](./assets/rackpeek_banner_thin.png)](https://timmoth.github.io/RackPeek/)

![Version](https://img.shields.io/badge/Version-2.3.0-2ea44f) ![Status](https://img.shields.io/badge/Status-Stable-success)
[![Discord](https://img.shields.io/badge/Discord-Join%20Us-7289DA?logo=discord&logoColor=white)](https://discord.gg/egXRPdesee)
[![Live Demo](https://img.shields.io/badge/Live%20Demo-Try%20RackPeek%20Online-2ea44f?logo=githubpages&logoColor=white)](https://timmoth.github.io/RackPeek/)
[![Docker Hub](https://img.shields.io/badge/Docker%20Hub-rackpeek-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/aptacode/rackpeek/)

RackPeek is a self-hosted CLI and web app for documenting home labs and small IT environments. Track hardware, systems, services, and connections in a portable YAML inventory. Use the CLI or web app to manage it, discover existing infrastructure, and export data for tools such as Ansible.

## Quick start

Run RackPeek with a persistent Docker volume:

```sh
docker volume create rackpeek-config
docker run -d \
  --name rackpeek \
  -p 8080:8080 \
  -v rackpeek-config:/app/config \
  aptacode/rackpeek:latest
```

Open [http://localhost:8080](http://localhost:8080). RackPeek stores its inventory in `config.yaml` inside the volume. For bind mounts, the host directory must be writable by container user `1654`; add `:Z` on SELinux systems.

## Mobile app

The iOS and Android app supports local inventory editing and syncs with a RackPeek server when connected. The app is in early access; [join the Discord](https://discord.gg/egXRPdesee) for updates and testing information.

## Documentation

- [Overview](https://timmoth.github.io/RackPeek/docs/overview)
- [Installation](https://timmoth.github.io/RackPeek/docs/install-guide)
- [CLI reference](https://timmoth.github.io/RackPeek/docs/cli-commands)
- [Discovery](https://timmoth.github.io/RackPeek/docs/discovery-guide)
- [Inventory API](https://timmoth.github.io/RackPeek/docs/inventory-api)
- [MCP server](https://timmoth.github.io/RackPeek/docs/mcp-guide)
- [Ansible inventory](https://timmoth.github.io/RackPeek/docs/ansible-generator-guide)

## Development

See the [developer setup](docs/development/dev-setup.md), [testing guide](docs/development/testing-guidelines.md), and [contribution guide](docs/development/contribution-guidelines.md). The release process is in [release-guide.md](docs/development/release-guide.md).
