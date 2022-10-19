#!/bin/bash
#
# Script to download and install/upgrade UVtools in current location
# Can be run outside UVtools and as standalone script
# Then run this script
# usage 1: ./install-uvtools.sh
#
cd "$(dirname "$0")"
arch_name="$(uname -m)" # x86_64 or arm64
osVariant=""            # osx, linux, arch, rhel
api_url="https://api.github.com/repos/sn4k3/UVtools/releases/latest"
dependencies_url="https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/install-dependencies.sh"
macOS_least_version=10.15

function version { echo "$@" | awk -F. '{ printf("%d%03d%03d%03d\n", $1,$2,$3,$4); }'; }

if [ "$arch_name" != "x86_64" -a "$arch_name" != "arm64" ]; then
    echo "Error: Unsupported host arch $arch_name"
    exit -1
fi

echo "Script to download and install UVtools"

echo "- Detecting OS"

if [ "${OSTYPE:0:6}" == "darwin" ]; then
    osVariant="osx"
    macOS_version="$(sw_vers -productVersion)"

    if [ $(version $macOS_version) -lt $(version $macOS_least_version) ]; then
        echo "Error: Unable to install, UVtools requires at least macOS $macOS_least_version."
        exit -1
    fi
    
    [ -z "$(command -v brew)" ] && /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    brew install --cask uvtools
    exit 1
elif command -v apt-get &> /dev/null
then
	osVariant="linux"
    [ -z "$(command -v wget)" ] && sudo apt-get install -y wget
	[ -z "$(command -v curl)" ] && sudo apt-get install -y curl
elif command -v pacman &> /dev/null
then
	osVariant="arch"
	[ -z "$(command -v wget)" ] && sudo pacman -S wget
	[ -z "$(command -v curl)" ] && sudo pacman -S curl
elif command -v yum &> /dev/null
then
	osVariant="rhel"
	[ -z "$(command -v wget)" ] && sudo yum install -y wget
	[ -z "$(command -v curl)" ] && sudo yum install -y curl
fi

if [ -z "$osVariant" ]; then
    echo "Error: Unable to detect your Operative System."
    exit -1
fi

echo "- Detected: $osVariant $arch_name"

if [ ! "$(ldconfig -p | grep libpng)" -o ! "$(ldconfig -p | grep libgdiplus)" -o ! "$(ldconfig -p | grep libavcodec)" ]; then
	echo "- Missing dependencies found, installing..."
	wget -qO - $dependencies_url | sudo bash
fi

echo "- Detecting download url"
download_url="$(curl -s "$api_url" \
| grep "browser_download_url.*_${osVariant}-x64_.*\.AppImage" \
| head -1 \
| cut -d : -f 2,3 \
| tr -d \")"

if [ -z "$download_url" ]; then
    echo "Error: Unable to detect the download url."
    exit -1
fi

filename="$(basename "${download_url}")"

echo "- Kill instances"
killall -q UVtools

echo "- Removing old versions"
rm -f UVtools_*.AppImage

echo "Downloading: $download_url"
wget $download_url -O $filename -q --show-progress

echo "- Setting permissions"
chmod -fv a+x "$filename"

if [ -d ~/Applications ]; then
    echo "- Moving '$filename' to ~/Applications"
    rm -f ~/Applications/UVtools_*.AppImage
	mv -f "$filename" ~/Applications
	~/Applications/$filename &
	echo "If prompt for 'Desktop integration', click 'Integrate and run'"
else 
    ./$filename &
fi

echo "UVtools will now run."

