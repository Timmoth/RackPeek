# Hardware Templates

Hardware templates provide pre-filled specifications for well-known hardware models. Instead of manually entering every port, CPU, drive, and NIC when adding a new device, you can reference a template and get an accurately populated resource in seconds.

## Why templates?

When you add a new UniFi USW-Enterprise-24 to your rack, you already know its exact port layout, PoE capability, and management features. Templates capture this shared knowledge so every RackPeek user benefits from accurate, consistent data without repetitive manual entry.

Templates are especially valuable for:

- **Network gear** (switches, routers, firewalls, access points) where port counts, speeds, and capabilities are fixed per model.
- **Mini-PCs and commodity servers** (Minisforum, GMKtec, Beelink) where CPU, RAM, drive, and NIC configurations ship as a known specification.
- **UPS units** where VA ratings are standardised per model.

## Using templates

### CLI

Add a resource from a template with the `--template` (or `-t`) flag:

```bash
rpk servers add home-nuc --template Minisforum-MS-01-13900H
rpk switches add core-switch --template UniFi-USW-Enterprise-24
rpk routers add edge-router --template UniFi-UDM-Pro-Max
rpk firewalls add main-fw --template Netgate-6100
rpk accesspoints add office-ap --template UniFi-U7-Pro
rpk ups add rack-ups --template APC-SmartUPS-2200
```

The `--template` value is the model name (the YAML filename without `.yaml`).

### Web UI

When adding a hardware resource through the web interface, a template dropdown appears automatically. Select a template to pre-fill all specifications, then customise the name and any fields as needed.

## Browsing available templates

List all bundled templates:

```bash
rpk templates list
```

Filter by kind:

```bash
rpk templates list --kind Switch
rpk templates list --kind Server
rpk templates list --kind Router
```

View detailed specifications for a specific template:

```bash
rpk templates show Server/Minisforum-MS-01-13900H
rpk templates show Switch/UniFi-USW-Enterprise-24
rpk templates show Router/UniFi-UDM-Pro-Max
```

The template ID format is `Kind/Model` (e.g. `Server/Minisforum-MS-01-13900H`).

## Contributing a template

We welcome community contributions of hardware templates. The more templates we have, the easier it is for everyone to document their infrastructure accurately.

There are two ways to create a template: export one from a resource you have already added in the Web UI, or write the YAML file by hand.

### Option A — Copy as Template from the Web UI

The fastest way to contribute is to export a template directly from an existing resource:

1. **Add the hardware** to your RackPeek instance through the Web UI and fill in its specifications (CPU, RAM, drives, NICs, ports, etc.).
2. Open the resource's detail card and click the gold **Copy as Template** button (next to Rename, Clone, and Delete).
3. A dialog asks for the **official hardware name** from the vendor (e.g. `Minisforum-MS-01`). Enter it and press Submit.
4. The generated template YAML is copied to your clipboard.
5. Save the clipboard contents to a new `.yaml` file in the appropriate `templates/{kind-plural}/` directory, using the hardware name as the filename.
6. Validate and submit (see [Validate your template](#validate-your-template) and [Submit a pull request](#submit-a-pull-request) below).

### Option B — Write the YAML by hand

If you prefer, you can create the template file manually. Copy an existing template in the same `templates/{kind-plural}/` directory as a starting point, then update the fields to match the new hardware model's specifications from the vendor's product page.

### Directory layout

Templates are stored in `templates/{kind-plural}/` where `{kind-plural}` is the lowercase plural form of the resource kind:

```
templates/
├── accesspoints/
│   ├── UniFi-U6-Lite.yaml
│   ├── UniFi-U6-Pro.yaml
│   ├── UniFi-U7-Lite.yaml
│   └── ...
├── firewalls/
│   ├── Netgate-1100.yaml
│   └── Netgate-6100.yaml
├── routers/
│   ├── MikroTik-hEX-S.yaml
│   ├── UniFi-UDM-Pro-Max.yaml
│   ├── UniFi-UCG-Fiber.yaml
│   └── ...
├── servers/
│   ├── Minisforum-MS-01-13900H.yaml
│   ├── GMKtec-NucBox-K8-Plus.yaml
│   ├── Beelink-EQ14.yaml
│   └── ...
├── switches/
│   ├── UniFi-USW-Enterprise-24.yaml
│   ├── UniFi-USW-Pro-HD-24-PoE.yaml
│   └── ...
└── ups/
    └── APC-SmartUPS-2200.yaml
```

### Naming conventions

- **Filename**: Use the hardware model name with hyphens instead of spaces. The filename (without `.yaml`) becomes the template's model identifier.
  - Good: `UniFi-USW-Enterprise-24.yaml`, `Minisforum-MS-01-13900H.yaml`
  - Bad: `unifi switch.yaml`, `my_switch.yaml`
- **`name` field**: Set to the same value as the filename (without extension). This is a placeholder — users override it when creating a resource.
- **`model` field**: Required for switches, routers, firewalls, and access points. For servers, the model is derived from the filename automatically.

### YAML format by kind

Every template requires a `kind` and `name` field. Additional fields depend on the resource kind.

#### Server

```yaml
kind: Server
name: Minisforum-MS-01-13900H
cpus:
  - model: Intel Core i9-13900H
    cores: 14
    threads: 20
ram:
  size: 32
  mts: 5200
drives:
  - type: nvme
    size: 1024
nics:
  - type: sfp+
    speed: 10
    ports: 2
  - type: rj45
    speed: 2.5
    ports: 2
gpus:
  - model: Intel Iris Xe Graphics
    vram: 0
ipmi: true
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `cpus` | list | No | CPU entries with `model`, `cores`, `threads` |
| `ram` | object | No | `size` (GB) and `mts` (memory transfer speed) |
| `drives` | list | No | Drive entries with `type` (nvme, ssd, hdd, sas, sata, usb, sdcard, micro-sd) and `size` (GB) |
| `nics` | list | No | NIC entries with `type` (rj45, sfp, sfp+, etc.), `speed` (Gbps), `ports` |
| `gpus` | list | No | GPU entries with `model` and `vram` (GB) |
| `ipmi` | boolean | No | Whether the server has IPMI/BMC management |

#### Switch

```yaml
kind: Switch
name: UniFi-USW-Enterprise-24
model: UniFi-USW-Enterprise-24
ports:
  - type: rj45
    speed: 1
    count: 12
  - type: rj45
    speed: 2.5
    count: 8
  - type: sfp+
    speed: 10
    count: 4
managed: true
poe: true
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `model` | string | Yes | Hardware model identifier |
| `ports` | list | No | Port entries with `type`, `speed` (Gbps), `count` |
| `managed` | boolean | No | Whether the switch is managed |
| `poe` | boolean | No | Whether the switch supports PoE |

#### Router

```yaml
kind: Router
name: UniFi-UDM-Pro-Max
model: UniFi-UDM-Pro-Max
ports:
  - type: rj45
    speed: 1
    count: 8
  - type: rj45
    speed: 2.5
    count: 1
  - type: sfp+
    speed: 10
    count: 2
managed: true
poe: false
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `model` | string | Yes | Hardware model identifier |
| `ports` | list | No | Port entries with `type`, `speed` (Gbps), `count` |
| `managed` | boolean | No | Whether the router is managed |
| `poe` | boolean | No | Whether the router supports PoE |

#### Firewall

```yaml
kind: Firewall
name: Netgate-6100
model: Netgate-6100
ports:
  - type: rj45
    speed: 1
    count: 4
  - type: sfp+
    speed: 10
    count: 2
managed: true
poe: false
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `model` | string | Yes | Hardware model identifier |
| `ports` | list | No | Port entries with `type`, `speed` (Gbps), `count` |
| `managed` | boolean | No | Whether the firewall is managed |
| `poe` | boolean | No | Whether the firewall supports PoE |

#### Access Point

```yaml
kind: AccessPoint
name: UniFi-U7-Pro
model: UniFi-U7-Pro
speed: 2.5
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `model` | string | Yes | Hardware model identifier |
| `speed` | number | No | Maximum link speed in Gbps |

#### UPS

```yaml
kind: Ups
name: APC-SmartUPS-2200
model: APC-SmartUPS-2200
va: 2200
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `model` | string | Yes | Hardware model identifier |
| `va` | number | No | Volt-ampere rating |

### Validate your template

Before submitting, validate your template against the schema for its `kind`.

**Single file:**

```bash
rpk templates validate path/to/your-template.yaml
```

**All templates at once (recommended before submitting a PR):**

```bash
just validate-templates
```

The validator checks:
- The file is valid YAML with a recognised `kind`
- Required fields are present (`name`, `model` where applicable)
- Sub-resource types are valid (drive types, NIC types, port types)
- Numeric values are non-negative

A passing result looks like:

```
Valid: your-template.yaml passes all checks.
```

If there are problems, each error is listed individually so you can fix them before submitting.

### Submit a pull request

1. **Fork** the [RackPeek repository](https://github.com/Timmoth/RackPeek).
2. **Create** a new YAML file in the appropriate `templates/{kind-plural}/` directory, following the naming conventions above.
   - Use the Web UI's **Copy as Template** button to export from an existing resource, or write the YAML by hand using an existing template as reference.
3. **Validate** your template locally:
   ```bash
   just build
   rpk templates validate templates/servers/YourModel.yaml
   just validate-templates
   ```
4. **Spot-check** the template loads and creates a resource correctly:
   ```bash
   rpk templates show Server/YourModel
   rpk servers add test-resource --template YourModel
   rpk servers describe test-resource
   ```
5. **Run tests** to ensure nothing is broken:
   ```bash
   just test-cli
   ```
6. **Open a pull request** with a clear title (e.g. "Add template: Minisforum MS-01 server").

### Validation checklist

Before submitting your template, verify:

- [ ] `kind` matches one of: `Server`, `Switch`, `Router`, `Firewall`, `AccessPoint`, `Ups`
- [ ] `name` matches the filename (without `.yaml`)
- [ ] `model` is set (for all kinds except Server, where the filename is used)
- [ ] Numeric values use correct units (GB for storage/RAM, Gbps for network speed)
- [ ] Port types are valid: `rj45`, `sfp`, `sfp+`, `sfp28`, `qsfp+`, `qsfp28`, `qsfp-dd`, `osfp`
- [ ] Drive types are valid: `nvme`, `ssd`, `hdd`, `sas`, `sata`, `usb`, `sdcard`, `micro-sd`
- [ ] `rpk templates validate` passes with no errors
- [ ] `just validate-templates` passes all templates
- [ ] The template loads correctly with `rpk templates show Kind/Model`
- [ ] A resource created from the template has the expected specifications

### Custom template directories

By default, RackPeek loads templates from the bundled `templates/` directory shipped with the application. You can also point to an additional directory of templates by setting the `RPK_TEMPLATES_DIR` environment variable:

```bash
export RPK_TEMPLATES_DIR=/path/to/my/templates
rpk templates list
```

This is useful for organisations that maintain a private library of templates for internal hardware.
