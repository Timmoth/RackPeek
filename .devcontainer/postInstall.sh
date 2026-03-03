#!/usr/bin/env bash

# originally based on https://github.com/meaningful-ooo/devcontainer-features/tree/main/src/homebrew

HOMEBREW_PREFIX=/home/linuxbrew/.linuxbrew
HOMEBREW_CELLAR=/home/linuxbrew/.linuxbrew/Cellar
HOMEBREW_REPOSITORY=/home/linuxbrew/.linuxbrew/Homebrew
PATH=/home/linuxbrew/.linuxbrew/bin:/home/linuxbrew/.linuxbrew/sbin:${PATH}
MANPATH=/home/linuxbrew/.linuxbrew/share/man:${MANPATH}
INFOPATH=/home/linuxbrew/.linuxbrew/share/info:${INFOPATH}
SHALLOWCLONE=true
source .devcontainer/brew.sh
source /etc/bash.bashrc
brew install just