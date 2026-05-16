ARG VARIANT=1-10.0
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

# Ensure .NET 10 RTM runtime is installed alongside any RC that may ship in the base image
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir /usr/share/dotnet --no-path

# La imagen base trae preconfigurado el repo apt de Yarn, cuya clave GPG
# (62D54FD4003F6525) ha rotado. Eso rompe `apt-get update` durante la
# instalación de las devcontainer features (docker-in-docker, node, etc.).
# Eliminar la lista evita el fallo sin afectar a npm/pnpm/corepack.
RUN rm -f /etc/apt/sources.list.d/yarn.list \
          /etc/apt/keyrings/yarn*.gpg \
          /etc/apt/trusted.gpg.d/yarn*.gpg
