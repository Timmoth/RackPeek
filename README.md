# RackPeek

RackPeek is a lightweight, opinionated CLI tool for documenting and managing home lab and small-scale IT infrastructure.

It helps you track hardware, services, networks, and their relationships in a clear, scriptable, and reusable way without enterprise bloat or proprietary lock-in.

RackPeek is open source and community-driven.
Code, docs, ideas, bug reports, and real-world usage feedback are all massively appreciated.
If you run a home lab, you belong here.

[![Join our Discord](https://img.shields.io/badge/Discord-Join%20Us-7289DA?logo=discord&logoColor=white)](https://discord.gg/egXRPdesee)

[![Live Demo](https://img.shields.io/badge/Live%20Demo-Try%20RackPeek%20Online-2ea44f?logo=githubpages&logoColor=white)](https://timmoth.github.io/RackPeek/)


## Philosophy
RackPeek treats infrastructure documentation as living reference data rather than static paperwork.

You should be able to document your environment as you build it, explore relationships between systems, and quickly understand how everything fits together, without drowning in unnecessary metadata or process.

RackPeek is not a CMDB replacement. It’s a clean framework for understanding and maintaining your lab.

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
[x] Development
[~] Alpha Release
[ ] Beta Release
[ ] v1.0.0 Release
```

## Command Tree

- [rpk](Commands.md#rpk)
  - [summary](Commands.md#rpk-summary) - Show a summarized report of all resources in the system
  - [servers](Commands.md#rpk-servers) - Manage servers and their components
    - [summary](Commands.md#rpk-servers-summary) - Show a summarized hardware report for all servers
    - [add](Commands.md#rpk-servers-add) - Add a new server to the inventory
    - [get](Commands.md#rpk-servers-get) - List all servers or retrieve a specific server by name
    - [describe](Commands.md#rpk-servers-describe) - Display detailed information about a specific server
    - [set](Commands.md#rpk-servers-set) - Update properties of an existing server
    - [del](Commands.md#rpk-servers-del) - Delete a server from the inventory
    - [tree](Commands.md#rpk-servers-tree) - Display the dependency tree of a server
    - [cpu](Commands.md#rpk-servers-cpu) - Manage CPUs attached to a server
      - [add](Commands.md#rpk-servers-cpu-add) - Add a CPU to a specific server
      - [set](Commands.md#rpk-servers-cpu-set) - Update configuration of a server CPU
      - [del](Commands.md#rpk-servers-cpu-del) - Remove a CPU from a server
    - [drive](Commands.md#rpk-servers-drive) - Manage drives attached to a server
      - [add](Commands.md#rpk-servers-drive-add) - Add a storage drive to a server
      - [set](Commands.md#rpk-servers-drive-set) - Update properties of a server drive
      - [del](Commands.md#rpk-servers-drive-del) - Remove a drive from a server
    - [gpu](Commands.md#rpk-servers-gpu) - Manage GPUs attached to a server
      - [add](Commands.md#rpk-servers-gpu-add) - Add a GPU to a server
      - [set](Commands.md#rpk-servers-gpu-set) - Update properties of a server GPU
      - [del](Commands.md#rpk-servers-gpu-del) - Remove a GPU from a server
    - [nic](Commands.md#rpk-servers-nic) - Manage network interface cards (NICs) for a server
      - [add](Commands.md#rpk-servers-nic-add) - Add a NIC to a server
      - [set](Commands.md#rpk-servers-nic-set) - Update properties of a server NIC
      - [del](Commands.md#rpk-servers-nic-del) - Remove a NIC from a server
  - [switches](Commands.md#rpk-switches) - Manage network switches
    - [summary](Commands.md#rpk-switches-summary) - Show a hardware report for all switches
    - [add](Commands.md#rpk-switches-add) - Add a new network switch to the inventory
    - [list](Commands.md#rpk-switches-list) - List all switches in the system
    - [get](Commands.md#rpk-switches-get) - Retrieve details of a specific switch by name
    - [describe](Commands.md#rpk-switches-describe) - Show detailed information about a switch
    - [set](Commands.md#rpk-switches-set) - Update properties of a switch
    - [del](Commands.md#rpk-switches-del) - Delete a switch from the inventory
    - [port](Commands.md#rpk-switches-port) - Manage ports on a network switch
      - [add](Commands.md#rpk-switches-port-add) - Add a port to a switch
      - [set](Commands.md#rpk-switches-port-set) - Update a switch port
      - [del](Commands.md#rpk-switches-port-del) - Remove a port from a switch
  - [routers](Commands.md#rpk-routers) - Manage network routers
    - [summary](Commands.md#rpk-routers-summary) - Show a hardware report for all routers
    - [add](Commands.md#rpk-routers-add) - Add a new network router to the inventory
    - [list](Commands.md#rpk-routers-list) - List all routers in the system
    - [get](Commands.md#rpk-routers-get) - Retrieve details of a specific router by name
    - [describe](Commands.md#rpk-routers-describe) - Show detailed information about a router
    - [set](Commands.md#rpk-routers-set) - Update properties of a router
    - [del](Commands.md#rpk-routers-del) - Delete a router from the inventory
    - [port](Commands.md#rpk-routers-port) - Manage ports on a router
      - [add](Commands.md#rpk-routers-port-add) - Add a port to a router
      - [set](Commands.md#rpk-routers-port-set) - Update a router port
      - [del](Commands.md#rpk-routers-port-del) - Remove a port from a router
  - [firewalls](Commands.md#rpk-firewalls) - Manage firewalls
    - [summary](Commands.md#rpk-firewalls-summary) - Show a hardware report for all firewalls
    - [add](Commands.md#rpk-firewalls-add) - Add a new firewall to the inventory
    - [list](Commands.md#rpk-firewalls-list) - List all firewalls in the system
    - [get](Commands.md#rpk-firewalls-get) - Retrieve details of a specific firewall by name
    - [describe](Commands.md#rpk-firewalls-describe) - Show detailed information about a firewall
    - [set](Commands.md#rpk-firewalls-set) - Update properties of a firewall
    - [del](Commands.md#rpk-firewalls-del) - Delete a firewall from the inventory
    - [port](Commands.md#rpk-firewalls-port) - Manage ports on a firewall
      - [add](Commands.md#rpk-firewalls-port-add) - Add a port to a firewall
      - [set](Commands.md#rpk-firewalls-port-set) - Update a firewall port
      - [del](Commands.md#rpk-firewalls-port-del) - Remove a port from a firewall
  - [systems](Commands.md#rpk-systems) - Manage systems and their dependencies
    - [summary](Commands.md#rpk-systems-summary) - Show a summary report for all systems
    - [add](Commands.md#rpk-systems-add) - Add a new system to the inventory
    - [list](Commands.md#rpk-systems-list) - List all systems
    - [get](Commands.md#rpk-systems-get) - Retrieve a system by name
    - [describe](Commands.md#rpk-systems-describe) - Display detailed information about a system
    - [set](Commands.md#rpk-systems-set) - Update properties of a system
    - [del](Commands.md#rpk-systems-del) - Delete a system from the inventory
    - [tree](Commands.md#rpk-systems-tree) - Display the dependency tree for a system
  - [accesspoints](Commands.md#rpk-accesspoints) - Manage access points
    - [summary](Commands.md#rpk-accesspoints-summary) - Show a hardware report for all access points
    - [add](Commands.md#rpk-accesspoints-add) - Add a new access point
    - [list](Commands.md#rpk-accesspoints-list) - List all access points
    - [get](Commands.md#rpk-accesspoints-get) - Retrieve an access point by name
    - [describe](Commands.md#rpk-accesspoints-describe) - Show detailed information about an access point
    - [set](Commands.md#rpk-accesspoints-set) - Update properties of an access point
    - [del](Commands.md#rpk-accesspoints-del) - Delete an access point
  - [ups](Commands.md#rpk-ups) - Manage UPS units
    - [summary](Commands.md#rpk-ups-summary) - Show a hardware report for all UPS units
    - [add](Commands.md#rpk-ups-add) - Add a new UPS unit
    - [list](Commands.md#rpk-ups-list) - List all UPS units
    - [get](Commands.md#rpk-ups-get) - Retrieve a UPS unit by name
    - [describe](Commands.md#rpk-ups-describe) - Show detailed information about a UPS unit
    - [set](Commands.md#rpk-ups-set) - Update properties of a UPS unit
    - [del](Commands.md#rpk-ups-del) - Delete a UPS unit
  - [desktops](Commands.md#rpk-desktops) - Manage desktop computers and their components
    - [add](Commands.md#rpk-desktops-add) - Add a new desktop
    - [list](Commands.md#rpk-desktops-list) - List all desktops
    - [get](Commands.md#rpk-desktops-get) - Retrieve a desktop by name
    - [describe](Commands.md#rpk-desktops-describe) - Show detailed information about a desktop
    - [set](Commands.md#rpk-desktops-set) - Update properties of a desktop
    - [del](Commands.md#rpk-desktops-del) - Delete a desktop from the inventory
    - [summary](Commands.md#rpk-desktops-summary) - Show a summarized hardware report for all desktops
    - [tree](Commands.md#rpk-desktops-tree) - Display the dependency tree for a desktop
    - [cpu](Commands.md#rpk-desktops-cpu) - Manage CPUs attached to desktops
      - [add](Commands.md#rpk-desktops-cpu-add) - Add a CPU to a desktop
      - [set](Commands.md#rpk-desktops-cpu-set) - Update a desktop CPU
      - [del](Commands.md#rpk-desktops-cpu-del) - Remove a CPU from a desktop
    - [drive](Commands.md#rpk-desktops-drive) - Manage storage drives attached to desktops
      - [add](Commands.md#rpk-desktops-drive-add) - Add a drive to a desktop
      - [set](Commands.md#rpk-desktops-drive-set) - Update a desktop drive
      - [del](Commands.md#rpk-desktops-drive-del) - Remove a drive from a desktop
    - [gpu](Commands.md#rpk-desktops-gpu) - Manage GPUs attached to desktops
      - [add](Commands.md#rpk-desktops-gpu-add) - Add a GPU to a desktop
      - [set](Commands.md#rpk-desktops-gpu-set) - Update a desktop GPU
      - [del](Commands.md#rpk-desktops-gpu-del) - Remove a GPU from a desktop
    - [nic](Commands.md#rpk-desktops-nic) - Manage network interface cards (NICs) for desktops
      - [add](Commands.md#rpk-desktops-nic-add) - Add a NIC to a desktop
      - [set](Commands.md#rpk-desktops-nic-set) - Update a desktop NIC
      - [del](Commands.md#rpk-desktops-nic-del) - Remove a NIC from a desktop
  - [Laptops](Commands.md#rpk-laptops) - Manage Laptop computers and their components
    - [add](Commands.md#rpk-laptops-add) - Add a new Laptop
    - [list](Commands.md#rpk-laptops-list) - List all Laptops
    - [get](Commands.md#rpk-laptops-get) - Retrieve a Laptop by name
    - [describe](Commands.md#rpk-laptops-describe) - Show detailed information about a Laptop
    - [del](Commands.md#rpk-laptops-del) - Delete a Laptop from the inventory
    - [summary](Commands.md#rpk-laptops-summary) - Show a summarized hardware report for all Laptops
    - [tree](Commands.md#rpk-laptops-tree) - Display the dependency tree for a Laptop
    - [cpu](Commands.md#rpk-laptops-cpu) - Manage CPUs attached to Laptops
      - [add](Commands.md#rpk-laptops-cpu-add) - Add a CPU to a Laptop
      - [set](Commands.md#rpk-laptops-cpu-set) - Update a Laptop CPU
      - [del](Commands.md#rpk-laptops-cpu-del) - Remove a CPU from a Laptop
    - [drive](Commands.md#rpk-laptops-drive) - Manage storage drives attached to Laptops
      - [add](Commands.md#rpk-laptops-drive-add) - Add a drive to a Laptop
      - [set](Commands.md#rpk-laptops-drive-set) - Update a Laptop drive
      - [del](Commands.md#rpk-laptops-drive-del) - Remove a drive from a Laptop
    - [gpu](Commands.md#rpk-laptops-gpu) - Manage GPUs attached to Laptops
      - [add](Commands.md#rpk-laptops-gpu-add) - Add a GPU to a Laptop
      - [set](Commands.md#rpk-laptops-gpu-set) - Update a Laptop GPU
      - [del](Commands.md#rpk-laptops-gpu-del) - Remove a GPU from a Laptop
  - [services](Commands.md#rpk-services) - Manage services and their configurations
    - [summary](Commands.md#rpk-services-summary) - Show a summary report for all services
    - [add](Commands.md#rpk-services-add) - Add a new service
    - [list](Commands.md#rpk-services-list) - List all services
    - [get](Commands.md#rpk-services-get) - Retrieve a service by name
    - [describe](Commands.md#rpk-services-describe) - Show detailed information about a service
    - [set](Commands.md#rpk-services-set) - Update properties of a service
    - [del](Commands.md#rpk-services-del) - Delete a service
    - [subnets](Commands.md#rpk-services-subnets) - List subnets associated with a service, optionally filtered by CIDR
