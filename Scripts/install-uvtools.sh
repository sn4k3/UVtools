#!/bin/bash
#
# Script to download and install/upgrade UVtools in current location
# Can be run outside UVtools and as standalone script
# Then run this script
# usage 1: ./install-uvtools.sh
#
cd "$(dirname "$0")"
arch_name="$(uname -m)"
osVariant=""
api_url="https://api.github.com/repos/sn4k3/UVtools/releases/latest"
dependencies_url="https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/install-dependencies.sh"

if [[ "$arch_name" != "x86_64" && "$arch_name" != "arm64" ]]; then
    echo "Error: Unsupported host arch: $arch_name"
    exit -1
fi


echo "Script to download and install UVtools"

echo "- Detecting OS"

if [[ $OSTYPE == 'darwin'* ]]; then
    osVariant="osx"
    if [ installDependencies == true ]; then
        [[ ! "$(command -v brew)" ]] && /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
        brew install --cask uvtools
		exit 1
    fi
elif command -v apt-get &> /dev/null
then
	osVariant="linux"
    [[ ! "$(command -v wget)" ]] && sudo apt-get install -y wget
	[[ ! "$(command -v curl)" ]] && sudo apt-get install -y curl
elif command -v pacman &> /dev/null
then
	osVariant="arch"
	[[ ! "$(command -v wget)" ]] && sudo pacman -S wget
	[[ ! "$(command -v curl)" ]] && sudo pacman -S curl
elif command -v yum &> /dev/null
then
	osVariant="rhel"
	[[ ! "$(command -v wget)" ]] && sudo yum install -y wget
	[[ ! "$(command -v curl)" ]] && sudo yum install -y curl
fi

if [ -z "$osVariant" ]; then
    echo "Error: Unable to detect your Operative System."
    exit -1
fi

echo "- Detected: $osVariant $arch_name"

if [[ ! "$(ldconfig -p | grep libpng)" || ! "$(ldconfig -p | grep libgdiplus)" || ! "$(ldconfig -p | grep libavcodec)" ]]; then
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

