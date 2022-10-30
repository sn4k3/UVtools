#!/bin/bash
#
# Script to download and install/upgrade UVtools in current location
# Can be run outside UVtools and as standalone script
# Then run this script
# usage 1: ./install-uvtools.sh
#

#cd "$(dirname "$0")"
arch="$(uname -m)" # x86_64 or arm64
archCode="${arch/86_/}"
osVariant=""       # osx, linux, arch, rhel
api_url="https://api.github.com/repos/sn4k3/UVtools/releases/latest"
dependencies_url="https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/install-dependencies.sh"
macOS_least_version=10.15

version() { echo "$@" | awk -F. '{ printf("%d%03d%03d%03d\n", $1,$2,$3,$4); }'; }
testcmd() { command -v "$1" &> /dev/null; }
downloaduvtools(){
    if [ -z "$1" ]; then
        echo "Download url was not specified"
        exit -1
    fi

    filename="$(basename "${download_url}")"
    tmpfile="$(mktemp "${TMPDIR:-/tmp}"/UVtoolsUpdate.XXXXXXXX)"

    echo "Downloading: $download_url"
    curl -L --retry 4 $download_url -o "$tmpfile"

    echo "- Kill instances"
    killall UVtools 2> /dev/null
    ps -ef | grep '.*dotnet.*UVtools.dll' | grep -v grep | awk '{print $2}' | xargs -r kill
    sleep 0.5
}

if [ "$arch" != "x86_64" -a "$arch" != "arm64" ]; then
    echo "Error: Unsupported host arch $arch"
    exit -1
fi

echo "Script to download and install UVtools"

echo "- Detecting OS"

if [ "${OSTYPE:0:6}" == "darwin" ]; then
    #############
    #   macOS   #
    #############
    osVariant="osx"
    macOS_version="$(sw_vers -productVersion)"
    appPath="/Applications/UVtools.app"

    echo "- Detected: $osVariant $arch"

    if [ $(version $macOS_version) -lt $(version $macOS_least_version) ]; then
        echo "Error: Unable to install, UVtools requires at least macOS $macOS_least_version."
        exit -1
    fi

    # Required dotnet-sdk to run arm64 and bypass codesign
    if [ "$arch" == "arm64" -a -z "$(command -v dotnet)" ]; then
        if ! testcmd brew; then
            bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
            if [ -f "/opt/homebrew/bin/brew" -a -z "$(command -v brew)" ]; then
                echo '# Set PATH, MANPATH, etc., for Homebrew.' >> "$HOME/.zprofile"
                echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> "$HOME/.zprofile"
                eval "$(/opt/homebrew/bin/brew shellenv)"
            fi
        fi

        brew install --cask dotnet-sdk
    fi

    echo "- Detecting download"

    download_url="$(curl -s "$api_url" \
    | grep "browser_download_url.*_${osVariant}-${archCode}_.*\.zip" \
    | head -1 \
    | cut -d : -f 2,3 \
    | tr -d \")"

    if [ -z "$download_url" ]; then
        echo "Error: Unable to detect the download url."
        exit -1
    fi

    downloaduvtools "$download_url"

    echo "- Removing old versions"
    rm -rf "$appPath"

    echo "- Inflating $tmpfile to $appPath"
    unzip -q -o "$tmpfile" -d "/Applications"
    rm -f "$tmpfile"

    if [ -d "$appPath" ]; then
        # Remove quarantine security from files
        find "$appPath" -print0 | xargs -0 xattr -d com.apple.quarantine &> /dev/null

        echo ''
        echo 'Installation was successful. UVtools will now run.'

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

            chmod 775 "$run_script"
            echo 'Note: Always run "bash run-uvtools" from your Desktop to launch UVtools on this Mac (arm64)!'
        fi

        echo ''

        if [ -f "$appPath/Contents/MacOS/UVtools.sh" ]; then
            nohup bash "$appPath/Contents/MacOS/UVtools.sh" &> /dev/null &
            disown
        elif [ -d "$appPath" ]; then
            open "$appPath"
        fi
    else
        echo "Installation unsuccessful, unable to create '$appPath'."
        exit -1
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

#############
#   Linux   #
#############
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

downloaduvtools "$download_url"

targetDir="$PWD"
[ -d "$HOME/Applications" ] && targetDir="$HOME/Applications"
targetFilePath="$targetDir/$filename"

echo "- Removing old versions"
rm -f "$targetDir/UVtools_"*".AppImage"

echo "- Moving $filename to $targetDir"
mv -f "$tmpfile" "$targetFilePath"
rm -f "$tmpfile"

echo "- Setting permissions"
chmod -fv 775 "$targetFilePath"

"$targetFilePath" &

echo ''
echo 'Installation was successful. UVtools will now run.'
echo 'If prompt for "Desktop integration", click "Integrate and run"'
echo ''
