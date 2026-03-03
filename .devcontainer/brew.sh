#!/usr/bin/env bash

BREW_PREFIX=${BREW_PREFIX:-"/home/vscode/.linuxbrew"}
SHALLOW_CLONE=${SHALLOWCLONE:-"true"}
USERNAME=${USERNAME:-"automatic"}

ARCHITECTURE="$(uname -m)"
if [ "${ARCHITECTURE}" != "amd64" ] && [ "${ARCHITECTURE}" != "x86_64" ] && [ "${ARCHITECTURE}" != "aarch64" ]; then
  echo "(!) Architecture $ARCHITECTURE unsupported"
  exit 1
fi

cleanup() {
  source /etc/os-release
  case "${ID}" in
    debian|ubuntu)
      rm -rf /var/lib/apt/lists/*
    ;;
  esac
}

if [ "$(id -u)" -ne 0 ]; then
  echo -e 'Script must be run as root. Use sudo, su, or add "USER root" to your Dockerfile before running this script.'
  exit 1
fi

# Ensure that login shells get the correct path if the user updated the PATH using ENV.
rm -f /etc/profile.d/00-restore-env.sh
echo "export PATH=${PATH//$(sh -lc 'echo $PATH')/\$PATH}" > /etc/profile.d/00-restore-env.sh
chmod +x /etc/profile.d/00-restore-env.sh

# Determine the appropriate non-root user
if [ "${USERNAME}" = "auto" ] || [ "${USERNAME}" = "automatic" ]; then
  USERNAME=""
  POSSIBLE_USERS=("vscode" "node" "codespace" "$(awk -v val=1000 -F ":" '$3==val{print $1}' /etc/passwd)")
  for CURRENT_USER in "${POSSIBLE_USERS[@]}"; do
    if id -u ${CURRENT_USER} > /dev/null 2>&1; then
      USERNAME=${CURRENT_USER}
      break
    fi
  done
  if [ "${USERNAME}" = "" ]; then
    USERNAME=root
  fi
elif [ "${USERNAME}" = "none" ] || ! id -u ${USERNAME} > /dev/null 2>&1; then
  USERNAME=root
fi

apt_get_update() {
    if [ "$(find /var/lib/apt/lists/* | wc -l)" = "0" ]; then
        echo "Running apt-get update..."
        apt-get update -y
    fi
}

# Checks if packages are installed and installs them if not
check_packages() {
  source /etc/os-release
  case "${ID}" in
    debian|ubuntu)
      if ! dpkg -s "$@" >/dev/null 2>&1; then
        apt_get_update
        apt-get -y install --no-install-recommends "$@"
      fi
    ;;
    alpine)
      if ! apk -e info "$@" >/dev/null 2>&1; then
        apk add --no-cache "$@"
      fi
    ;;
  esac
}

updaterc() {
  if [ "${UPDATE_RC}" = "true" ]; then
    echo "Updating /etc/bash.bashrc and /etc/zsh/zshrc..."
    if [[ "$(cat /etc/bash.bashrc)" != *"$1"* ]]; then
      echo -e "$1" >> /etc/bash.bashrc
    fi
    if [ -f "/etc/zsh/zshrc" ] && [[ "$(cat /etc/zsh/zshrc)" != *"$1"* ]]; then
      echo -e "$1" >> /etc/zsh/zshrc
    fi
  fi
}

updatefishconfig() {
  if [ "${UPDATE_RC}" = "true" ]; then
    echo "Updating /etc/fish/config.fish..."
    if [ -f "/etc/fish/config.fish" ]; then
        echo -e "$1" >> /etc/fish/config.fish
      fi
  fi
}

export DEBIAN_FRONTEND=noninteractive

# Clean up
cleanup

# Install dependencies if missing
check_packages \
  bzip2 \
  ca-certificates \
  curl \
  file \
  fonts-dejavu-core \
  g++ \
  git \
  less \
  libz-dev \
  locales \
  make \
  netbase \
  openssh-client \
  patch \
  sudo \
  tzdata \
  uuid-runtime

# Install Homebrew
mkdir -p "${BREW_PREFIX}"
echo "Installing Homebrew..."
if [ "${SHALLOW_CLONE}" = "false" ]; then
  git clone https://github.com/Homebrew/brew "${BREW_PREFIX}/Homebrew"
  mkdir -p "${BREW_PREFIX}/Homebrew/Library/Taps/homebrew"
  git clone https://github.com/Homebrew/homebrew-core "${BREW_PREFIX}/Homebrew/Library/Taps/homebrew/homebrew-core"
else
  echo "Using shallow clone..."
  git clone --depth 1 https://github.com/Homebrew/brew "${BREW_PREFIX}/Homebrew"
  mkdir -p "${BREW_PREFIX}/Homebrew/Library/Taps/homebrew"
  git clone --depth 1 https://github.com/Homebrew/homebrew-core "${BREW_PREFIX}/Homebrew/Library/Taps/homebrew/homebrew-core"
  # Disable automatic updates as they are not allowed with shallow clone installation
  updaterc "export HOMEBREW_NO_AUTO_UPDATE=1"
  updatefishconfig "set -gx HOMEBREW_NO_AUTO_UPDATE 1"
fi
"${BREW_PREFIX}/Homebrew/bin/brew" config
mkdir "${BREW_PREFIX}/bin"
ln -s "${BREW_PREFIX}/Homebrew/bin/brew" "${BREW_PREFIX}/bin"
chown -R ${USERNAME} "${BREW_PREFIX}"

echo "Done!"

