[![RackPeek demo](./assets/rackpeek_banner_thin.png)](./assets/rackpeek_banner_thin.png)

![Version](https://img.shields.io/badge/Version-1.0.0-2ea44f) ![Status](https://img.shields.io/badge/Status-Stable-success)
[![Join our Discord](https://img.shields.io/badge/Discord-Join%20Us-7289DA?logo=discord&logoColor=white)](https://discord.gg/egXRPdesee) [![Live Demo](https://img.shields.io/badge/Live%20Demo-Try%20RackPeek%20Online-2ea44f?logo=githubpages&logoColor=white)](https://timmoth.github.io/RackPeek/) [![Docker Hub](https://img.shields.io/badge/Docker%20Hub-rackpeek-2496ED?logo=docker&logoColor=white)](https://hub.docker.com/r/aptacode/rackpeek/)

```
Announcing v1.0.0, officially out of beta.  

Thanks to everyone who tried early versions, opened issues, suggested changes, or used it in their lab and shared feedback.  
   
Appreciate all the support.
```

RackPeek is a lightweight, opinionated CLI tool / webui for documenting and managing home lab and small-scale IT infrastructure.

It helps you track hardware, services, networks, and their relationships in a clear, scriptable, and reusable way without enterprise bloat or proprietary lock-in or drowning in unnecessary metadata or process.


[![DB Tech — Finally Document Your Home Lab the Easy Way (Docker Install)](https://img.shields.io/badge/DB%20Tech%20[video]-Finally%20Document%20Your%20Home%20Lab%20the%20Easy%20Way%20(Docker%20Install)-blue?style=for-the-badge)](https://www.youtube.com/watch?v=RJtMO8kIsqU)
[![Brandon Lee — I’m Documenting My Entire Home Lab as Code with RackPeek](https://img.shields.io/badge/Brandon%20Lee%20[article]-I%E2%80%99m%20Documenting%20My%20Entire%20Home%20Lab%20as%20Code%20with%20RackPeek-blue?style=for-the-badge)](https://www.virtualizationhowto.com/2026/02/im-documenting-my-entire-home-lab-as-code-with-rackpeek/)
[![Jared Heinrichs — How to Document Your Entire Homelab](https://img.shields.io/badge/Jared%20Heinrichs%20[article]-How%20to%20Document%20Your%20Entire%20Homelab-blue?style=for-the-badge)](https://jaredheinrichs.substack.com/p/how-to-document-your-entire-homelab)


[![RackPeek demo](./vhs/rpk-demo.gif)](./rpk-demo.gif)
[![RackPeek demo](./vhs/webui_screenshots/output.gif)](./rpk-webui-demo.gif)

## Running RackPeek with Docker
```text
# Named volume
docker volume create rackpeek-config
docker run -d \
  --name rackpeek \
  -p 8080:8080 \
  -v rackpeek-config:/app/config \
  aptacode/rackpeek:latest

# Bind mount
docker run -d \
  --name rackpeek \
  -p 8080:8080 \
  -v $(pwd)/config:/app/config \
  aptacode/rackpeek:latest

# Note - RackPeek stores its state in YAML
config/
└── config.yaml
```
Or Docker compose
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
    restart: unless-stopped

volumes:
  rackpeek-config:

```

## Docs

* 
  [**Overview**](https://timmoth.github.io/RackPeek/docs/overview)

* 
  [**Installation Guide**](https://timmoth.github.io/RackPeek/docs/install-guide)

* 
  [**Ansible Inventory Generator Guide**](https://timmoth.github.io/RackPeek/docs/ansible-generator-guide)

* 
  [**CLI Commands Reference**](https://timmoth.github.io/RackPeek/docs/cli-commands)

* 
  [**Versioning**](https://timmoth.github.io/RackPeek/docs/versioning)


## POST V1.0.0 Roadmap
- Support for IoT and networked devices
- Enhanced networking and port mapping
- Git integration (version-controlled, shared configuration)
- Diagramming tools
- Ansible inventory generator
### The roadmap for the next wave of features is actively being discussed, please make your voice heard! 

## Contribution Guide

We are now gearing up for the full v1.0.0 release, so development focus is on stability / bug fixes / essential core missing features. Please raise any suggestions / bugs / feedback in the Github issues.

## Questionnaire

We’re gathering feedback from homelabbers to validate direction and prioritize features.  
Answer whichever questions stand out to you, your input directly shapes the project.

[![User Questionnaire](https://img.shields.io/badge/Questionnaire-Share%20Feedback-orange?logo=googleforms&logoColor=white)](https://forms.gle/KKA4bqfGAeRYvGxT6)

## Core Values

**Simplicity**  
RackPeek focuses on clarity and usefulness. Its scope is intentionally kept narrow to avoid unnecessary abstraction and feature creep.

**Ease of Deployment**  
The tool exists to reduce operational complexity. Installation, upgrades, and day-to-day usage should be straightforward and low-friction.

**Openness**  
RackPeek uses open, non-proprietary data formats. You fully own your data and should be free to easily inspect, migrate, or reuse it however you choose.

**Community**  
Contributors of all experience levels are welcome. Knowledge sharing, mentorship, and collaboration are core to the project’s culture.

**Privacy & Security**  
No telemetry, no ads, no tracking, and no artificial restrictions. What runs on your infrastructure stays on your infrastructure.

**Dogfooding**  
RackPeek is built to solve real problems we actively have. If a feature isn’t useful in practice, it doesn’t belong.

**Opinionated**  
The project is optimized for home labs and self-hosted environments, not enterprise CMDBs or corporate documentation workflows.


## Development Docs

* [`contribution-guidelines.md`](docs/development/contribution-guidelines.md) – How to propose changes and submit PRs
* [`dev-cheat-sheet.md`](docs/development/dev-cheat-sheet.md) – Build, release, Docker, and testing commands
* [`testing-guidelines.md`](docs/development/testing-guidelines.md) – Testing principles and standards