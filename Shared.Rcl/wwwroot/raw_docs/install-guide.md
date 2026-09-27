# Installation Guide

RackPeek can run in two ways:

* **Docker (includes Web UI + CLI)**
* **Native CLI binary**

RackPeek stores everything in a writable `config/` directory as YAML (including automatic backups).
Wherever you run it, that directory must be writable.

---

# Docker (Recommended)

This gives you:

* Web UI on port `8080`
* CLI available inside the container

---

## Docker Compose

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
    healthcheck:
      test: ["CMD", "curl", "-fsS", "http://localhost:8080/health"]
      interval: 30s
      timeout: 5s
      start_period: 15s
      retries: 3

volumes:
  rackpeek-config:
```

Start it:

```bash
docker compose up -d
```

Open:

```
http://localhost:8080
```

This uses a **named volume**, which avoids permission issues and is recommended for most users.

RackPeek stores its ASP.NET DataProtection keys under `config/.dataprotection` on the same volume, so browser sessions survive container upgrades. If you run the container with a **read-only root filesystem**, also mount a tmpfs for the app's temp directory:

```yaml
    read_only: true
    tmpfs:
      - /app/tmp:uid=1654,gid=0
```

---

## Portainer

Use the same Compose file above in a stack.
Portainer typically handles user permissions automatically.

---

## Bind Mount (Advanced)

If you want the YAML stored directly on your host:

```yaml
volumes:
  - /path/on/host/rackpeek:/app/config
```

⚠️ The directory must be writable.

If you see:

```
Access to the path '/app/config/config.yaml' is denied.
```

Fix ownership:

```bash
sudo chown -R 1000:1000 /path/on/host/rackpeek
```

Or explicitly set the container user:

```yaml
user: "1000:1000"
```

RackPeek must be able to:

* Create `config.yaml`
* Update it
* Write backup files

Permission issues are almost always the cause of startup failures.

---

## Using the CLI (Without Installing It)

If running Docker, you already have the CLI.

Run commands directly inside the container:

```bash
docker exec -it rackpeek rpk --help
docker exec -it rackpeek rpk systems list
```

# Native CLI (Linux Only)

If you prefer running RackPeek directly on Linux:

## Download

```bash
wget https://github.com/Timmoth/RackPeek/releases/download/RackPeek-2.2.0/rackpeek_2_2_0_linux-x64 -O rackpeek
```

Or:

```bash
curl -L https://github.com/Timmoth/RackPeek/releases/download/RackPeek-2.2.0/rackpeek_2_2_0_linux-x64 -o rackpeek
```

---

## Install

```bash
chmod +x rackpeek
sudo mv rackpeek /usr/local/bin/rpk
```

---

## Create Config Directory

RackPeek expects a `config` folder next to the binary:

```bash
sudo mkdir -p /usr/local/bin/config
sudo touch /usr/local/bin/config/config.yaml
```

⚠️ It must be writable, since RackPeek writes backups there as well.

If needed:

```bash
sudo chown -R $USER:$USER /usr/local/bin/config
```

---

## Test

```bash
rpk --help
```

---

# Where Your Data Lives

RackPeek stores everything in plain YAML:

```
config/
└── config.yaml
```

No database.
No telemetry.
No lock-in.

You own your data.