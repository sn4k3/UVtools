#!/bin/bash
#
# Script to build the libcvextern.so
# Can be run outside UVtools and as standard alone script
# Then run this script
# usage 1: ./libcvextern.sh clean
# usage 2: ./libcvextern.sh -i
# usage 3: ./libcvextern.sh 
#
cd "$(dirname "$0")"
directory="emgucv"
osVariant=""

for lastArg in $@; do :; done # Get last argument

if [[ $lastArg == "clean" ]]; then
    echo "Cleaning $directory directory"
    rm -rf "$directory" 2>/dev/null
    exit
fi

installDependencies=false
while getopts 'i' flag; do
  case "${flag}" in
    i) installDependencies=true ;;
    *) echo "Usage:"
        echo "clean Cleans the emgucv folder and it's contents"
        echo "-i    Install all the dependencies to build libcvextern"
        exit 1 ;;
  esac
done

echo "Script to build libcvextern.so|dylib"

echo "- Detecting OS"
[ "$installDependencies" == true ] && echo "- Installing all the dependencies"
if [[ $OSTYPE == 'darwin'* ]]; then
    osVariant="macOS"
    if [ installDependencies == true ]; then
        /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install.sh)"
        brew analytics off
        brew install git cmake mono mono-libgdiplus
        brew install --cask dotnet
    fi
elif command -v apt-get &> /dev/null
then
    osVariant="debian"
    if [ "$installDependencies" == true ]; then
        sudo apt-get update
        sudo apt-get -y install build-essential libgtk-3-dev libgstreamer1.0-dev libavcodec-dev libswscale-dev libavformat-dev libdc1394-22-dev libv4l-dev cmake-curses-gui ocl-icd-dev freeglut3-dev libgeotiff-dev libusb-1.0-0-dev
        sudo apt-get install -y apt-transport-https
        sudo apt-get update
        sudo apt-get install -y dotnet-sdk-6.0
    fi
elif command -v pacman &> /dev/null
then
    osVariant="arch"
    if [ "$installDependencies" == true ]; then
        sudo pacman -Syu
        sudo pacman -S base-devel git cmake msbuild gtk3 gstreamer ffmpeg libdc1394 v4l-utils ocl-icd freeglut libgeotiff libusb dotnet-sdk
    fi
elif command -v yum &> /dev/null
then
    osVariant="rhel"
    if [ "$installDependencies" == true ]; then
        sudo yum update -y
        sudo yum install -y https://download1.rpmfusion.org/free/fedora/rpmfusion-free-release-$(rpm -E %fedora).noarch.rpm
        sudo yum install -y https://download1.rpmfusion.org/nonfree/fedora/rpmfusion-nonfree-release-$(rpm -E %fedora).noarch.rpm
        sudo yum install -y libjpeg-devel libjpeg-turbo-devel libpng-devel libgeotiff-devel libdc1394-devel ffmpeg-devel tbb-devel mesa-libGL wget dotnet-sdk-6.0
    fi
fi

echo "- Checks"
if ! command -v git &> /dev/null
then
    echo "Error: git not installed. Please re-run this script with -r flag."
    exit -1
fi

if [ -z "$osVariant" ]; then
    echo "Error: Unable to detect your Operative System."
    exit -1
fi

if [ ! -d "$directory" ]; then
    git clone https://github.com/emgucv/emgucv "$directory"
    cd "$directory"
    git submodule update --init --recursive
else
    cd "$directory"
fi

echo "- Bulding"
if [ osVariant == "macOS" ]; then
    cd platforms/macos
    ./configure
else
    cd platforms/ubuntu/20.04
    ./cmake_configure
fi

echo "Completed - Check for errors but also for libcvextern presence on $directory/libs"
