# Resource Levels

RackPeek organizes your infrastructure into three levels: **Hardware**, **Systems**, and **Services**. Understanding how these levels relate to each other is the key to modeling your homelab accurately.

:::abstract
Hardware is the physical layer (servers, switches, UPS units). Systems run on hardware or on other systems (hypervisors, VMs, containers). Services run on systems (apps, databases, network endpoints). The `runs-on` property connects them. Systems can nest to any depth, so you can model Proxmox hosting VMs hosting Docker containers.
:::

## The Three Levels

Every resource in RackPeek belongs to one of three levels. Each level builds on the one below it, connected by the `runs-on` relationship.

```
Hardware          The physical machines in your rack or on your desk.
  └── Systems     The operating systems, VMs, and containers running on that hardware.
       └── Services   The applications and network services running on those systems.
```

Think of it as: hardware is the metal, systems are what boot on it, and services are what your users actually connect to.

---

## Hardware

Hardware represents the physical devices in your infrastructure. These are things you can touch — rack-mounted servers, network switches, Wi-Fi access points, UPS units, and workstations.

### Hardware Types

| Type | What it represents | Example |
|------|--------------------|---------|
| **Server** | Rack or tower server | Dell PowerEdge R730, custom Proxmox node |
| **Switch** | Network switch | UniFi USW-Pro-48-PoE |
| **Router** | Network router | MikroTik RB5009 |
| **Firewall** | Dedicated firewall appliance | Netgate SG-3100, pfSense box |
| **Access Point** | Wireless access point | UniFi U6-Pro |
| **Desktop** | Desktop workstation | Custom build, Mac Mini |
| **Laptop** | Laptop | ThinkPad T14s |
| **UPS** | Uninterruptible power supply | APC SMT1500 |

### Sub-Resources

Some hardware types support sub-resources that describe their internal components.

| Sub-Resource | Server | Desktop | Laptop | Switch | Router | Firewall |
|-------------|:------:|:-------:|:------:|:------:|:------:|:--------:|
| CPU         | Yes    | Yes     | Yes    |        |        |          |
| Drive       | Yes    | Yes     | Yes    |        |        |          |
| GPU         | Yes    | Yes     | Yes    |        |        |          |
| NIC         | Yes    | Yes     |        |        |        |          |
| Port        |        |         |        | Yes    | Yes    | Yes      |
| RAM         | Yes    | Yes     | Yes    |        |        |          |

Hardware is the foundation. Nothing runs "on" hardware in the RackPeek sense — hardware just exists. Systems and services cannot be hardware; they live on top of it.

### Adding Hardware

#### CLI

```bash
rpk servers add pve-01
rpk switches add usw-pro-48
rpk ups add apc-1500
```

#### Web UI

Navigate to **Hardware** and select the hardware type (e.g., **Servers**). Click **Add Server** and enter a name.

---

## Systems

Systems represent the software environments that run on your hardware — or on other systems. A system is anything that provides an execution context: a hypervisor, a virtual machine, a container, a bare-metal OS installation, or a cloud instance.

### System Types

| Type | What it represents | Example |
|------|--------------------|---------|
| `baremetal` | OS installed directly on hardware | Debian on a dedicated server |
| `hypervisor` | Virtualization platform | Proxmox VE, ESXi, Hyper-V |
| `vm` | Virtual machine | Ubuntu VM on Proxmox |
| `container` | Container or container runtime | Docker container, LXC |
| `embedded` | Firmware or embedded OS | OPNsense on a firewall appliance |
| `cloud` | Cloud-hosted instance | AWS EC2, Hetzner VPS |
| `other` | Anything that doesn't fit above | Custom runtime |

### How Systems Connect

Every system has a `runs-on` property that defines where it lives. A system can run on:

- **Hardware** — a hypervisor installed on a physical server, or a bare-metal OS on a desktop.
- **Another system** — a VM running on a hypervisor, or a container running inside a VM.

> [!TIP]
> You can nest systems. A hypervisor is a system that runs on hardware, and a VM is a system that runs on that hypervisor. There is no artificial limit to how deep you can nest. See [Systems Running on Systems](docs/resource-levels#systems-running-on-systems) for a full worked example.

### Adding Systems

#### CLI

```bash
rpk systems add proxmox-ve
rpk systems set proxmox-ve --type hypervisor --os "Proxmox VE 8.3" --runs-on pve-01
```

#### Web UI

Navigate to **Systems**, click **Add System**, and enter a name. Then open the system's detail page to set its type, OS, and `runs-on` target.

---

## Services

Services represent the applications and network endpoints your systems provide. If a system is the platform, a service is what actually does the work — a web app, a database, a DNS server, a media streaming service.

> [!IMPORTANT]
> Services always run on systems. You cannot attach a service directly to hardware.

### Service Properties

| Property | Description | Example |
|----------|-------------|---------|
| IP | Network address | `10.0.10.50` |
| Port | Listening port | `8096` |
| Protocol | Network protocol | `tcp`, `http` |
| URL | Access URL | `https://jellyfin.home.lab` |

### Adding Services

#### CLI

```bash
rpk services add jellyfin
rpk services set jellyfin --runs-on media-vm --ip 10.0.10.50 --port 8096 --url https://jellyfin.home.lab
```

#### Web UI

Navigate to **Services**, click **Add Service**, and enter a name. Open the service's detail page to set its `runs-on` target and network details.

---

## The Runs-On Relationship

`runs-on` is the glue that connects everything in RackPeek. It tells you what a resource depends on.

### Rules

| Resource | Can run on |
|----------|------------|
| Hardware | Nothing — hardware is the base layer |
| System   | Hardware **or** another System |
| Service  | System only |

> [!NOTE]
> A resource can have multiple `runs-on` targets. This handles cases like a clustered service that runs across several systems, or a VM that has been migrated between hosts.

### Setting Runs-On

#### CLI

Use the `--runs-on` flag on the `set` command:

```bash
rpk systems set my-vm --runs-on proxmox-ve
rpk services set gitea --runs-on docker-host
```

To set multiple parents:

```bash
rpk systems set my-vm --runs-on proxmox-ve --runs-on proxmox-ve-02
```

#### Web UI

Open any system or service detail page. The `runs-on` field lets you pick from existing hardware or systems.

---

## Systems Running on Systems

This is the most powerful part of the resource model and the one most often asked about. RackPeek fully supports nesting systems inside other systems, which is how you model real-world virtualization stacks.

### The Problem

In a typical Proxmox homelab, your physical server runs a hypervisor, and that hypervisor runs VMs. Without nesting, you'd lose the hypervisor layer — VMs would appear to run directly on the bare metal, which doesn't reflect reality and makes it impossible to document important details about the hypervisor itself.

### The Solution

Model every layer as its own system, each pointing `runs-on` to the layer below it:

```
pve-01 (Server)                      ← Hardware
  └── proxmox-ve (System: hypervisor) ← runs-on pve-01
       ├── media-vm (System: vm)      ← runs-on proxmox-ve
       ├── docker-vm (System: vm)     ← runs-on proxmox-ve
       └── pihole-lxc (System: container) ← runs-on proxmox-ve
```

Each system in this chain is a first-class resource. You can document its OS version, allocated RAM, CPU cores, drives, IP address, tags, labels, and notes — just like any other resource.

### Worked Example: Proxmox Server with VMs and Services

This walks through modeling a common homelab setup from the ground up.

**Step 1: Add the physical server.**

```bash
rpk servers add pve-01
```

**Step 2: Add the hypervisor and link it to the server.**

```bash
rpk systems add proxmox-ve
rpk systems set proxmox-ve --type hypervisor --os "Proxmox VE 8.3" --runs-on pve-01
```

**Step 3: Add VMs and containers that run on the hypervisor.**

```bash
rpk systems add media-vm
rpk systems set media-vm --type vm --os "Ubuntu 24.04" --cores 4 --ram 8 --runs-on proxmox-ve

rpk systems add docker-vm
rpk systems set docker-vm --type vm --os "Debian 12" --cores 8 --ram 16 --runs-on proxmox-ve

rpk systems add pihole-lxc
rpk systems set pihole-lxc --type container --os "Alpine 3.21" --cores 1 --ram 512 --runs-on proxmox-ve
```

**Step 4: Add services that run on those systems.**

```bash
rpk services add jellyfin
rpk services set jellyfin --runs-on media-vm --port 8096 --url https://jellyfin.home.lab

rpk services add gitea
rpk services set gitea --runs-on docker-vm --port 3000 --url https://gitea.home.lab

rpk services add pihole
rpk services set pihole --runs-on pihole-lxc --port 80 --ip 10.0.10.3
```

The result is a complete picture:

```
pve-01 (Server)
  └── proxmox-ve (Hypervisor, Proxmox VE 8.3)
       ├── media-vm (VM, Ubuntu 24.04)
       │    └── jellyfin (:8096)
       ├── docker-vm (VM, Debian 12)
       │    └── gitea (:3000)
       └── pihole-lxc (Container, Alpine 3.21)
            └── pihole (:80)
```

### Deeper Nesting

You can go further. If `docker-vm` runs Docker, and you want to model individual containers:

```bash
rpk systems add gitea-container
rpk systems set gitea-container --type container --runs-on docker-vm

rpk services add gitea
rpk services set gitea --runs-on gitea-container --port 3000
```

This gives you:

```
docker-vm (VM)
  └── gitea-container (Container)
       └── gitea (:3000)
```

There is no hard limit on depth. Model as many layers as you need to accurately represent your setup.

---

## Choosing the Right Level

If you are unsure whether something should be hardware, a system, or a service, use this decision guide:

| Question | Answer | Level |
|----------|--------|-------|
| Can you physically touch it? | Yes | **Hardware** |
| Does it provide an execution environment (boots, runs processes)? | Yes | **System** |
| Does it listen on a port or provide a network endpoint? | Yes | **Service** |
| Is it virtualization software that hosts other systems? | Yes | **System** (type: `hypervisor`) |
| Is it a Docker container? | Yes | **System** (type: `container`) |
| Is it an app running inside a container or VM? | Yes | **Service** |

### Common Modeling Decisions

**Proxmox / ESXi / Hyper-V** — Model as a system with type `hypervisor`, running on the physical server.

**Docker host** — Model the host VM or bare-metal install as a system. Optionally model individual containers as nested systems.

**Pi-hole / AdGuard Home** — If it runs in an LXC container, model the container as a system and Pi-hole as a service on that system.

**TrueNAS** — Model as a system with type `baremetal` (or `vm` if virtualized), running on the NAS hardware. SMB, NFS, and iSCSI shares are services on that system.

**OPNsense on a Netgate appliance** — The Netgate box is hardware (firewall). OPNsense is a system with type `embedded` running on it.

---

## Related Pages

- [CLI Commands](docs/cli-commands) for the full command reference
- [Installation Guide](docs/install-guide) to get started with RackPeek
