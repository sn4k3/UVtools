#!/bin/bash
#
# Script to install the dependencies in order to run the UVtools
# Can be run outside UVtools and as standalone script
# Then run this script
# usage 1: sudo ./install-dependencies.sh 
#

echo "Script to install the dependencies in order to run the UVtools"

if [ $EUID -ne 0 ]; then
   echo "This script must be run as root" 
   exit 1
fi

testcmd() { command -v "$1" &> /dev/null; }

arch="$(uname -m)"
osVariant=""

if [ "$arch" != "x86_64" -a "$arch" != "arm64" ]; then
    echo "Error: Unsupported host arch $arch"
    exit -1
fi

#echo "- Detecting OS"
if [ "${OSTYPE:0:6}" == "darwin" ]; then
    osVariant="macOS"
    #/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install.sh)"
    #brew analytics off
    #brew install git cmake mono-libgdiplus
    #brew install --cask dotnet
elif testcmd apt-get; then
    osVariant="debian"
    apt update
    apt install -y libdc1394-22 libopenexr24
    apt install -y libdc1394-25 libopenexr25
    apt install -y libjpeg-dev libpng-dev libgeotiff-dev libgeotiff5 libavcodec-dev libavformat-dev libswscale-dev libtbb-dev libgl1-mesa-dev libgdiplus
    # mini only requires: libgdiplus libgeotiff-dev libgeotiff5 
elif testcmd pacman; then
    osVariant="arch"
    pacman -Syu
    pacman -S openjpeg2 libjpeg-turbo libpng libgeotiff libdc1394 ffmpeg openexr tbb libgdiplus
elif testcmd dnf; then
    osVariant="rhel"
    dnf update -y
    dnf install -y https://download1.rpmfusion.org/free/fedora/rpmfusion-free-release-$(rpm -E %fedora).noarch.rpm
    dnf install -y https://download1.rpmfusion.org/nonfree/fedora/rpmfusion-nonfree-release-$(rpm -E %fedora).noarch.rpm
    dnf install -y libjpeg-devel libjpeg-turbo-devel libpng-devel libgeotiff-devel libdc1394-devel ffmpeg-devel tbb-devel mesa-libGL libgdiplus
else
    echo "Error: Base operative system / package manager not identified, nothing was installed"
    exit -1
fi

echo "Completed: You can now run UVtools"
