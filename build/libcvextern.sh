#!/usr/bin/env bash
#
# Script to build the libcvextern.so
# Can be run outside UVtools and as standalone script
# Then run this script
# usage 1: ./libcvextern.sh clean
# usage 2: ./libcvextern.sh -i
# usage 3: ./libcvextern.sh
# usage 4: ./libcvextern.sh 5.0.0
#

set -Eeuo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
cd "$script_dir"

base_directory="emgucv"
directory="$base_directory"
rawArch="$(uname -m)"
arch="$rawArch"
osVariant=""
build_package="mini"   # mini, core, full
ubuntu_build_version="22.04"
distribution_id=""
distribution_version=""
distribution_codename=""
linux_configure_script=""
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
        if [ -L "$path" ]; then
            rm -- "$path"
            return
        fi
        local resolved_path
        resolved_path="$(cd -- "$path" && pwd -P)"
        case "$resolved_path" in
            "$script_dir"/*) ;;
            *) die "Refusing to remove a directory outside $script_dir: $resolved_path" ;;
        esac
        echo "Removing $description $path"
        rm -rf -- "$resolved_path"
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
            "$source_dir"/platforms/ubuntu/*/build \
            "$source_dir"/platforms/debian/*/build \
            "$source_dir/platforms/macos/build" \
            "$source_dir/platforms/macos/build_x86_64" \
            "$source_dir/platforms/macos/build_arm64"; do
            [ -d "$build_dir" ] || continue
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
5.0.0: Continue with specified tag
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

    # Consume all output so grep cannot cause a SIGPIPE with pipefail enabled.
    if testcmd ldconfig && ! ldconfig -p | grep "libpng" > /dev/null; then
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
    sudo apt install -y git build-essential cmake cmake-curses-gui apt-transport-https libtbb-dev libpng-dev libtiff-dev pkg-config nasm wget ca-certificates

    # A failed package lookup must reach repository setup instead of triggering set -e.
    if ! testcmd dotnet && ! sudo apt install -y dotnet-sdk-10.0; then
        case "$distribution_id" in
            ubuntu)
                [ "$distribution_version" == "22.04" ] || die "dotnet-sdk-10.0 is unavailable in your Ubuntu feeds; enable the appropriate .NET feed for Ubuntu $distribution_version."
                sudo apt install -y software-properties-common
                sudo add-apt-repository -y ppa:dotnet/backports
                ;;
            debian)
                [[ "$distribution_version" =~ ^[0-9]+$ ]] || die "Unable to determine your Debian version."
                register_microsoft_repository
                ;;
            *) die "Install the .NET 10 SDK for your distribution before rerunning this script." ;;
        esac
        sudo apt update
        sudo apt install -y dotnet-sdk-10.0
    fi
}

register_microsoft_repository() (
    # A subshell keeps cleanup local, including when download or installation fails.
    package_dir="$(mktemp -d "${TMPDIR:-/tmp}/uvtools-dotnet-repo.XXXXXX")"
    package_file="$package_dir/packages-microsoft-prod.deb"
    trap 'rm -f -- "$package_file"; rmdir -- "$package_dir"' EXIT
    wget "https://packages.microsoft.com/config/debian/$distribution_version/packages-microsoft-prod.deb" -O "$package_file"
    sudo dpkg -i "$package_file"
)

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
    elif testcmd apt; then
        osVariant="debian"
        if [ -r /etc/os-release ]; then
            . /etc/os-release
            distribution_id="${ID:-}"
            distribution_version="${VERSION_ID:-}"
            distribution_codename="${VERSION_CODENAME:-}"
        fi
    elif testcmd pacman; then
        osVariant="arch"
    elif testcmd dnf; then
        osVariant="rhel"
    fi

    if [ "$installDependencies" == true ]; then
        case "$osVariant" in
            macOS) install_macos_dependencies ;;
            debian) install_debian_dependencies ;;
            arch) install_arch_dependencies ;;
            rhel) install_rhel_dependencies ;;
        esac
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

resolve_linux_configure() {
    local platform="ubuntu/$ubuntu_build_version"
    case "$distribution_id" in
        ubuntu) platform="ubuntu/$distribution_version" ;;
        debian)
            local codename="$distribution_codename"
            if [ -z "$codename" ]; then
                case "$distribution_version" in
                    12) codename="bookworm" ;;
                    13) codename="trixie" ;;
                esac
            fi
            [ -n "$codename" ] || die "Unable to determine your Debian release codename."
            platform="debian/$codename"
            ;;
    esac
    linux_configure_script="$directory/platforms/$platform/cmake_configure"
    [ -f "$linux_configure_script" ] || die "Unable to find Emgu CV configure script: $linux_configure_script"
}

patch_configure() {
    local configure_script="$1"
    local search_for="$2"
    local platform_flag="$3"
    local content replacement flag

    [ -f "$configure_script" ] || die "Unable to find Emgu CV configure script: $configure_script"
    [ "$build_package" == "mini" ] || return 0
    content="$(<"$configure_script")"
    [[ "$content" == *"$search_for"* ]] || die "Unable to patch $configure_script. Could not find: $search_for"

    # Only add the missing flags, so a configure script patched by a previous run still gets the new ones.
    # EMGU_CV_WITH_TIFF is Emgu's GeoTIFF writer, unused by UVtools. It builds PROJ, which links the system libsqlite3.
    replacement="$search_for"
    for flag in \
        '-DWITH_EIGEN:BOOL=FALSE' "$platform_flag" \
        '-DWITH_FFMPEG:BOOL=FALSE' '-DWITH_GSTREAMER:BOOL=FALSE' \
        '-DWITH_1394:BOOL=FALSE' '-DVIDEOIO_ENABLE_PLUGINS:BOOL=FALSE' \
        '-DBUILD_opencv_videoio:BOOL=FALSE' '-DBUILD_opencv_gapi:BOOL=FALSE' \
        '-DWITH_PROTOBUF:BOOL=FALSE' '-DBUILD_PROTOBUF:BOOL=FALSE' \
        '-DWITH_LAPACK:BOOL=FALSE' '-DEMGU_CV_WITH_TIFF:BOOL=FALSE'; do
        [[ "$content" == *"$flag"* ]] || replacement+=$' \\\n       '"$flag"
    done
    [ "$replacement" != "$search_for" ] || return 0
    content="${content/"$search_for"/"$replacement"}"
    printf '%s\n' "$content" > "$configure_script"
}

patch_macos_configure() {
    patch_configure "$directory/platforms/macos/configure" '${FREETYPE_OPTIONS[@]}' '-DWITH_AVFOUNDATION:BOOL=FALSE'
}

patch_linux_configure() {
    local content replacement
    content="$(<"$linux_configure_script")"
    # Link the bundled libtiff statically, the system libtiff soname changes between distributions
    # (libtiff.so.5 on Ubuntu 22.04, libtiff.so.6 on Ubuntu 24.04 / Debian 12+) and breaks the binary on the others.
    content="${content//-DBUILD_TIFF:BOOL=FALSE/-DBUILD_TIFF:BOOL=TRUE}"
    if [[ "$content" != *'-DBUILD_TIFF:BOOL=TRUE'* ]]; then
        [[ "$content" == *'-DWITH_EIGEN:BOOL=TRUE'* ]] || die "Unable to patch $linux_configure_script. Could not find: -DWITH_EIGEN:BOOL=TRUE"
        replacement='-DWITH_EIGEN:BOOL=TRUE'$' \\\n       ''-DBUILD_TIFF:BOOL=TRUE'
        content="${content//-DWITH_EIGEN:BOOL=TRUE/"$replacement"}"
    fi
    printf '%s\n' "$content" > "$linux_configure_script"
    patch_configure "$linux_configure_script" '-DWITH_EIGEN:BOOL=TRUE' '-DWITH_V4L:BOOL=FALSE'
}

check_linux_dependencies() {
    local native_file="$1"
    local library
    local unexpected=""

    if ! testcmd readelf; then
        echo "Warning: readelf not found, unable to check the shared library dependencies of $native_file" >&2
        return
    fi

    echo "Shared library dependencies:"
    while IFS= read -r library; do
        echo "  $library"
        # Only libraries with a stable soname that every distribution installs, anything else must be built in.
        case "$library" in
            libc.so.6|libm.so.6|libdl.so.2|libpthread.so.0|librt.so.1|ld-linux-*.so.*) ;;
            libstdc++.so.6|libgcc_s.so.1|libz.so.1) ;;
            *) unexpected+=" $library" ;;
        esac
    done < <(readelf -d "$native_file" | sed -n 's/.*(NEEDED).*\[\(.*\)\]/\1/p')

    if [ -n "$unexpected" ]; then
        die "$native_file depends on system libraries that are not portable across distributions:$unexpected. Disable or build them in, then run ./libcvextern.sh clean and rebuild."
    fi
}

report_native_binary() {
    local native_arch="x64"
    local native_file
    [ "$arch" != "arm64" ] || native_arch="arm64"
    if [ "$osVariant" == "macOS" ]; then
        for native_file in \
            "$directory/libs/runtimes/osx-$native_arch/native/libcvextern.dylib" \
            "$directory/libs/runtimes/osx/native/$native_arch/libcvextern.dylib"; do
            if [ -f "$native_file" ]; then
                echo "Completed - Native binary: $native_file"
                return
            fi
        done
    else
        for native_file in \
            "$directory/libs/runtimes/linux-$native_arch/native/libcvextern.so" \
            "$directory/libs/runtimes/ubuntu-$native_arch/native/libcvextern.so" \
            "$directory/libs/runtimes"/ubuntu.*-"$native_arch"/native/libcvextern.so \
            "$directory/libs/runtimes/rhel-$native_arch/native/libcvextern.so"; do
            if [ -f "$native_file" ]; then
                check_linux_dependencies "$native_file"
                echo "Completed - Native binary: $native_file"
                return
            fi
        done
    fi
    die "Build finished without the expected $native_arch native binary under $directory/libs/runtimes."
}

build_emgucv() {
    echo "- Building"

    if [ "$osVariant" == "macOS" ]; then
        patch_macos_configure
        bash -e "$directory/platforms/macos/configure" "$arch" "$build_package"
    else
        resolve_linux_configure
        patch_linux_configure
        bash -e "$linux_configure_script" "$build_package" cvextern
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

report_native_binary
