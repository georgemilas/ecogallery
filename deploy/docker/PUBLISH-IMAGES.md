# Publishing Docker Images to Registry

This guide shows how to build and publish Docker images so users can deploy without source code.

## Option 1: Docker Hub (Free, Public)

### 1. Create Docker Hub Account

Sign up at https://hub.docker.com

### 2. Login to Docker Hub

```bash
docker login
```

Enter your Docker Hub username and password.

### 3. Build and Push Images

From the repository root:

```bash
# Set your Docker Hub username
export DOCKER_USER=yourusername

# Build all images
docker build -f deploy/docker/Dockerfile.api -t $DOCKER_USER/ecogallery-api:latest .
docker build -f deploy/docker/Dockerfile.frontend -t $DOCKER_USER/ecogallery-frontend:latest .
docker build -f deploy/docker/Dockerfile.service -t $DOCKER_USER/ecogallery-service:latest .

# Build nginx with config
docker build -f deploy/docker/Dockerfile.nginx -t $DOCKER_USER/ecogallery-nginx:latest .

# Push to Docker Hub
docker push $DOCKER_USER/ecogallery-api:latest
docker push $DOCKER_USER/ecogallery-frontend:latest
docker push $DOCKER_USER/ecogallery-service:latest
docker push $DOCKER_USER/ecogallery-nginx:latest
```

### 4. Tag with Version Numbers

```bash
# Tag with specific version
docker tag $DOCKER_USER/ecogallery-api:latest $DOCKER_USER/ecogallery-api:v1.0.0
docker tag $DOCKER_USER/ecogallery-frontend:latest $DOCKER_USER/ecogallery-frontend:v1.0.0
docker tag $DOCKER_USER/ecogallery-service:latest $DOCKER_USER/ecogallery-service:v1.0.0
docker tag $DOCKER_USER/ecogallery-nginx:latest $DOCKER_USER/ecogallery-nginx:v1.0.0

# Push versioned tags
docker push $DOCKER_USER/ecogallery-api:v1.0.0
docker push $DOCKER_USER/ecogallery-frontend:v1.0.0
docker push $DOCKER_USER/ecogallery-service:v1.0.0
docker push $DOCKER_USER/ecogallery-nginx:v1.0.0
```

## Option 2: GitHub Container Registry (Free, Public/Private)

### 1. Create Personal Access Token

1. Go to GitHub Settings → Developer settings → Personal access tokens
2. Create token with `write:packages` permission

### 2. Login to GitHub Container Registry

```bash
echo YOUR_TOKEN | docker login ghcr.io -u YOUR_USERNAME --password-stdin
```

### 3. Build and Push Images

```bash
# Set your GitHub username
export GITHUB_USER=yourusername

# Build all images
docker build -f deploy/docker/Dockerfile.api -t ghcr.io/$GITHUB_USER/ecogallery-api:latest .
docker build -f deploy/docker/Dockerfile.frontend -t ghcr.io/$GITHUB_USER/ecogallery-frontend:latest .
docker build -f deploy/docker/Dockerfile.service -t ghcr.io/$GITHUB_USER/ecogallery-service:latest .
docker build -f deploy/docker/Dockerfile.nginx -t ghcr.io/$GITHUB_USER/ecogallery-nginx:latest .

# Push to GitHub Container Registry
docker push ghcr.io/$GITHUB_USER/ecogallery-api:latest
docker push ghcr.io/$GITHUB_USER/ecogallery-frontend:latest
docker push ghcr.io/$GITHUB_USER/ecogallery-service:latest
docker push ghcr.io/$GITHUB_USER/ecogallery-nginx:latest
```

## Create Nginx Image

Create `Dockerfile.nginx` in `deploy/docker/`:

```dockerfile
FROM nginx:alpine

# Copy nginx configuration
COPY nginx.config /etc/nginx/nginx.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
```

Build and push:
```bash
docker build -f deploy/docker/Dockerfile.nginx -t $DOCKER_USER/ecogallery-nginx:latest .
docker push $DOCKER_USER/ecogallery-nginx:latest
```

## Automate with GitHub Actions

Create `.github/workflows/docker-publish.yml`:

```yaml
name: Publish Docker Images

on:
  release:
    types: [published]
  workflow_dispatch:

env:
  REGISTRY: ghcr.io
  IMAGE_PREFIX: ghcr.io/${{ github.repository_owner }}

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    strategy:
      matrix:
        include:
          - name: api
            dockerfile: deploy/docker/Dockerfile.api
          - name: frontend
            dockerfile: deploy/docker/Dockerfile.frontend
          - name: service
            dockerfile: deploy/docker/Dockerfile.service
          - name: nginx
            dockerfile: deploy/docker/Dockerfile.nginx

    steps:
      - uses: actions/checkout@v3

      - name: Log in to Container Registry
        uses: docker/login-action@v2
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v4
        with:
          images: ${{ env.IMAGE_PREFIX }}/ecogallery-${{ matrix.name }}
          tags: |
            type=ref,event=branch
            type=semver,pattern={{version}}
            type=semver,pattern={{major}}.{{minor}}
            type=raw,value=latest

      - name: Build and push
        uses: docker/build-push-action@v4
        with:
          context: .
          file: ${{ matrix.dockerfile }}
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
```

## Distribution Package

Create a release package with just these files:
- `docker-compose.prod.yml`
- `.env.example`
- `DOCKER-SETUP-PREBUILT.md`

Users download this small package, configure `.env`, and run!

## Update Instructions for Users

Tell users to update:

```bash
# Pull latest images
docker-compose -f docker-compose.prod.yml pull

# Restart with new images
docker-compose -f docker-compose.prod.yml up -d
```

## Best Practices

1. **Version tags:** Always tag releases with version numbers (v1.0.0, v1.1.0, etc.)
2. **Latest tag:** Keep `latest` tag for current stable release
3. **Testing:** Test images before pushing to registry
4. **Security:** Scan images for vulnerabilities before publishing
5. **Documentation:** Update DOCKER-SETUP-PREBUILT.md with correct image names
