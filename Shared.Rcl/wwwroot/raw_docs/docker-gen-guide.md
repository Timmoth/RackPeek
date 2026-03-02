# `docker-gen` User Guide

Use [docker-gen](https://github.com/nginx-proxy/docker-gen) on a target machine to generate RackPeek [**Services**](/docs/resource-levels#services) YAML from existing docker containers.

The generated YAML can be directly used in RackPeek or with the **Import YAML** tool to merge/replace existing services.

# 1. Setup `docker-gen` project

Clone the RackPeek repoistory or copy the [`docker-gen`](https://github.com/Timmoth/RackPeek/tree/main/docs/docker-gen) folder from the repository to a machine.

Create a `.env` file next to the provided `compose.yaml` compose stack. All of your docker-gen config will live in this file.

---

# 2. Configure Docker Daemon Connectivity

docker-gen needs to be able to connect to the Docker Daemon on the target machine in order to generate Service YAML.

## Same Machine

If you created the docker-gen project on the *same machine* Docker is running on then uncomment the volume `- "/var/run/docker.sock:/tmp/docker.sock"` in the `compose.yaml` file.

## Remote Machine

 If you want to generate Services from a *remote machine* Docker is running on your will need a docker **socket-proxy** container running on the target machine, such as [tecnativa/docker-socket-proxy](https://github.com/Tecnativa/docker-socket-proxy) or [wollomatic/socket-proxy](https://github.com/wollomatic/socket-proxy).

 The socket-proxy container needs **read-only** access to the **containers** api.

 <details>
  
 <summary>Example</summary>

 On the remote machine run this compose project:

 ```yaml
 services:
  docker-socket-proxy:
    image: tecnativa/docker-socket-proxy
    ports: "2375:2375"
    volumes:
      - "/var/run/docker.sock:/tmp/docker.sock"
    environment:
      - POST=0
      - CONTAINERS=1
 ```

 </details>

In the docker-gen `compose.yaml` file uncomment environmental variable `- "DOCKER_HOST=${DOCKER_HOST}"` and in your `.env` set it to `tcp://HOST:IP` of the target machine IE `tcp://192.168.0.100:2375`

---

# 3. Configure Container Filtering

This step is **optional.**

Use [container filters](https://docs.docker.com/engine/reference/commandline/ps/#filter) to restrict which containers docker-gen generates Services from.

Filter should be added to `DOCKER_CONTAINER_FILTERS` in your `.env`. Multiple filters can be used by specifying them as a comma-separated list.

<details>

<summary>Examples</summary>

Only run docker-gen for containers that show up in [homepage](https://gethomepage.dev/):

* filter on label `homepage.name`

```
DOCKER_CONTAINER_FILTERS=label=homepage.name
```

Only run docker-gen for containers that are discovered by Traefik and are currently running:

* filter on label `traefik.http.routers`
* filter on status `running`

```
DOCKER_CONTAINER_FILTERS=label=traefik.http.routers,status=running
```

</details>

---

# 4. Add RackPeek Environmental Variables

This step is **optional.**

More RackPeek Service config can be enabled by specifying optional environmental variables in `.env`:

* `RUNS_ON` enables and specifies the name of the [**System**](/docs/resource-levels#systems) the generated Services run on (`runsOn` in YAML)
* `HOST_IP` enables IP and port in the `network` section of a Service. The IP will always be `HOST_IP` (useful for ports published on the bridge network)
  * IP and port will only be generated if the container binds to `0.0.0.0` (no IP specified) or specifically binds the ip from `HOST_IP`
    * docker-gen will first look for ports `80` and `443`. If these ports aren't published then it uses the first published port meeting above conditions

---

# 5. Generate Services

With no further configuration `docker-gen` will output generated Services to stdout. To get clean output from docker compose run:

```bash
docker compose up --no-log-prefix
```

Services can also be written to a file by specifying the last line in `command` in your `compose.yaml`. Uncommment `"/etc/docker-gen/templates/services.yaml"` to write to `service.yaml` in the same folder as the `docker-gen` project you created.

---

# Additional Generation Behavior

The `name` for a Service will be parsed from a container in this order:

* label `rackpeek.name`
* label `homepage.name`
* label `org.opencontainers.image.title`
* container name

The `url` for a Service will be parsed from a container in this order:

* label `rackpeek.href`
* label `homepage.href`
* label `caddy`