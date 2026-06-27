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

set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$script_dir"

base_directory="emgucv"
directory="$base_directory"
rawArch="$(uname -m)"
arch="$rawArch"
osVariant=""
build_package="mini"   # mini, core, full
ubuntu_build_version="22.04"
branch=""
clean=false
removeDirectory=false
installDependencies=false

testcmd() { command -v "$1" >/dev/null 2>&1; }

usage() {
    echo "Usage:"
    echo "  ./libcvextern.sh clean"
    echo "  ./libcvextern.sh remove"
    echo "  ./libcvextern.sh [-i] [tag]"
    echo
    echo "Options:"
    echo "  clean   Removes EmguCV build folders"
    echo "  remove  Removes EmguCV source folders"
    echo "  tag     Tag or branch name to clone"
    echo "  -i      Install all the dependencies to build libcvextern"
}

die() {
    echo "Error: $1" >&2
    exit 9
}

remove_path() {
    local path="$1"
    local description="$2"

    if [ -e "$path" ]; then
        echo "Removing $description $path"
        rm -rf -- "$path"
    else
        echo "Warning: $description $path does not exist" >&2
    fi
}

for_each_emgucv_directory() {
    local source_dir

    for source_dir in "$base_directory" "$base_directory"-*; do
        [ -d "$source_dir" ] || continue
        printf '%s\n' "$source_dir"
    done
}

clean_build_directories() {
    local found=false
    local source_dir
    local build_dir

    while IFS= read -r source_dir; do
        found=true
        for build_dir in \
            "$source_dir/platforms/ubuntu/$ubuntu_build_version/build" \
            "$source_dir/platforms/macos/build"; do
            remove_path "$build_dir" "build directory"
        done
    done < <(for_each_emgucv_directory)

    if [ "$found" == false ]; then
        echo "Warning: no $base_directory source folders found" >&2
    fi
}

remove_source_directories() {
    local found=false
    local source_dir

    while IFS= read -r source_dir; do
        found=true
        remove_path "$source_dir" "source directory"
    done < <(for_each_emgucv_directory)

    if [ "$found" == false ]; then
        echo "Warning: no $base_directory source folders found" >&2
    fi
}

parse_arguments() {
    local arg

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
            remove)
                removeDirectory=true
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
}

validate_action() {
    if [ "$clean" == true ] && [ "$removeDirectory" == true ]; then
        usage
        die "clean and remove cannot be combined."
    fi

    if [ "$clean" == true ]; then
        if [ -n "$branch" ] || [ "$installDependencies" == true ]; then
            usage
            die "clean cannot be combined with other options."
        fi

        clean_build_directories
        exit 0
    fi

    if [ "$removeDirectory" == true ]; then
        if [ -n "$branch" ] || [ "$installDependencies" == true ]; then
            usage
            die "remove cannot be combined with other options."
        fi

        remove_source_directories
        exit 0
    fi
}

resolve_architecture() {
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
}

prompt_for_branch() {
    local confirmation

    if [ -n "$branch" ]; then
        return
    fi

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
}

detect_missing_dependencies() {
    if ! testcmd git || ! testcmd cmake || ! testcmd dotnet; then
        installDependencies=true
        return
    fi

    if testcmd ldconfig && ! ldconfig -p | grep -q "libpng"; then
        installDependencies=true
    fi
}

install_macos_dependencies() {
    if ! testcmd brew; then
        bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
        if [ -f "/opt/homebrew/bin/brew" ] && ! testcmd brew; then
            echo '# Set PATH, MANPATH, etc., for Homebrew.' >> "$HOME/.zprofile"
            echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> "$HOME/.zprofile"
            eval "$(/opt/homebrew/bin/brew shellenv)"
        fi
    fi

    testcmd git || brew install git
    testcmd cmake || brew install cmake
    testcmd mono || brew install mono
    testcmd dotnet || brew install --cask dotnet-sdk
}

install_debian_dependencies() {
    sudo apt update
    # FULL: sudo apt -y install git build-essential libgtk-3-dev libgstreamer1.0-dev libavcodec-dev libswscale-dev libavformat-dev libdc1394-dev libv4l-dev cmake-curses-gui ocl-icd-dev freeglut3-dev libgeotiff-dev libusb-1.0-0-dev
    sudo apt install -y git build-essential cmake-curses-gui apt-transport-https libtbb-dev pkg-config nasm
    sudo apt update
    sudo apt install -y dotnet-sdk-10.0

    if ! testcmd dotnet; then
        wget https://packages.microsoft.com/config/ubuntu/$ubuntu_build_version/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        sudo dpkg -i packages-microsoft-prod.deb
        rm -f packages-microsoft-prod.deb
        sudo apt update
        sudo apt install -y dotnet-sdk-10.0
    fi
}

install_arch_dependencies() {
    sudo pacman -Syu
    # FULL: sudo pacman -S git base-devel cmake msbuild gtk3 gstreamer ffmpeg libdc1394 v4l-utils ocl-icd freeglut libgeotiff libusb dotnet-sdk
    sudo pacman -S git base-devel cmake msbuild dotnet-sdk nasm
}

install_rhel_dependencies() {
    sudo dnf update -y
    sudo dnf install -y "https://download1.rpmfusion.org/free/fedora/rpmfusion-free-release-$(rpm -E %fedora).noarch.rpm"
    sudo dnf install -y "https://download1.rpmfusion.org/nonfree/fedora/rpmfusion-nonfree-release-$(rpm -E %fedora).noarch.rpm"
    sudo dnf groupinstall -y "Development Tools" "Development Libraries"
    # FULL: sudo dnf install -y git cmake gcc-c++ gtk3-devel gstreamer1-devel ffmpeg ffmpeg-devel libdc1394 libv4l-devel cmake-gui ocl-icd-devel freeglut libgeotiff libusb dotnet-sdk-6.0
    sudo dnf install -y git cmake gcc-c++ dotnet-sdk-10.0 libtbb-devel nasm
}

detect_os_and_install_dependencies() {
    echo "- Detecting OS"

    if [ "${OSTYPE:0:6}" == "darwin" ]; then
        osVariant="macOS"
        [ "$installDependencies" == true ] && install_macos_dependencies
    elif testcmd apt; then
        osVariant="debian"
        [ "$installDependencies" == true ] && install_debian_dependencies
    elif testcmd pacman; then
        osVariant="arch"
        [ "$installDependencies" == true ] && install_arch_dependencies
    elif testcmd dnf; then
        osVariant="rhel"
        [ "$installDependencies" == true ] && install_rhel_dependencies
    fi
}

check_requirements() {
    echo "- Checks"

    [ -n "$osVariant" ] || die "Unable to detect your operating system."
    testcmd git || die "git not installed. Please re-run this script with -i flag."
    testcmd cmake || die "cmake not installed. Please re-run this script with -i flag."
    testcmd dotnet || die "dotnet not installed. Please re-run this script with -i flag."

    if [ "$osVariant" == "macOS" ] && ! testcmd mono; then
        die "mono not installed. Please re-run this script with -i flag."
    fi
}

clone_emgucv() {
    [ -n "$branch" ] && directory="$base_directory-$branch"

    if [ -d "$directory" ]; then
        return
    fi

    if [ -z "$branch" ]; then
        echo "Cloning master"
        git clone --recurse-submodules --depth 1 https://github.com/emgucv/emgucv "$directory"
    else
        echo "Cloning $branch"
        git clone --recurse-submodules --depth 1 --branch "$branch" https://github.com/emgucv/emgucv "$directory"
    fi
}

patch_macos_configure() {
    local configure_script="$directory/platforms/macos/configure"
    local searchFor='${FREETYPE_OPTIONS[@]}'
    local searchRegex='\${FREETYPE_OPTIONS\[@\]}'

    [ -f "$configure_script" ] || die "Unable to find EmguCV macOS configure script: $configure_script"
    [ "$build_package" == "mini" ] || return

    if grep -Fq -- "-DBUILD_opencv_videoio:BOOL=FALSE" "$configure_script"; then
        return
    fi

    grep -Fq -- "$searchFor" "$configure_script" || die "Unable to patch $configure_script. Could not find: $searchFor"

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
}

patch_linux_configure() {
    local configure_script="$directory/platforms/ubuntu/$ubuntu_build_version/cmake_configure"
    local searchFor='-DWITH_EIGEN:BOOL=TRUE'

    [ -f "$configure_script" ] || die "Unable to find EmguCV Ubuntu $ubuntu_build_version configure script: $configure_script"

    sed -i "s/-DBUILD_TIFF:BOOL=TRUE/-DBUILD_TIFF:BOOL=FALSE/g" "$configure_script"
    [ "$build_package" == "mini" ] || return

    if grep -Fq -- "-DBUILD_opencv_videoio:BOOL=FALSE" "$configure_script"; then
        return
    fi

    grep -Fq -- "$searchFor" "$configure_script" || die "Unable to patch $configure_script. Could not find: $searchFor"

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
}

build_emgucv() {
    echo "- Building"
    # https://docs.opencv.org/4.x/db/d05/tutorial_config_reference.html

    if [ "$osVariant" == "macOS" ]; then
        patch_macos_configure
        "$directory/platforms/macos/configure" "$arch" "$build_package"
    else
        patch_linux_configure
        "$directory/platforms/ubuntu/$ubuntu_build_version/cmake_configure" "$build_package"
    fi
}

parse_arguments "$@"
validate_action
resolve_architecture
prompt_for_branch

echo "Script to build libcvextern.so|dylib on $(uname -a) $arch"

detect_missing_dependencies
[ "$installDependencies" == true ] && echo "- Installing all the dependencies"
detect_os_and_install_dependencies
check_requirements
clone_emgucv
build_emgucv

echo "Completed - Check for errors but also for libcvextern presence on $directory/libs"
