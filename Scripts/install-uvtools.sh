#!/bin/bash
#
# Script to download and install/upgrade UVtools in current location
# Can be run outside UVtools and as standalone script
# Then run this script
# usage 1: ./install-uvtools.sh
#

#cd "$(dirname "$0")"
arch="$(uname -m)" # x86_64 or arm64
osVariant=""       # osx, linux, arch, rhel
api_url="https://api.github.com/repos/sn4k3/UVtools/releases/latest"
dependencies_url="https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/install-dependencies.sh"
macOS_least_version=10.15

version() { echo "$@" | awk -F. '{ printf("%d%03d%03d%03d\n", $1,$2,$3,$4); }'; }
testcmd() { command -v "$1" &> /dev/null; }

if [ "$arch" != "x86_64" -a "$arch" != "arm64" ]; then
    echo "Error: Unsupported host arch $arch"
    exit -1
fi

echo "Script to download and install UVtools"

echo "- Detecting OS"

if [ "${OSTYPE:0:6}" == "darwin" ]; then
    osVariant="osx"
    macOS_version="$(sw_vers -productVersion)"
    appPath="/Applications/UVtools.app"

    echo "- Detected: $osVariant $arch"

    if [ $(version $macOS_version) -lt $(version $macOS_least_version) ]; then
        echo "Error: Unable to install, UVtools requires at least macOS $macOS_least_version."
        exit -1
    fi

    if ! testcmd brew; then
        bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
        if [ -f "/opt/homebrew/bin/brew" -a -z "$(command -v brew)" ]; then
            echo '# Set PATH, MANPATH, etc., for Homebrew.' >> "$HOME/.zprofile"
            echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> "$HOME/.zprofile"
            eval "$(/opt/homebrew/bin/brew shellenv)"
        fi
    fi

    brew install --cask --no-quarantine uvtools

    # Required dotnet-sdk to run arm64 and bypass codesign
    [ "$arch" == "arm64" -a -z "$(command -v dotnet)" ] && brew install --cask dotnet-sdk

    if [ -d "$appPath" ]; then
        # Remove quarantine security from files
        find "$appPath" -print0 | xargs -0 xattr -d com.apple.quarantine &> /dev/null

        # arm64: Create script on user desktop to run UVtools
        if [ "$arch" == "arm64" ]; then
            run_script="$HOME/Desktop/run-uvtools"

            echo '#!/bin/bash
# Run this script to run UVtools on macOS arm64 machines
cd "$(dirname "$0")"

lookupPaths=(
    "/Applications/UVtools.app/Contents/MacOS/UVtools.sh"
    "UVtools.app/Contents/MacOS/UVtools.sh"
    "$HOME/Desktop/UVtools.app/Contents/MacOS/UVtools.sh"
    "$HOME/Downloads/UVtools.app/Contents/MacOS/UVtools.sh"
    "$HOME/UVtools.app/Contents/MacOS/UVtools.sh"
)

for path in "${lookupPaths[@]}"
do
	if [ -f "$path" ]; then
        echo "Found: $path"
        echo "UVtools will now run."

        nohup bash "$path" &> /dev/null &
        disown
        
        echo "You can close this terminal window."
        exit
    fi
done

echo "Error: UVtools.app not found on known paths"
' > "$run_script"

            chmod a+x "$run_script"
            echo "Note: Always run \"bash run-uvtools\" from your Desktop to launch UVtools on this Mac (arm64)!"
        fi

        if [ -f "$appPath/Contents/MacOS/UVtools.sh" ]; then
            nohup bash "$appPath/Contents/MacOS/UVtools.sh" &> /dev/null &
            disown
        elif [ -d "$appPath" ]; then
            open "$appPath"
        fi
    fi

    exit 1
elif testcmd apt-get; then
    osVariant="linux"
    [ -z "$(command -v curl)" ] && sudo apt-get install -y curl
elif testcmd pacman; then
    osVariant="arch"
    [ -z "$(command -v curl)" ] && sudo pacman -S curl
elif testcmd dnf; then
    osVariant="rhel"
    [ -z "$(command -v curl)" ] && sudo dnf install -y curl
fi

if [ -z "$osVariant" ]; then
    echo "Error: Unable to detect your Operative System."
    exit -1
fi

echo "- Detected: $osVariant $arch"

if [ -z "$(ldconfig -p | grep libpng)" -o -z "$(ldconfig -p | grep libgdiplus)" -o -z "$(ldconfig -p | grep libgeotiff)" -o -z "$(ldconfig -p | grep libavcodec)" ]; then
    echo "- Missing dependencies found, installing..."
    sudo bash -c "$(curl -fsSL $dependencies_url)"
fi

echo "- Detecting download"
response="$(curl -s "$api_url")"

download_url="$(echo "$response" \
| grep "browser_download_url.*_${osVariant}-x64_.*\.AppImage" \
| head -1 \
| cut -d : -f 2,3 \
| tr -d \")"

if [ -z "$download_url" ]; then
    download_url="$(echo "$response" \
    | grep "browser_download_url.*_linux-x64_.*\.AppImage" \
    | head -1 \
    | cut -d : -f 2,3 \
    | tr -d \")"
fi

if [ -z "$download_url" ]; then
    echo "Error: Unable to detect the download url."
    exit -1
fi

filename="$(basename "${download_url}")"
tmpfile="/tmp/$filename"

echo "Downloading: $download_url"
#wget $download_url -O "$tmpfile" -q --show-progress
curl -L --retry 4 $download_url -o "$tmpfile"

echo "- Setting permissions"
chmod -fv a+x "$tmpfile"

echo "- Kill instances"
killall -q UVtools

if [ -d "$HOME/Applications" ]; then
    echo "- Removing old versions"
    rm -f "$HOME/Applications/UVtools_*.AppImage"
    
    echo "- Moving $filename to $HOME/Applications"
    mv -f "$tmpfile" "$HOME/Applications"
    
    "$HOME/Applications/$filename" &
    echo "If prompt for \"Desktop integration\", click \"Integrate and run\""
else 
    echo "- Removing old versions"
    rm -f UVtools_*.AppImage

    echo "- Moving $filename to $(pwd)"
    mv -f "$tmpfile" .

    ./$filename &
fi

echo "UVtools will now run."
