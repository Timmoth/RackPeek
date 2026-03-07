# Git Integration Guide

RackPeek can automatically save and sync its configuration using Git.
To enable this you need a GitHub Personal Access Token with permission to push to the repository that will store your config.

Create a fine-grained access token on GitHub. Select the repository that will contain your RackPeek config and grant **Contents: Read and Write** access. Copy the token when it is created.

Provide the token to the container using the `GIT_TOKEN` environment variable. You should also provide your GitHub username with `GIT_USERNAME`.

Example using Docker Compose:

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
    environment:
      - GIT_TOKEN=your_token_here
      - GIT_USERNAME=your_github_username
    restart: unless-stopped

volumes:
  rackpeek-config:
```

Example using the Docker CLI:

```bash
docker run -d \
  --name rackpeek \
  -p 8080:8080 \
  -v rackpeek-config:/app/config \
  -e GIT_TOKEN=your_token_here \
  -e GIT_USERNAME=your_github_username \
  aptacode/rackpeek:latest
```

Open RackPeek in the browser, enable Git when prompted, then add the repository remote URL. RackPeek will commit and sync configuration changes automatically.
