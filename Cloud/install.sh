#!/bin/bash

# Determine the operating system
OS=$(uname)

# Set the installation path based on the operating system
if [ "$OS" == "Linux" ]; then
  INSTALL_PATH="/usr/share/cloud"
  DOTNET_PATH="/usr/share/dotnet"
elif [ "$OS" == "Darwin" ]; then
  INSTALL_PATH="/usr/local/share/cloud"
  DOTNET_PATH="/usr/local/share/dotnet"

else
  echo "Unsupported operating system"
  exit 1
fi

# Create the installation directory and copy files
if [ "$PWD" != "$INSTALL_PATH" ]; then
  sudo mkdir -p "$INSTALL_PATH"
  sudo chmod 777 "$INSTALL_PATH"
  cp -R . "$INSTALL_PATH"
  sudo chmod 777 "$INSTALL_PATH/install.sh"
  cd "$INSTALL_PATH"
  ./install.sh
  exit
fi

sudo chmod 755 "$INSTALL_PATH"


if (( $EUID == 0 )); then
    echo "WARNING: Don't run as root!"
    exit
fi

# DOTNET runtime
# NOTE: You can download the runtime from here: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
#       and unzip the *.tga.gz file in home/$YourUser/.dotnet (or in a different position)

# Download the installer:
if which wget >/dev/null ; then
    sudo wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
elif which curl >/dev/null ; then
    sudo curl -L -O https://dot.net/v1/dotnet-install.sh
else
    echo "Cannot download, neither wget nor curl is available!"
    exit
fi

sudo mkdir -p $DOTNET_PATH
sudo chmod 755 $DOTNET_PATH

sudo chmod +x ./dotnet-install.sh
# info https://learn.microsoft.com/it-it/dotnet/core/tools/dotnet-install-script
sudo ./dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir $DOTNET_PATH

#start the application
sudo $DOTNET_PATH/dotnet Cloud.dll