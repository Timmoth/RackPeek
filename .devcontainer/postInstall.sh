#!/usr/bin/env bash

# originally based on https://github.com/meaningful-ooo/devcontainer-features/tree/main/src/homebrew

source .devcontainer/brew.sh
echo 'export PATH=/home/vscode/.linuxbrew/bin:/home/linuxbrew/.linuxbrew/sbin:$PATH' >> /home/vscode/.bashrc
/home/vscode/.linuxbrew/bin/brew install just