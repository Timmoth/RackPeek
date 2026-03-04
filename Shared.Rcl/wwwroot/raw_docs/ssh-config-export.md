# SSH Config Export

RackPeek can generate a ready-to-use `~/.ssh/config` file from your modeled infrastructure.

That means:

* No manually editing SSH config
* No memorizing IP addresses
* Just `ssh vm-name` and go
* RackPeek stays your single source of truth

Works natively on macOS and Linux.

---

## 1. Make a Resource SSH-Ready

A resource will be included in the SSH export if it has an address.

At minimum, define one of:

```yaml
labels:
  ip: 192.168.1.20
```

or

```yaml
labels:
  hostname: server01.local
```

If you already use Ansible:

```yaml
labels:
  ansible_host: 192.168.1.20
```

If no address is found, the resource is skipped.

---

## 2. Optional SSH Settings

You can control how SSH connects using labels:

```yaml
labels:
  ip: 192.168.1.20
  ssh_user: ubuntu
  ssh_port: 2222
  ssh_identity_file: ~/.ssh/id_rsa
```

If `ssh_user` or `ssh_port` are missing, RackPeek falls back to:

* `ansible_user`
* `ansible_port`
* CLI defaults (if provided)

---

## 3. Example Resource

```yaml
- kind: System
  type: vm
  name: vm-web01
  tags:
    - prod
    - linux
  labels:
    ip: 192.168.1.20
    ssh_user: ubuntu
```

---

## 4. What Gets Generated

```ssh
Host vm-web01
  HostName 192.168.1.20
  User ubuntu
  IdentityFile ~/.ssh/id_rsa
```

Now you can connect with:

```
ssh vm-web01
```

No IP address needed.

---

## 5. Filtering by Tags

You can export only specific systems:

```
rpk export ssh --include-tags prod
```

Only resources tagged `prod` will be included.

---

## 6. CLI Example

```
rpk export ssh \
  --include-tags prod,linux \
  --default-user ubuntu \
  --default-port 22 \
  --default-identity ~/.ssh/id_rsa \
  --output ssh_config
```

---

## 7. Apply the Config

Append to your existing SSH config:

```
cat ssh_config >> ~/.ssh/config
```

Or replace it:

```
mv ssh_config ~/.ssh/config
chmod 600 ~/.ssh/config
```

Permissions must be `600`, or SSH will ignore the file.

---

## 8. How Naming Works

By default, each host alias is the resource name:

```
Host vm-web01
```

RackPeek automatically:

* Lowercases names
* Replaces spaces with `-`
* Removes unsupported characters

Example:

```
vm web 01
```

Becomes:

```
vm-web-01
```

---

## 9. Fallback Logic (Simple Version)

RackPeek resolves values in this order:

**Address**

1. `ip`
2. `hostname`
3. `ansible_host`

**User**

1. `ssh_user`
2. `ansible_user`
3. CLI default

**Port**

1. `ssh_port`
2. `ansible_port`
3. CLI default

**Identity**

1. `ssh_identity_file`
2. CLI default

---

## 10. Best Practices

* Always define an `ip` or `hostname`
* Prefer `ssh_user` over reusing unrelated labels
* Use tags like `prod`, `staging`, `linux` for filtering
* Keep resource names clean and predictable

---

## 11. Example Final Output

```ssh
Host vm-db01
  HostName 192.168.1.30
  User postgres
  Port 2222
  IdentityFile ~/.ssh/id_rsa

Host vm-web01
  HostName 192.168.1.20
  User ubuntu
  IdentityFile ~/.ssh/id_rsa
```

And that’s it:

```
ssh vm-db01
ssh vm-web01
```