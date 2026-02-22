# Developer Setup

---

This guide is targetted at developers willing to get involved in the development of RackPeek.

Please review all commands in this guide carefully before execution and ensure you understand the implications of each step.

This guide is by no means exhaustive, so please feel free to contribute back to it if you have any additional information or tips.

## Brew Installation

This project leverages the [brew](https://brew.sh) package manager for installation of dependencies on MacOS/Linux

Please follow the installation instructions for Brew as found [here](https://brew.sh/index#installation)

## Just Installation

This project makes use of the [Just](https://github.com/casey/just) tool for streamlining development and developer productivity.

If using Homebrew, installation is as simple as:

```shell
brew install just
```

Please follow your preferred installation method for Just as found [here](https://github.com/casey/just?tab=readme-ov-file#installation) if not using [brew](https://brew.sh).

## VHS Installation

This project makes use of the [VHS](https://github.com/charmbracelet/vhs) tool for recording and creating GIFs of the CLI for documentation purposes.

If using Homebrew, installation is as simple as:

```shell
brew install vhs
```

Please follow your preferred installation method for VHS as found [here](https://github.com/charmbracelet/vhs?tab=readme-ov-file#installation) if not using [brew](https://brew.sh).

## DotNet Installation

For those looking for a quick start for getting setup with `dotnet` for development on RackPeek, please follow the instructions below.

### Ubuntu Linux

Setup the development environment on Ubuntu Linux.

#### Install Prerequisites

```shell
sudo apt update
sudo apt install -y wget apt-transport-https software-properties-common
```

#### Download and Register Microsoft Package Repository

Find linux distribution version:

```shell
export RACKPEEK_KERNEL_VERSION=$(dpkg -l | grep linux-image | grep ii | head -1 | awk '{print $3}' | sed 's/.*~\([0-9]*\.[0-9]*\)\..*/\1/')
```

Ensure the correct kernel version was found (example: `22.04`, `24.04`, etc.):

```shell
echo "KERNEL_VERSION: ${RACKPEEK_KERNEL_VERSION}"
```

Download and register the Microsoft package repository:

```shell
wget https://packages.microsoft.com/config/ubuntu/${RACKPEEK_KERNEL_VERSION}/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
```

Update package lists:

```shell
sudo apt update
```

#### Install .NET 10 SDK

```shell
sudo apt install -y dotnet-sdk-10.0
```

#### Verify Installation

```shell
dotnet --version
```

### MacOS

🏗️ Help wanted for guide on MacOS development environment setup.

### Windows

🏗️ Help wanted for guide on Windows development environment setup.