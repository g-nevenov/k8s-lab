#!/usr/bin/env bash
# Installs pinned kind, Helm 4 and make inside the dev container (never on the host).
set -euo pipefail

KIND_VERSION=v0.33.0
ARCH="$(dpkg --print-architecture)"   # amd64 or arm64

curl -fsSLo /tmp/kind "https://kind.sigs.k8s.io/dl/${KIND_VERSION}/kind-linux-${ARCH}"
sudo install -m 0755 /tmp/kind /usr/local/bin/kind

curl -fsSLo /tmp/get_helm.sh https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-4
bash /tmp/get_helm.sh

sudo apt-get update && sudo apt-get install -y --no-install-recommends make
