# Ansible Inventory Generator User Guide

RackPeek can generate production-ready Ansible inventory directly from your modeled infrastructure.

---

# 1. Making a Resource Ansible-Ready

A resource becomes an Ansible host when it has an address label.

## Required Label

At minimum, set:

```yaml
labels:
  ansible_host: 192.168.1.10
```

Without this, the resource will not appear in inventory.

RackPeek will also accept these alternatives if `ansible_host` is not provided:

| Source              | Used As      |
| ------------------- | ------------ |
| `ip` label          | ansible_host |
| `hostname` label    | ansible_host |
| a System's own `ip` | ansible_host |

So a System that carries an address — hand-written or found by
[`rpk discover network`](/docs/discovery-guide) — is addressable without any labels;
an explicit `ansible_host` label always wins when both are present.

Example:

```yaml
labels:
  ip: 192.168.1.10
```

---

# 2. Standard Ansible Labels

RackPeek automatically exports any label beginning with **`ansible_`** as an Ansible host variable.

Example:

```yaml
labels:
  ansible_host: 192.168.1.10
  ansible_user: ubuntu
  ansible_port: 22
  ansible_ssh_private_key_file: ~/.ssh/id_rsa
```

### What these do

| Label                        | Purpose          |
| ---------------------------- | ---------------- |
| ansible_host                 | IP or DNS target |
| ansible_user                 | SSH user         |
| ansible_port                 | SSH port         |
| ansible_ssh_private_key_file | SSH key          |

These variables appear directly in the generated inventory.

---

# 3. Custom Host Variables (`ansible_var_*`)

RackPeek supports exposing **custom variables** to Ansible playbooks using the label prefix:

```
ansible_var_
```

The prefix is removed when generating inventory.

### Example

```yaml
labels:
  ansible_host: 10.0.0.10
  ansible_var_mac: 52:54:00:11:22:33
  ansible_var_rack: rack01
```

Generated inventory:

```yaml
cerberus-0:
  ansible_host: 10.0.0.10
  mac: 52:54:00:11:22:33
  rack: rack01
```

This allows RackPeek to remain the **source of truth for infrastructure metadata** while making the data available to playbooks.

### Example Playbook Usage

```yaml
- hosts: all
  gather_facts: false

  tasks:
    - name: Copy ignition file
      ansible.builtin.copy:
        src: "output/{{ inventory_hostname }}.ign"
        dest: "/srv/ignition/{{ mac }}.ign"
```

---

# 4. Using Tags for Grouping

Tags are simple grouping mechanisms.

Example:

```yaml
tags:
- prod
- web
- ansible
```

If you generate inventory with:

```
--group-tags prod,web
```

You will get:

```ini
[prod]
vm-web01 ...

[web]
vm-web01 ...
```

---

# 5. Using Labels for Structured Groups

Labels allow structured grouping.

Example:

```yaml
labels:
  env: prod
  role: database
```

Generating with:

```
--group-labels env,role
```

Produces:

```ini
[env_prod]
vm-db01 ...

[role_database]
vm-db01 ...
```

This is cleaner and more scalable than raw tags.

---

# 6. Example Resource

```yaml
- kind: System
  type: vm
  os: ubuntu-22.04
  cores: 4
  ram: 8
  name: vm-web01

  tags:
  - prod
  - web

  labels:
    ansible_host: 192.168.1.10
    ansible_user: ubuntu
    ansible_var_mac: 52:54:00:11:22:33
    env: prod
    role: web
```

---

# 7. Generating Inventory

## CLI

```
rpk ansible inventory \
  --group-tags prod,web \
  --group-labels env,role \
  --global-var ansible_user=ansible \
  --global-var ansible_python_interpreter=/usr/bin/python3
```

## Web UI

Navigate to:

```
/ansible/inventory
```

Set:

* Group By Tags
* Group By Labels
* Global Variables

Click **Generate**.

---

# 8. Example Generated Inventory

```ini
[all:vars]
ansible_python_interpreter=/usr/bin/python3
ansible_user=ansible

[env_prod]
vm-web01 ansible_host=192.168.1.10 ansible_user=ubuntu mac=52:54:00:11:22:33

[role_web]
vm-web01 ansible_host=192.168.1.10 ansible_user=ubuntu mac=52:54:00:11:22:33

[prod]
vm-web01 ansible_host=192.168.1.10 ansible_user=ubuntu mac=52:54:00:11:22:33
```

---

# 9. Writing Playbooks Against RackPeek Inventory

## Example 1 – Ping Production

```yaml
- name: Test production connectivity
  hosts: env_prod
  gather_facts: false

  tasks:
    - name: Ping hosts
      ansible.builtin.ping:
```

Run:

```
ansible-playbook -i inventory.ini ping.yml
```

---

## Example 2 – Deploy Web Servers

```yaml
- name: Configure web servers
  hosts: role_web
  become: true

  tasks:
    - name: Install nginx
      ansible.builtin.apt:
        name: nginx
        state: present
        update_cache: true

    - name: Ensure nginx running
      ansible.builtin.service:
        name: nginx
        state: started
        enabled: true
```

---

## Example 3 – Database Setup

```yaml
- name: Configure database servers
  hosts: role_database
  become: true

  tasks:
    - name: Install PostgreSQL
      ansible.builtin.apt:
        name: postgresql
        state: present
        update_cache: true
```

---

# 10. Best Practices

### 1. Use Labels for Structure

Prefer:

```
env: prod
role: web
```

Over raw tags when designing larger infrastructure.

---

### 2. Use `ansible_var_*` for Infrastructure Metadata

Examples:

```
ansible_var_mac
ansible_var_rack
ansible_var_datacenter
ansible_var_vlan
```

This allows playbooks to reference infrastructure information without duplicating configuration.

---

### 3. Keep Global Vars Minimal

Use:

```ini
[all:vars]
ansible_user=ansible
ansible_python_interpreter=/usr/bin/python3
```

Override per host only when needed.

---

### 4. Separate Infrastructure and Services

Model:

* Systems → Ansible hosts
* Services → Applications running on systems

Deploy against systems, not services.

---

### 5. Keep Inventory Deterministic

Avoid:

* Missing ansible_host
* Mixed case group names
* Unstructured tags

---

# 11. Advanced Pattern (Recommended)

Use both:

* `env`
* `role`

Then your structure becomes:

```
env_prod
env_staging
role_web
role_database
```

This allows extremely flexible play targeting:

```
ansible-playbook site.yml -l env_prod
ansible-playbook site.yml -l role_web
ansible-playbook site.yml -l env_prod:&role_web
```

---

# 12. Summary

To use RackPeek effectively with Ansible:

1. Add `ansible_host` label
2. Add `env` and `role` labels
3. Optionally add tags
4. Use `ansible_var_*` for custom host variables
5. Generate inventory
6. Write playbooks targeting groups