#!/bin/bash
#
# Script to build the libcvextern.so
# Can be run outside UVtools and as standalone script
# Then run this script
# usage 1: ./libcvextern.sh clean
# usage 2: ./libcvextern.sh -i
# usage 3: ./libcvextern.sh 
# usage 3: ./libcvextern.sh 4.x.0
#
#cd "$(dirname "$0")"
echo $PWD
directory="emgucv"
arch="$(uname -m)"
osVariant=""
build_package="mini"   # mini, core, full

testcmd() { command -v "$1" &> /dev/null; }

if [ "$arch" != "x86_64" -a "$arch" != "arm64" ]; then
    echo "Error: Unsupported host arch: $arch"
    exit -1
fi

for lastArg in $@; do :; done # Get last argument

if [ $lastArg == "clean" ]; then
    echo "Cleaning $directory directory"
    rm -rf "$directory" 2>/dev/null
    rm -rf "$directory-"* 2>/dev/null
    exit
fi


if [ -z "$lastArg" ]; then
    read -p "You are about to clone the master branch which are not recommended, you should choose a stable tag instead.
Are you sure you want to continue with the master branch?

y/yes/blank: Continue with master
4.x.0: Continue with specified tag
n/no:  Cancel

Anwser: " confirmation 
    
    if [ -z "$confirmation" -o "$confirmation" == "y" -o "$confirmation" == "yes" ]; then
        echo "Continuing with master..."
    elif [ "$confirmation" == "n" -o "$confirmation" == "no" -o "$confirmation" == "cancel" ]; then
        exit 1
    elif [[ "$confirmation" =~ ^[0-9]+[.][0-9]+[.][0-9]+$ ]]; then
        echo "Changing from master to $confirmation branch"
        lastArg="$confirmation"
    else
        echo "Unable to recognize your input, continuing with master branch..."
        sleep 2
    fi
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

echo "Script to build libcvextern.so|dylib on $(uname -a) $arch"

if testcmd ldconfig; then
    #if [ -z "$(ldconfig -p | grep libpng)" -o -z "$(ldconfig -p | grep libgdiplus)" -o -z "$(ldconfig -p | grep libavcodec)" -o -z "$(command -v git)" -o -z "$(command -v cmake)" -o -z "$(command -v dotnet)" ]; then
    if [ -z "$(ldconfig -p | grep libpng)" -o -z "$(command -v git)" -o -z "$(command -v cmake)" -o -z "$(command -v dotnet)" ]; then
        installDependencies=true
    fi
fi

echo "- Detecting OS"
[ "$installDependencies" == true ] && echo "- Installing all the dependencies"
if [ "${OSTYPE:0:6}" == "darwin" ]; then
    osVariant="macOS"
    if ! testcmd brew; then
        bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
        if [ -f "/opt/homebrew/bin/brew" -a -z "$(command -v brew)" ]; then
            echo '# Set PATH, MANPATH, etc., for Homebrew.' >> "$HOME/.zprofile"
            echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> "$HOME/.zprofile"
            eval "$(/opt/homebrew/bin/brew shellenv)"
        fi
    fi
    [ -z "$(command -v git)" ] && brew install git
    [ -z "$(command -v cmake)" ] && brew install cmake
    [ -z "$(command -v mono)" ] && brew install mono
    [ -z "$(command -v dotnet)" ] && brew install --cask dotnet-sdk
elif testcmd apt; then
    osVariant="debian"
    if [ "$installDependencies" == true ]; then
        sudo apt update
        #FULL: sudo apt -y install git build-essential libgtk-3-dev libgstreamer1.0-dev libavcodec-dev libswscale-dev libavformat-dev libdc1394-dev libv4l-dev cmake-curses-gui ocl-icd-dev freeglut3-dev libgeotiff-dev libusb-1.0-0-dev
        sudo apt -y install git build-essential cmake-curses-gui
        sudo apt install -y apt-transport-https
        sudo apt install -y libtbb-dev
        sudo apt update
        sudo apt install -y dotnet-sdk-9.0
        if ! testcmd dotnet; then
            wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            sudo apt update
            sudo apt install -y dotnet-sdk-9.0
        fi
    fi
elif testcmd pacman; then
    osVariant="arch"
    if [ "$installDependencies" == true ]; then
        sudo pacman -Syu
        #FULL: sudo pacman -S git base-devel cmake msbuild gtk3 gstreamer ffmpeg libdc1394 v4l-utils ocl-icd freeglut libgeotiff libusb dotnet-sdk
        sudo pacman -S git base-devel cmake msbuild dotnet-sdk
    fi
elif testcmd dnf; then
    osVariant="rhel"
    if [ "$installDependencies" == true ]; then
        sudo dnf update -y
        sudo dnf install -y https://download1.rpmfusion.org/free/fedora/rpmfusion-free-release-$(rpm -E %fedora).noarch.rpm
        sudo dnf install -y https://download1.rpmfusion.org/nonfree/fedora/rpmfusion-nonfree-release-$(rpm -E %fedora).noarch.rpm
        sudo dnf groupinstall -y "Development Tools" "Development Libraries"
        #FULL: sudo dnf install -y git cmake gcc-c++ gtk3-devel gstreamer1-devel ffmpeg ffmpeg-devel libdc1394 libv4l-devel cmake-gui ocl-icd-devel freeglut libgeotiff libusb dotnet-sdk-6.0
        sudo dnf install -y git cmake gcc-c++ dotnet-sdk-9.0 libtbb-devel
    fi
fi

echo "- Checks"
if [ -z "$osVariant" ]; then
    echo "Error: Unable to detect your Operative System."
    exit -1
fi

if ! testcmd git; then
    echo "Error: git not installed. Please re-run this script with -i flag."
    exit -1
fi

if ! testcmd cmake; then
    echo "Error: cmake not installed. Please re-run this script with -i flag."
    exit -1
fi

if ! testcmd dotnet; then
    echo "Error: dotnet not installed. Please re-run this script with -i flag."
    exit -1
fi


[ -n "$lastArg" ] && directory="$directory-$lastArg"

if [ ! -d "$directory" ]; then
    if [ -z "$lastArg" ]; then
        echo "Cloning master"
        git clone --recurse-submodules --depth 1 https://github.com/emgucv/emgucv "$directory"
    else
        echo "Cloning $lastArg"
        git clone --recurse-submodules --depth 1 --branch "$lastArg" https://github.com/emgucv/emgucv "$directory"
    fi
fi

echo "- Bulding"
# https://docs.opencv.org/4.x/db/d05/tutorial_config_reference.html

if [ "$osVariant" == "macOS" ]; then
	#sed -i '' "s/-DBUILD_TIFF:BOOL=TRUE/-DBUILD_TIFF:BOOL=FALSE/g" "$directory/platforms/macos/configure" 2>/dev/null
    if [ "$build_package" == "mini" ]; then
        searchFor='${FREETYPE_OPTIONS[@]}'
        sed -i '' "s/$searchFor/$searchFor \\\\\\
       -DWITH_EIGEN:BOOL=FALSE \\\\\\
       -DWITH_AVFOUNDATION:BOOL=FALSE \\\\\\
       -DWITH_FFMPEG:BOOL=FALSE \\\\\\
       -DWITH_GSTREAMER:BOOL=FALSE \\\\\\
       -DWITH_1394:BOOL=FALSE \\\\\\
       -DVIDEOIO_ENABLE_PLUGINS:BOOL=FALSE \\\\\\
       -DBUILD_opencv_videoio:BOOL=FALSE \\\\\\
       -DBUILD_opencv_gapi:BOOL=FALSE \\\\\\
       -DWITH_PROTOBUF:BOOL=FALSE \\\\\\
       -DBUILD_PROTOBUF:BOOL=FALSE/g" "$directory/platforms/macos/configure" 2>/dev/null
    fi
    "$directory/platforms/macos/configure" $arch $build_package
else # Linux
	sed -i "s/-DBUILD_TIFF:BOOL=TRUE/-DBUILD_TIFF:BOOL=FALSE/g" "$directory/platforms/ubuntu/24.04/cmake_configure" 2>/dev/null
    if [ "$build_package" == "mini" ]; then
        searchFor='-DWITH_EIGEN:BOOL=TRUE'
        sed -i "s/$searchFor/-DWITH_EIGEN:BOOL=FALSE \\\\\\
       -DWITH_V4L:BOOL=FALSE \\\\\\
       -DWITH_FFMPEG:BOOL=FALSE \\\\\\
       -DWITH_GSTREAMER:BOOL=FALSE \\\\\\
       -DWITH_1394:BOOL=FALSE \\\\\\
       -DVIDEOIO_ENABLE_PLUGINS:BOOL=FALSE \\\\\\
       -DBUILD_opencv_videoio:BOOL=FALSE \\\\\\
       -DBUILD_opencv_gapi:BOOL=FALSE \\\\\\
       -DWITH_PROTOBUF:BOOL=FALSE \\\\\\
       -DBUILD_PROTOBUF:BOOL=FALSE /g" "$directory/platforms/ubuntu/24.04/cmake_configure" 2>/dev/null
    fi
    "$directory/platforms/ubuntu/24.04/cmake_configure" $build_package
fi

echo "Completed - Check for errors but also for libcvextern presence on $directory/libs"
