# Auto Discovery Guide

`rpk discover` reads your infrastructure and writes it out as RackPeek YAML, so you
don't have to type in what the machine already knows about itself.

| Command | Reads | Produces |
|---|---|---|
| `rpk discover system` | the machine it runs on | one **System** resource |
| `rpk discover docker` | the Docker Engine API | one **Service** per published container, plus the **System** they run on |
| `rpk discover proxmox` | a Proxmox VE cluster | a **Server** and **System** per node, a **System** per guest, already wired together |
| `rpk discover opnsense` | an OPNsense firewall's neighbour table | one **System** per machine it has seen, on every subnet it routes |
| `rpk discover network` | a subnet, from outside | one **System** per host that answers, plus a **Service** for each web application it recognises |

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

* a **Server** named after the node (`pve-node-01`) — the machine, carrying its processor
  (model, cores and threads per socket), memory, physical disks with Proxmox's own
  nvme/ssd/hdd classification, and its GPUs;
* a **System** of type `hypervisor` (`pve-node-01-pve`) — the Proxmox install running on that
  machine, carrying the PVE version.

Guests then run on the hypervisor, giving the full Hardware → System → System tree that
the graph views are built around.

```yaml
- kind: Server
  name: pve-node-01
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
  name: pve-node-01-pve
  type: hypervisor
  os: Proxmox VE 8.2.2
  runsOn: [pve-node-01]
- kind: System
  name: docker-01
  type: vm
  runsOn: [pve-node-01-pve]
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
  runsOn: [pve-node-01-pve]
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

### Where a guest's address comes from

Proxmox only records an address in a guest's config when someone set one statically, so
on a DHCP estate the config knows nothing. The guest itself does, and will say so: a VM
through its **qemu-guest-agent**, a container through its running interfaces. Discovery
asks every *running* guest, which is one extra call per guest and nothing at all for one
that is switched off.

A guest that runs containers has several interfaces — Docker's `docker0` and its
per-network bridges, Home Assistant's `hassio`, any VPN tunnel — and recording
`172.17.0.1` as the machine's address would be worse than recording nothing, because
every container host on the estate reports the same one. So the NIC MACs Proxmox assigned
are used as the discriminator: an interface carrying one is a NIC the hypervisor gave the
guest, anything else is something the guest invented. Where the MACs cannot be read,
nothing is claimed.

No agent, a stopped guest, or a token without `VM.Monitor` all mean the same thing —
the address stays unknown, exactly as before.

**Why it matters beyond the address itself.** A guest's address is what lets
`rpk discover network` recognise it. ARP is link-local, so a sweep of any subnet but its
own gets no MAC and can only identify a host by address; once the hypervisor has reported
that same address, the sweep's find is matched to the guest the hypervisor already
described in full rather than becoming a second, emptier card beside it.

---

## `rpk discover opnsense`

The collector for everything a sweep can see but not identify.

ARP is link-local. Sweeping from one machine gets you a MAC for that machine's own
segment and nothing but an address for every other subnet — and an address alone cannot
survive a DHCP re-lease or be matched against anything else you have documented. The
firewall routes every subnet, so its neighbour table has the MAC for all of them, plus
the name it handed out and which leg it was seen on.

```bash
# Look at what the firewall knows
rpk discover opnsense --host https://firewall.lan --insecure

# Merge it into your server
rpk discover opnsense --host firewall.lan --push
```

| Option | Meaning |
|---|---|
| `--host <URL>` | The firewall. A bare name gets `https`. |
| `--key <KEY>` | API key. Defaults to `RPK_OPN_KEY`. |
| `--secret <SECRET>` | API secret. Defaults to `RPK_OPN_SECRET`. |
| `--insecure` | Accept the self-signed certificate OPNsense ships with. |
| `--include-public` | Also record neighbours on public addresses (see below). |

Plus the same `--push` / `--server` / `--api-key` / `--dry-run` options as every other
collector.

Create the credentials in the firewall under **System → Access → Users**, on a user that
holds the **Diagnostics: ARP Table** privilege. Read-only is enough; nothing here writes.

### What it records, and what it leaves out

One **System** per machine, carrying its address, its MAC, the vendor that MAC belongs
to, and a `segment` label naming the firewall leg it answered on — which is the closest
thing to "which VLAN is this on" that the firewall can tell you.

Left out on purpose:

* **The firewall's own addresses.** Every routed subnet contributes one and they are all
  the same box — which is a Firewall, not the handful of Systems this would invent.
* **Entries that have aged out.** They say where something used to be.
* **Broadcast and multicast addresses**, which no machine owns.
* **Neighbours on public addresses**, such as the ISP equipment on the WAN leg. They are
  not your infrastructure, and recording one would put a public address into a file you
  may well commit. Pass `--include-public` if you are documenting a fleet that lives on
  them.

A machine answering on two of the firewall's legs is one card, not two: its identity is
its MAC.

### Why it lines up with everything else

A card from the firewall is seeded exactly as `rpk discover network` seeds its own —
keyed on the MAC — because both describe the same thing by the same evidence: a machine
observed on the network rather than asked about itself. So a host the firewall knows and
a host a sweep found are **one card**, whichever collector ran first, with no special
case anywhere to say so. Run both and the firewall fills in the identity a sweep of a
routed subnet could never get.

One wrinkle worth knowing: names are yours, so discovery never renames a resource that
already exists — including one a sweep named `host-<hash>` before the firewall could
offer something better. Running the firewall collector first, or on a fresh inventory,
gets you the good names.

---

## `rpk discover network`

The collector for machines nothing else can describe: no agent, no API — just an
address that answers. It sweeps a subnet and emits one **System** per responding host —
with its IP, a name, its MAC address, and the vendor that MAC belongs to — plus a
**Service** for each web application that names itself.

```bash
# Sweep this machine's own subnet and look at the result
rpk discover network

# Sweep a specific block, then merge it into the server
rpk discover network --cidr 192.168.1.0/24 --push
```

### What "answering" means

A host counts as alive when it replies to ping **or** accepts a TCP connection on any
probed port — plenty of gear drops ICMP, so ping alone would miss half a homelab. The
default port list is a curated homelab set (ssh, http/https, dns, smb, rdp, ipp,
proxmox, and friends); `--ports 22,80,443` narrows or widens it. The ports are only a
liveness check: the sweep records that the host exists, not what it serves — pair it
with `rpk discover docker` or hand-written Service cards for that.

### Where a scanned host's name comes from

A homelab rarely has PTR records for everything, and a page of `host-1a2b3c4d` cards is
not documentation. So once a host is known to be alive, the sweep asks it what it is,
taking the first answer from:

1. **A reverse-DNS (PTR) record** — the network's own answer, so it always wins.
2. **A TLS certificate's Common Name**, read from 443, 8006 or 8443. The strongest
   remaining evidence, because appliances ship a certificate naming themselves: a
   Proxmox node presents `CN=pve-node-01.example.com`, OPNsense presents its hostname.
   The certificate is read, never trusted — self-signed is the norm here.
3. **An SSH greeting** on 22, reduced to the software (`SSH-2.0-dropbear` → `dropbear`).
   That names what the host runs rather than the host, which still separates an
   embedded appliance from a general-purpose box.
4. **An HTTP page title or `Server` header**, on the usual web ports and on anything else
   found open — how a Home Assistant or a Forgejo announces itself. Titles are trimmed to
   the phrase before their first separator, so "Forgejo: Beyond coding. We Forge." names a
   machine `forgejo`, and the titles of error pages are ignored entirely.

Whatever answered is recorded in an `identified-by` label (`tls:8006 pve-node-01.example.com`)
so you can see where a name came from and judge it. Nothing is sent to the host beyond a
bare `GET /`, and a host that stays silent simply keeps its generated name.

Identification costs a handful of short connections per *living* host — never per
address — and `--no-identify` turns it off for a pure liveness sweep.

### Open ports

The liveness sweep stops at the first answer, because it only needs to know the host
exists. Once a host has answered, it is checked against a wider list — cameras (554),
MQTT brokers (1883), Home Assistant (8123), Portainer (9000), Plex, Postgres and so on —
and whatever is open lands in an `open-ports` label.

These are recorded as observations, not conclusions. "554 is open" is a fact; "this is a
camera" is an inference, and the person reading the card is far better placed to draw it
than the scanner is. A port number is a convention rather than a guarantee, so RackPeek
will not name a service from one — but a port **is** the best possible target for the
banner probes above, which is how `9000` became "Portainer" and `8123` became
"Home Assistant".

The list is what answered out of the ports probed, not a full port scan. `--ports`
widens the liveness set if you want more.

### Applications become Services

When a port answers HTTP with something that names itself, that is a fact about what the
host **runs**, not about what the host **is** — so it becomes a Service hanging off the
host's card rather than renaming it:

```yaml
- kind: System
  ip: 192.0.2.204
  name: host-1a2b3c4d
  labels:
    open-ports: 1883,8123
    identified-by: http:8123 Home Assistant
- kind: Service
  name: home-assistant
  network: { ip: 192.0.2.204, port: 8123, protocol: TCP }
  runsOn: [host-1a2b3c4d]
```

A host may run several, so each identified port gets its own Service with its own stable
id — a rescan updates them rather than duplicating them. This is why a page title does
not name the machine: picking whichever application answered first would be arbitrary,
and a certificate is the only answer that is a claim about the machine itself.

An appliance's own management page is not a service running on it, so a title matching
the host's own name is skipped — a firewall already called `opnsense` does not also need
a service called `opnsense`.

### Vendor from the MAC

Where the sweep has a MAC, the card also gets a `vendor` label naming the organisation
that OUI belongs to — `Espressif`, `Ubiquiti`, `Raspberry Pi`, `Proxmox`. For a silent
device with no PTR record and no web UI, this is often the only thing that distinguishes
it from an address.

Two details worth knowing. A hypervisor's own prefix wins over the
locally-administered bit, so a KVM guest reads as `QEMU/KVM` rather than anonymous. And
an address a device made up for itself — modern phones and laptops randomise per network
for privacy — is reported as `Randomised (locally administered)`, because the OUI half of
such an address names nobody and a lookup would otherwise attribute it to whichever
company happens to own the matching block.

The table is a curated subset of the IEEE registry covering the gear that turns up on a
homelab, not all 40,000 assignments; an unknown prefix yields no label rather than a
guess. Run `./generate-oui-table.py` against the registry to extend it.

Sweeps are capped at a /16 (65,534 addresses). `--timeout` and `--parallel` tune how
patient and how aggressive the sweep is; the defaults finish a quiet /24 in seconds.

A network of several VLANs is several sweeps — each merges into the same inventory,
and the ids keep re-runs honest:

```bash
rpk discover network --cidr 192.168.10.0/24 --push   # the LAN
rpk discover network --cidr 192.168.50.0/24 --push   # the server VLAN
```

### Identity

A scanned host is identified by its **MAC address**, read from the ARP table the
sweep itself populates — so a DHCP re-lease updates the same resource's address rather
than inventing a new machine. One MAC answering on several addresses (a gateway's
VIPs and aliases) is still one machine and becomes **one card**: the lowest address as
its `ip`, every address in an `ips` label — so a VIP failing over never moves the
machine's identity. Two caveats:

- **Hosts beyond the local segment have no ARP entry** (a routed VLAN, a VPN subnet).
  Their identity falls back to the IP address, and the command says so — a DHCP
  re-lease will then look like a new machine. Scan from a machine on the same segment
  when you can. On a statically-addressed subnet — a server VLAN, say — the IP
  fallback is stable in practice and nothing more is needed.
- **A scan sees an address, not an operating system.** Scanned cards deliberately carry
  no type, OS, cores or RAM, so a re-scan can never overwrite the details you (or an
  agent collector) filled in afterwards.

### One machine, one card — across collectors

`rpk discover system` records the machine's physical MAC addresses (a `macs` label),
and a scan identifies machines by exactly those MACs — so **the two collectors land on
the same card**, whichever ran first:

- Scan first: the sweep creates the card; when the agent later runs on that box, it
  claims the card, fills in the OS/cores/RAM, and upgrades its identity to the
  machine-id. Every rescan afterwards keeps updating that same card via the MAC.
- Agent first: a later sweep recognises the box and just refreshes its address —
  never touching the identity or anything you or the agent wrote.

The card keeps whatever name it already had (names are always user-owned), so a
scan-first card keeps its generated `host-…` name until you rename it once. A MAC that
two stored cards both claim unifies nothing — ambiguity always falls back to separate
cards — and agent-grade identities never unify with each other on a MAC alone (cloned
VMs can share one; that is what machine-ids are for). The machine running the sweep
finds itself, and unifies with its own `rpk discover system` card the same way.

`rpk discover proxmox` joins the bridge for **guests**: a guest's config names the
NIC MACs Proxmox assigned it, so a VM or container found by a sweep and the same guest
reported by the Proxmox collector become one card too. Nodes stay outside the bridge
(the API exposes no host MACs we read), and a guest documented both by Proxmox and by
`rpk discover system` *inside* it remains two cards — vmid and machine-id are both
agent-grade identities, and MACs alone never unify those.

### Being a good citizen

The sweep is a burst of pings and TCP connection attempts — the polite end of network
scanning, but scan networks you operate, not networks you merely use.

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
