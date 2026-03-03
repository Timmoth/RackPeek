# Hosts File Export

RackPeek can generate a ready-to-use `/etc/hosts` file from your infrastructure model.

This is useful when you:

* Don’t run internal DNS
* Want simple local name resolution
* Are working in an air-gapped lab
* Need consistent host mappings across machines

RackPeek stays the source of truth. Your hosts file just reflects it.

---

## 1. Make a Resource Eligible

A resource is included if it has an address.

Define at least one:

```yaml
labels:
  ip: 192.168.1.20
```

or

```yaml
labels:
  hostname: server01.local
```

(If you already use Ansible, `ansible_host` also works.)

If no address is present, the resource is skipped.

---

## 2. Example Resource

```yaml
- kind: System
  name: vm-web01
  tags:
    - prod
  labels:
    ip: 192.168.1.20
```

---

## 3. Generated Output

RackPeek produces standard hosts entries:

```text
127.0.0.1 localhost
::1 localhost

192.168.1.20 vm-web01
192.168.1.30 vm-db01
```

With a domain suffix:

```
--domain-suffix home.local
```

You’ll get:

```text
192.168.1.20 vm-web01.home.local
```

---

## 4. CLI Example

```bash
rpk export hosts \
  --include-tags prod \
  --domain-suffix home.local \
  --output hosts.txt
```

---

## 5. Apply It

On macOS or Linux:

```bash
sudo cp hosts.txt /etc/hosts
```

Now you can:

```bash
ping vm-web01
```

No DNS required.