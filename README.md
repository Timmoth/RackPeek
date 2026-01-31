# RackPeek

RackPeek is a lightweight, opinionated CLI tool for documenting and managing home lab and small-scale IT infrastructure.

It helps you track hardware, services, networks, and their relationships in a clear, scriptable, and reusable way without enterprise bloat or proprietary lock-in.

RackPeek is open source and community-driven.
Code, docs, ideas, bug reports, and real-world usage feedback are all massively appreciated.
If you run a home lab, you belong here.

[![Join our Discord](https://img.shields.io/badge/Discord-Join%20Us-7289DA?logo=discord&logoColor=white)](https://discord.gg/egXRPdesee)


## Philosophy
RackPeek treats infrastructure documentation as living reference data rather than static paperwork.

You should be able to document your environment as you build it, explore relationships between systems, and quickly understand how everything fits together, without drowning in unnecessary metadata or process.

RackPeek is not a CMDB replacement. It’s a clean framework for understanding and maintaining your lab.

[![RackPeek demo](./vhs/rpk-demo.gif)](./rpk-demo.gif)
[![RackPeek demo](./vhs/webui_screenshots/output.gif)](./rpk-webui-demo.gif)

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

## Release Status
```
[x] Ideation
[~] Development
[ ] Alpha Release
[ ] Beta Release
[ ] v1.0.0 Release
```

## Command Tree

- [rpk](Commands.md#rpk)
  - [summary](Commands.md#rpk-summary)
  - [servers](Commands.md#rpk-servers)
    - [summary](Commands.md#rpk-servers-summary)
    - [add](Commands.md#rpk-servers-add)
    - [get](Commands.md#rpk-servers-get)
    - [describe](Commands.md#rpk-servers-describe)
    - [set](Commands.md#rpk-servers-set)
    - [del](Commands.md#rpk-servers-del)
    - [tree](Commands.md#rpk-servers-tree)
    - [cpu](Commands.md#rpk-servers-cpu)
      - [add](Commands.md#rpk-servers-cpu-add)
      - [set](Commands.md#rpk-servers-cpu-set)
      - [del](Commands.md#rpk-servers-cpu-del)
    - [drive](Commands.md#rpk-servers-drive)
      - [add](Commands.md#rpk-servers-drive-add)
      - [set](Commands.md#rpk-servers-drive-set)
      - [del](Commands.md#rpk-servers-drive-del)
    - [gpu](Commands.md#rpk-servers-gpu)
      - [add](Commands.md#rpk-servers-gpu-add)
      - [set](Commands.md#rpk-servers-gpu-set)
      - [del](Commands.md#rpk-servers-gpu-del)
    - [nic](Commands.md#rpk-servers-nic)
      - [add](Commands.md#rpk-servers-nic-add)
      - [set](Commands.md#rpk-servers-nic-set)
      - [del](Commands.md#rpk-servers-nic-del)
  - [switches](Commands.md#rpk-switches)
    - [summary](Commands.md#rpk-switches-summary)
    - [add](Commands.md#rpk-switches-add)
    - [list](Commands.md#rpk-switches-list)
    - [get](Commands.md#rpk-switches-get)
    - [describe](Commands.md#rpk-switches-describe)
    - [set](Commands.md#rpk-switches-set)
    - [del](Commands.md#rpk-switches-del)
  - [systems](Commands.md#rpk-systems)
    - [summary](Commands.md#rpk-systems-summary)
    - [add](Commands.md#rpk-systems-add)
    - [list](Commands.md#rpk-systems-list)
    - [get](Commands.md#rpk-systems-get)
    - [describe](Commands.md#rpk-systems-describe)
    - [set](Commands.md#rpk-systems-set)
    - [del](Commands.md#rpk-systems-del)
    - [tree](Commands.md#rpk-systems-tree)
  - [accesspoints](Commands.md#rpk-accesspoints)
    - [summary](Commands.md#rpk-accesspoints-summary)
    - [add](Commands.md#rpk-accesspoints-add)
    - [list](Commands.md#rpk-accesspoints-list)
    - [get](Commands.md#rpk-accesspoints-get)
    - [describe](Commands.md#rpk-accesspoints-describe)
    - [set](Commands.md#rpk-accesspoints-set)
    - [del](Commands.md#rpk-accesspoints-del)
  - [ups](Commands.md#rpk-ups)
    - [summary](Commands.md#rpk-ups-summary)
    - [add](Commands.md#rpk-ups-add)
    - [list](Commands.md#rpk-ups-list)
    - [get](Commands.md#rpk-ups-get)
    - [describe](Commands.md#rpk-ups-describe)
    - [set](Commands.md#rpk-ups-set)
    - [del](Commands.md#rpk-ups-del)
  - [desktops](Commands.md#rpk-desktops)
    - [add](Commands.md#rpk-desktops-add)
    - [list](Commands.md#rpk-desktops-list)
    - [get](Commands.md#rpk-desktops-get)
    - [describe](Commands.md#rpk-desktops-describe)
    - [set](Commands.md#rpk-desktops-set)
    - [del](Commands.md#rpk-desktops-del)
    - [summary](Commands.md#rpk-desktops-summary)
    - [tree](Commands.md#rpk-desktops-tree)
    - [cpu](Commands.md#rpk-desktops-cpu)
      - [add](Commands.md#rpk-desktops-cpu-add)
      - [set](Commands.md#rpk-desktops-cpu-set)
      - [del](Commands.md#rpk-desktops-cpu-del)
    - [drive](Commands.md#rpk-desktops-drive)
      - [add](Commands.md#rpk-desktops-drive-add)
      - [set](Commands.md#rpk-desktops-drive-set)
      - [del](Commands.md#rpk-desktops-drive-del)
    - [gpu](Commands.md#rpk-desktops-gpu)
      - [add](Commands.md#rpk-desktops-gpu-add)
      - [set](Commands.md#rpk-desktops-gpu-set)
      - [del](Commands.md#rpk-desktops-gpu-del)
    - [nic](Commands.md#rpk-desktops-nic)
      - [add](Commands.md#rpk-desktops-nic-add)
      - [set](Commands.md#rpk-desktops-nic-set)
      - [del](Commands.md#rpk-desktops-nic-del)
  - [services](Commands.md#rpk-services)
    - [summary](Commands.md#rpk-services-summary)
    - [add](Commands.md#rpk-services-add)
    - [list](Commands.md#rpk-services-list)
    - [get](Commands.md#rpk-services-get)
    - [describe](Commands.md#rpk-services-describe)
    - [set](Commands.md#rpk-services-set)
    - [del](Commands.md#rpk-services-del)
    - [subnets](Commands.md#rpk-services-subnets)
