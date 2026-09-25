# Auto Discovery Guide

`rpk discover` reads your infrastructure and writes it out as RackPeek YAML, so you
don't have to type in what the machine already knows about itself.

| Command | Reads | Produces |
|---|---|---|
| `rpk discover system` | the machine it runs on | one **System** resource |
| `rpk discover docker` | the Docker Engine API | one **Service** per published container, plus the **System** they run on |
| `rpk discover proxmox` | a Proxmox VE cluster | a **Server** and **System** per node, a **System** per guest, already wired together |

Both print YAML to standard output by default and change nothing, so it is always safe
to run one and look at the result first.

---

## Quick start

```bash
# Look at what this machine reports
rpk discover system

# Save it
rpk discover system > nas01.yaml

# Send it straight to your RackPeek server
export RPK_SERVER=http://rack.lan:8080
export RPK_API_KEY=your-shared-secret
rpk discover system --push
```

`--push` uses the same [Inventory API](/docs/inventory-api) as any other import, so the
server needs `RPK_API_KEY` set. Discovery always **merges** — it can add and update, and
never removes anything it did not find.

---

## Keeping it honest: names and identity

The problem with re-running discovery is names. A RackPeek resource is identified by its
name, and names are yours to choose — but a machine only knows its hostname. Run
discovery twice, rename something in between, and a naive tool gives you two resources.

Each discovered resource therefore carries a `discoveryId`:

```yaml
- kind: System
  name: nas01
  discoveryId: rpk1:sys:a3f9c2e1b8d47e60
  type: baremetal
  os: Debian GNU/Linux 12 (bookworm)
```

The id is derived from something stable about the machine — `/etc/machine-id` on Linux,
the platform UUID on macOS — hashed, so no raw machine identifier ends up in a config
file you might commit. The same machine produces the same id every time, with nothing
stored locally, from any machine you run the command on.

What that buys you:

* **Renaming is safe.** Call it `storage-01` in the web UI and the next discovery run
  updates `storage-01`. It will never rename a resource you named.
* **Existing resources are adopted.** If you already documented `nas01` by hand, the
  first discovery run attaches to it — keeping your notes and gaining an id — rather
  than creating a duplicate.
* **Two machines cannot collide.** A second machine that happens to share a hostname is
  given a suffixed name instead of overwriting the first.

> **Cloned VM templates share `/etc/machine-id`.** If you clone a Proxmox or VMware
> template without resetting it, every clone reports the same identity. RackPeek rejects
> a payload containing duplicate ids rather than silently merging the machines. Run
> `systemd-machine-id-setup` on the clones, or reset it in the template before cloning.

---

## `rpk discover system`

Supported on **Linux and macOS**. Reports hostname, OS, cores, RAM, primary address, and
whether the machine is bare metal, a VM or a container. Disks are included on Linux.

```bash
rpk discover system --name nas01
```

| Option | Meaning |
|---|---|
| `-n`, `--name <NAME>` | Name for this machine. Defaults to its hostname. |
| `--push` | Upload instead of printing. |
| `--server <URL>` | Server to upload to. Defaults to `RPK_SERVER`. |
| `--api-key <KEY>` | API key. Defaults to `RPK_API_KEY`. |
| `--dry-run` | Ask the server what would change, without changing it. |

Anything the host cannot answer is left out rather than guessed at, and the merge treats
a missing field as "leave whatever is already there alone".

### Keeping it up to date

Because re-runs update rather than duplicate, this is safe to put on a timer. On a
systemd host:

```ini
# /etc/systemd/system/rackpeek-discover.service
[Service]
Type=oneshot
Environment=RPK_SERVER=http://rack.lan:8080
Environment=RPK_API_KEY=your-shared-secret
ExecStart=/usr/local/bin/rpk discover system --name nas01 --push
```

```ini
# /etc/systemd/system/rackpeek-discover.timer
[Timer]
OnCalendar=daily
Persistent=true

[Install]
WantedBy=timers.target
```

Passing `--name` is worth it here: it pins the resource name so a hostname change does
not look like a new machine.

---

## `rpk discover docker`

Reads the Docker Engine API and emits every container with a **published port** as a
Service, pointed at the host it runs on. For a local engine the host's own **System**
resource rides along in front of the services — that is what lets the server keep
`runsOn` pointing at the right resource even after you rename the host (the id travels
with the System; the services only know a name).

```bash
rpk discover docker
```

| Option | Meaning |
|---|---|
| `--docker-host <URI>` | Docker endpoint. Defaults to `DOCKER_HOST`, then `/var/run/docker.sock`. |
| `--host <NAME>` | Name of the machine the containers run on. Defaults to its hostname. |

Plus the same `--push` / `--server` / `--api-key` / `--dry-run` options as above.

Containers nothing outside the host can reach are skipped and counted in a note — no
published port, or every binding on a loopback address (`-p 127.0.0.1:5050:80`), which
only the host itself can reach. A binding pinned to one interface
(`-p 192.168.1.21:8443:8443`) is recorded at that address rather than the host's. Stopped
containers are never listed for the same reason — the daemon does not create host port
bindings until a container runs, so there is no address to record.

A container's compose project becomes a tag, so a stack stays grouped. `runsOn` points at
the same name `rpk discover system` produces on that machine, so running both gives you a
connected tree.

### Remote and rootless daemons

```bash
# A remote daemon behind a read-only socket proxy
rpk discover docker --docker-host tcp://192.168.1.20:2375 --host nas01

# Podman speaks the same API
rpk discover docker --docker-host unix:///run/user/1000/podman/podman.sock
```

For remote hosts, exposing the socket through a read-only proxy such as
[tecnativa/docker-socket-proxy](https://github.com/Tecnativa/docker-socket-proxy) with
only `CONTAINERS=1` is the safer arrangement — the same one the
[docker-gen guide](/docs/docker-gen-guide) describes.

Over TCP the machine running the command is not the machine running the containers, so
nothing probed locally is attributed to the engine. Instead the engine is asked about
itself (`GET /info`): its daemon id seeds the services' identities — the same ids no
matter which machine runs the command — and its hostname is what `runsOn` points at,
which is the same name `rpk discover system` reports on that box. Services are recorded
at the endpoint's address (resolved once if you dialled a name). No System resource is
emitted for the host itself; document it with `rpk discover system` on that machine, or
by hand, and the services attach to it by name.

If you rename that host in RackPeek, re-discovery keeps your link: an update whose
`runsOn` points at nothing that exists leaves the stored link alone. A `runsOn` that
does name a real resource is recorded — that is a genuine move.

A proxy restricted to `CONTAINERS=1` blocks `/info`, and discovery says so and degrades:
the endpoint itself becomes the identity seed (so keep addressing the engine the same
way — switching between an IP and a hostname would re-mint every id), and `--host` is
how to name the machine the containers run on. Allowing `INFO=1` on the proxy removes
both caveats.

---

## `rpk discover proxmox`

Reads a Proxmox cluster and emits its nodes and guests as Systems, with `runsOn`
already pointing each guest at the node it runs on. That tree is the tedious part to
type by hand, and it is the reason this collector is worth more than its fields suggest:
one call inventories the whole estate without installing anything on the guests.

```bash
rpk discover proxmox --host https://pve.lan:8006 --insecure
```

| Option | Meaning |
|---|---|
| `--host <URL>` | Proxmox host. A bare name gets `https://` and `:8006`. |
| `--token-id <ID>` | API token id, e.g. `root@pam!rackpeek`. Defaults to `RPK_PVE_TOKEN_ID`. |
| `--token-secret <SECRET>` | Token secret. Defaults to `RPK_PVE_TOKEN_SECRET`. |
| `--insecure` | Accept a self-signed certificate. |

Plus the same `--push` / `--server` / `--api-key` / `--dry-run` options as above.

### Making a token

In the Proxmox UI: **Datacenter → Permissions → API Tokens → Add**. Give it a read-only
role (`PVEAuditor` is enough) and clear "Privilege Separation" only if you need to.

```bash
export RPK_PVE_TOKEN_ID='root@pam!rackpeek'
export RPK_PVE_TOKEN_SECRET='xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
rpk discover proxmox --host pve.lan --insecure --push
```

`--insecure` is needed more often than not: Proxmox ships with a self-signed certificate
and most installations keep it.

### What it reports

**A node becomes two resources**, because it is two things:

* a **Server** named after the node (`kepler`) — the machine, carrying its processor
  (model, cores and threads per socket), memory, physical disks with Proxmox's own
  nvme/ssd/hdd classification, and its GPUs;
* a **System** of type `hypervisor` (`kepler-pve`) — the Proxmox install running on that
  machine, carrying the PVE version.

Guests then run on the hypervisor, giving the full Hardware → System → System tree that
the graph views are built around.

```yaml
- kind: Server
  name: kepler
  cpus:
  - model: AMD Ryzen 5 5600G
    cores: 6
    threads: 12
  ram:
    size: 63
  drives:
  - type: nvme
    size: 932
  gpus:
  - model: GeForce RTX 3090
  - model: GeForce RTX 3090
- kind: System
  name: kepler-pve
  type: hypervisor
  os: Proxmox VE 8.2.2
  runsOn: [kepler]
- kind: System
  name: docker-01
  type: vm
  runsOn: [kepler-pve]
```

Each QEMU guest becomes a `vm` and each LXC guest a `container`, with its allocated
cores, memory and its Proxmox tags. A container with a static address keeps it; one on
DHCP reports none rather than a wrong one.

**Every disk is recorded, not just the boot one.** The guest list only reports the boot
disk, so a VM with a 64 GB root and a 2 TB data volume would otherwise appear as a 64 GB
machine; the guest's config is read for the full set. Container mount points count too.
Install media, detached volumes and the few megabytes of EFI or TPM scratch space are
left out. The storage backend says nothing about the underlying medium, so guest disks
carry a size but no nvme/ssd/hdd type — unlike the node's own disks, which Proxmox has
already classified.

Stopped guests are included — unlike a stopped container, a stopped VM is still a real
system with real resources.

A GPU passed through to a guest is recorded on the **Server**, not the guest — the card
is bolted into the host, and a System has nowhere to put one. Integrated graphics are
included too, since they are equally present. VRAM is not something the PCI device list
knows, so it is left off.

The guest that holds a card gets a `gpu` **label** naming it, so the assignment is
visible from either end:

```yaml
- kind: System
  name: ai
  type: vm
  labels:
    gpu: GeForce RTX 3090, GeForce RTX 3090
  runsOn: [kepler-pve]
```

A label rather than a field, because RackPeek has no first-class way to say "this device
is assigned to that system". PCI addresses repeat on every machine, so a guest is only
ever matched against cards in the node it actually runs on.

The hardware detail needs the same permission as the node status call. Without it you
still get the Server, the hypervisor and the whole guest tree — just without the
processor and disks.

Running `rpk discover system --with-hardware` on the node itself gives better hardware
data still, since it reads real DMI rather than Proxmox's second-hand view.

### Identity

A guest is identified by its vmid within the cluster, so renaming it in Proxmox, or
migrating it between nodes, still updates the same RackPeek resource. A standalone host
with no cluster uses its node name as the scope instead.

> **Known limitation.** Proxmox identifies a guest by vmid; the guest identifies itself
> by its machine-id. Neither can derive the other, so running both `rpk discover proxmox`
> and `rpk discover system` *inside* the same guest produces two resources rather than
> one. The second is reported as an addition with a suffixed name, so it is visible
> rather than silent — but pick one collector per guest for now.

---

## Reviewing before you commit to it

`--dry-run` asks the server what would change and writes nothing:

```bash
rpk discover system --dry-run
```

```text
updated nas01
Dry run — nothing was written to http://rack.lan:8080.
```

Without a server, redirect the output and read it:

```bash
rpk discover docker > services.yaml
```

Then import it through the web UI's **Import YAML** tool, which shows you the same diff.
