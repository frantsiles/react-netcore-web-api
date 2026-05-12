FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0

# The base image ships with a Yarn APT repo whose GPG key is expired
# (NO_PUBKEY 62D54FD4003F6525). This causes apt-get update to fail when the
# docker-in-docker feature runs its install script. Node.js is provided by the
# devcontainers/features/node feature (nvm), so the Yarn APT repo is not needed.
RUN rm -f /etc/apt/sources.list.d/yarn.list
