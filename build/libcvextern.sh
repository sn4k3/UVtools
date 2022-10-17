#!/bin/bash
#
# Script to build the libcvextern.so
# Can be run outside UVtools and as standalone script
# Then run this script
# usage 1: ./libcvextern.sh clean
# usage 2: ./libcvextern.sh -i
# usage 3: ./libcvextern.sh 
# usage 3: ./libcvextern.sh 4.6.0
#
cd "$(dirname "$0")"
directory="emgucv"
arch_name="$(uname -m)"
osVariant=""

if [[ "$arch_name" != "x86_64" && "$arch_name" != "arm64" ]]; then
    echo "Error: Unsupported host arch: $arch_name"
    exit -1
fi

for lastArg in $@; do :; done # Get last argument

if [ $lastArg == "clean" ]; then
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
        echo "x.x.x Tag name to clone"
        echo "-i    Install all the dependencies to build libcvextern"
        exit 1 ;;
  esac
done

echo "Script to build libcvextern.so|dylib on $(uname -a) $arch_name"

echo "- Detecting OS"
[ "$installDependencies" == true ] && echo "- Installing all the dependencies"
if [[ $OSTYPE == 'darwin'* ]]; then
    osVariant="macOS"
    if [ installDependencies == true ]; then
        [[ ! "$(command -v brew)" ]] && /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
        brew install git cmake
        brew install --cask dotnet
    fi
elif command -v apt-get &> /dev/null
then
    osVariant="debian"
    if [ "$installDependencies" == true ]; then
        sudo apt-get update
        sudo apt-get -y install git build-essential libgtk-3-dev libgstreamer1.0-dev libavcodec-dev libswscale-dev libavformat-dev libdc1394-dev libv4l-dev cmake-curses-gui ocl-icd-dev freeglut3-dev libgeotiff-dev libusb-1.0-0-dev
        sudo apt-get install -y apt-transport-https
        sudo apt-get update
        sudo apt-get install -y dotnet-sdk-6.0
    fi
elif command -v pacman &> /dev/null
then
    osVariant="arch"
    if [ "$installDependencies" == true ]; then
        sudo pacman -Syu
        sudo pacman -S git base-devel cmake msbuild gtk3 gstreamer ffmpeg libdc1394 v4l-utils ocl-icd freeglut libgeotiff libusb dotnet-sdk
    fi
elif command -v yum &> /dev/null
then
    osVariant="rhel"
    if [ "$installDependencies" == true ]; then
        sudo yum update -y
        sudo yum groupinstall -y "Development Tools" "Development Libraries"
        sudo yum install -y git cmake gcc-c++ gtk3-devel gstreamer1-devel ffmpeg ffmpeg-devel libdc1394 libv4l-devel cmake-gui ocl-icd-devel freeglut libgeotiff libusb dotnet-sdk-6.0
    fi
fi

echo "- Checks"
if ! command -v git &> /dev/null
then
    echo "Error: git not installed. Please re-run this script with -i flag."
    exit -1
fi

if [ -z "$osVariant" ]; then
    echo "Error: Unable to detect your Operative System."
    exit -1
fi

if [ ! -d "$directory" ]; then
    if [ -z "$lastArg" ]; then
        echo "Cloning master"
        git clone --recurse-submodules --depth 1 https://github.com/emgucv/emgucv "$directory"
    else
        echo "Cloning $lastArg"
        git clone --recurse-submodules --depth 1 --branch "$lastArg" https://github.com/emgucv/emgucv "$directory"
    fi
fi

cd "$directory"

echo "- Bulding"
if [ osVariant == "macOS" ]; then
    cd platforms/macos
    if [ "${arch_name}" = "x86_64" ]; then
        ./configure x86_64 mini
    elif [ "${arch_name}" = "arm64" ]; then
        ./configure arm64 mini
    fi
else
    cd platforms/ubuntu/22.04
    ./cmake_configure mini
fi

echo "Completed - Check for errors but also for libcvextern presence on $directory/libs"
