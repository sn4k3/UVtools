#!/usr/bin/env bash
#
# Script to build the libcvextern.so
# Can be run outside UVtools and as standalone script
# Then run this script
# usage 1: ./libcvextern.sh clean
# usage 2: ./libcvextern.sh -i
# usage 3: ./libcvextern.sh
# usage 4: ./libcvextern.sh 4.x.0
#
#cd "$(dirname "$0")"
echo "$PWD"
directory="emgucv"
rawArch="$(uname -m)"
arch="$rawArch"
osVariant=""
build_package="mini"   # mini, core, full
ubuntu_build_version="22.04"
branch=""
clean=false
installDependencies=false

testcmd() { command -v "$1" &> /dev/null; }

usage() {
    echo "Usage:"
    echo "  ./libcvextern.sh clean"
    echo "  ./libcvextern.sh [-i] [tag]"
    echo
    echo "Options:"
    echo "  clean  Cleans the emgucv folders"
    echo "  tag    Tag or branch name to clone"
    echo "  -i     Install all the dependencies to build libcvextern"
}

die() {
    echo "Error: $1" >&2
    exit 9
}

for arg in "$@"; do
    case "$arg" in
        -i)
            installDependencies=true
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        clean)
            clean=true
            ;;
        -*)
            usage
            die "Unknown option: $arg"
            ;;
        *)
            if [ -n "$branch" ]; then
                usage
                die "Only one tag or branch can be specified."
            fi
            branch="$arg"
            ;;
    esac
done

if [ "$clean" == true ]; then
    if [ -n "$branch" ] || [ "$installDependencies" == true ]; then
        usage
        die "clean cannot be combined with other options."
    fi
    echo "Cleaning $directory directory"
    rm -rf "$directory" 2>/dev/null
    rm -rf "$directory-"* 2>/dev/null
    exit
fi

case "$arch" in
    x86_64|amd64)
        arch="x86_64"
        ;;
    arm64|aarch64)
        arch="arm64"
        ;;
    *)
        die "Unsupported host arch: $rawArch"
        ;;
esac

if [ -z "$branch" ]; then
    read -r -p "You are about to clone the master branch which is not recommended, you should choose a stable tag instead.
Are you sure you want to continue with the master branch?

y/yes/blank: Continue with master
4.x.0: Continue with specified tag
n/no:  Cancel

Answer: " confirmation

    if [ -z "$confirmation" ] || [ "$confirmation" == "y" ] || [ "$confirmation" == "yes" ] || [ "$confirmation" == "master" ] || [ "$confirmation" == "main" ]; then
        echo "Continuing with master..."
    elif [ "$confirmation" == "n" ] || [ "$confirmation" == "no" ] || [ "$confirmation" == "cancel" ]; then
        exit 1
    elif [[ "$confirmation" =~ ^[0-9]+[.][0-9]+[.][0-9]+$ ]]; then
        echo "Changing from master to $confirmation"
        branch="$confirmation"
    else
        echo "Unable to recognize your input, continuing with master branch..."
        sleep 2
    fi
fi

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
    if [ "$installDependencies" == true ]; then
        if ! testcmd brew; then
            bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
            if [ -f "/opt/homebrew/bin/brew" ] && ! testcmd brew; then
                echo '# Set PATH, MANPATH, etc., for Homebrew.' >> "$HOME/.zprofile"
                echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> "$HOME/.zprofile"
                eval "$(/opt/homebrew/bin/brew shellenv)"
            fi
        fi
        [ -z "$(command -v git)" ] && brew install git
        [ -z "$(command -v cmake)" ] && brew install cmake
        [ -z "$(command -v mono)" ] && brew install mono
        [ -z "$(command -v dotnet)" ] && brew install --cask dotnet-sdk
    fi
elif testcmd apt; then
    osVariant="debian"
    if [ "$installDependencies" == true ]; then
        sudo apt update
        #FULL: sudo apt -y install git build-essential libgtk-3-dev libgstreamer1.0-dev libavcodec-dev libswscale-dev libavformat-dev libdc1394-dev libv4l-dev cmake-curses-gui ocl-icd-dev freeglut3-dev libgeotiff-dev libusb-1.0-0-dev
        sudo apt -y install git build-essential cmake-curses-gui
        sudo apt install -y apt-transport-https
        sudo apt install -y libtbb-dev
        sudo apt update
        sudo apt install -y dotnet-sdk-10.0
        if ! testcmd dotnet; then
            wget https://packages.microsoft.com/config/ubuntu/$ubuntu_build_version/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            sudo apt update
            sudo apt install -y dotnet-sdk-10.0
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
        sudo dnf install -y git cmake gcc-c++ dotnet-sdk-10.0 libtbb-devel
    fi
fi

echo "- Checks"
if [ -z "$osVariant" ]; then
    die "Unable to detect your operating system."
fi

if ! testcmd git; then
    die "git not installed. Please re-run this script with -i flag."
fi

if ! testcmd cmake; then
    die "cmake not installed. Please re-run this script with -i flag."
fi

if ! testcmd dotnet; then
    die "dotnet not installed. Please re-run this script with -i flag."
fi

if [ "$osVariant" == "macOS" ] && ! testcmd mono; then
    die "mono not installed. Please re-run this script with -i flag."
fi


[ -n "$branch" ] && directory="$directory-$branch"

if [ ! -d "$directory" ]; then
    if [ -z "$branch" ]; then
        echo "Cloning master"
        git clone --recurse-submodules --depth 1 https://github.com/emgucv/emgucv "$directory"
    else
        echo "Cloning $branch"
        git clone --recurse-submodules --depth 1 --branch "$branch" https://github.com/emgucv/emgucv "$directory"
    fi
fi

echo "- Building"
# https://docs.opencv.org/4.x/db/d05/tutorial_config_reference.html

if [ "$osVariant" == "macOS" ]; then
	#sed -i '' "s/-DBUILD_TIFF:BOOL=TRUE/-DBUILD_TIFF:BOOL=FALSE/g" "$directory/platforms/macos/configure" 2>/dev/null
    configure_script="$directory/platforms/macos/configure"
    if [ ! -f "$configure_script" ]; then
        die "Unable to find EmguCV macOS configure script: $configure_script"
    fi
    if [ "$build_package" == "mini" ]; then
        searchFor='${FREETYPE_OPTIONS[@]}'
        searchRegex='\${FREETYPE_OPTIONS\[@\]}'
        if ! grep -Fq -- "-DBUILD_opencv_videoio:BOOL=FALSE" "$configure_script"; then
            if ! grep -Fq -- "$searchFor" "$configure_script"; then
                die "Unable to patch $configure_script. Could not find: $searchFor"
            fi
            sed -i '' "s/$searchRegex/$searchFor \\\\\\
       -DWITH_EIGEN:BOOL=FALSE \\\\\\
       -DWITH_AVFOUNDATION:BOOL=FALSE \\\\\\
       -DWITH_FFMPEG:BOOL=FALSE \\\\\\
       -DWITH_GSTREAMER:BOOL=FALSE \\\\\\
       -DWITH_1394:BOOL=FALSE \\\\\\
       -DVIDEOIO_ENABLE_PLUGINS:BOOL=FALSE \\\\\\
       -DBUILD_opencv_videoio:BOOL=FALSE \\\\\\
       -DBUILD_opencv_gapi:BOOL=FALSE \\\\\\
       -DWITH_PROTOBUF:BOOL=FALSE \\\\\\
       -DBUILD_PROTOBUF:BOOL=FALSE /g" "$configure_script"
        fi
    fi
    "$configure_script" "$arch" "$build_package"
else # Linux
	configure_script="$directory/platforms/ubuntu/$ubuntu_build_version/cmake_configure"
    if [ ! -f "$configure_script" ]; then
        die "Unable to find EmguCV Ubuntu $ubuntu_build_version configure script: $configure_script"
    fi
	sed -i "s/-DBUILD_TIFF:BOOL=TRUE/-DBUILD_TIFF:BOOL=FALSE/g" "$configure_script"
    if [ "$build_package" == "mini" ]; then
        searchFor='-DWITH_EIGEN:BOOL=TRUE'
        if ! grep -Fq -- "-DBUILD_opencv_videoio:BOOL=FALSE" "$configure_script"; then
            if ! grep -Fq -- "$searchFor" "$configure_script"; then
                die "Unable to patch $configure_script. Could not find: $searchFor"
            fi
        sed -i "s/$searchFor/-DWITH_EIGEN:BOOL=FALSE \\\\\\
       -DWITH_V4L:BOOL=FALSE \\\\\\
       -DWITH_FFMPEG:BOOL=FALSE \\\\\\
       -DWITH_GSTREAMER:BOOL=FALSE \\\\\\
       -DWITH_1394:BOOL=FALSE \\\\\\
       -DVIDEOIO_ENABLE_PLUGINS:BOOL=FALSE \\\\\\
       -DBUILD_opencv_videoio:BOOL=FALSE \\\\\\
       -DBUILD_opencv_gapi:BOOL=FALSE \\\\\\
       -DWITH_PROTOBUF:BOOL=FALSE \\\\\\
       -DBUILD_PROTOBUF:BOOL=FALSE /g" "$configure_script"
        fi
    fi
    "$configure_script" "$build_package"
fi

echo "Completed - Check for errors but also for libcvextern presence on $directory/libs"
