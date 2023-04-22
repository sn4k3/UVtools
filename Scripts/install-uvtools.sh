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
osVariant=''       # osx, linux, arch, rhel
api_url="https://api.github.com/repos/sn4k3/UVtools/releases/latest"
dependencies_url="https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/install-dependencies.sh"
macOS_least_version='10.15'

if [ "$arch" != "x86_64" -a "$arch" != "arm64" ]; then
    echo "Error: Unsupported host arch $arch"
    exit -1
fi

version() { echo "$@" | awk -F. '{ printf("%d%03d%03d%03d\n", $1,$2,$3,$4); }'; }
testcmd() { command -v "$1" &> /dev/null; }
get_filesize() {
    (
      du --apparent-size --block-size=1 "$1" 2>/dev/null ||
      gdu --apparent-size --block-size=1 "$1" 2>/dev/null ||
      find "$1" -printf "%s" 2>/dev/null ||
      gfind "$1" -printf "%s" 2>/dev/null ||
      stat --printf="%s" "$1" 2>/dev/null ||
      stat -f%z "$1" 2>/dev/null ||
      wc -c <"$1" 2>/dev/null
    ) | awk '{print $1}'
}
downloaduvtools(){
    if [ -z "$1" ]; then
        echo 'Error: Download url was not specified!'
        exit -1
    fi

    filename="$(basename "${download_url}")"
    tmpfile="$(mktemp "${TMPDIR:-/tmp}"/UVtoolsUpdate.XXXXXXXX)"

    echo "- Downloading: $download_url"
    curl -L --retry 4 $download_url -o "$tmpfile"

    download_size="$(curl -sLI $download_url | grep -i Content-Length | awk 'END{print $2}' | tr -d '\r')"
    if [ -n "$download_size" ]; then
        echo '- Validating file'
        local filesize="$(get_filesize "$tmpfile")"
        if [ "$download_size" -ne "$filesize" ]; then
            echo "Error: File verification failed, expecting $download_size bytes but downloaded $filesize bytes. Please re-run the script."
            rm -f "$tmpfile"
            exit -1
        fi
    fi

    echo '- Kill instances'
    killall UVtools 2> /dev/null
    ps -ef | grep '.*dotnet.*UVtools.dll' | grep -v grep | awk '{print $2}' | xargs kill 2> /dev/null
    sleep 0.5
}

echo '  _   ___     ___              _     '
echo ' | | | \ \   / / |_ ___   ___ | |___ '
echo ' | | | |\ \ / /| __/ _ \ / _ \| / __|'
echo ' | |_| | \ V / | || (_) | (_) | \__ \'
echo '  \___/   \_/   \__\___/ \___/|_|___/'
echo '  Auto download and installer script '
echo ''
echo '- Detecting OS'

if [ "${OSTYPE:0:6}" == "darwin" ]; then
    osVariant="osx"
elif testcmd apt-get; then
    osVariant="linux"
    ! testcmd curl && sudo apt-get install -y curl
elif testcmd pacman; then
    osVariant="arch"
    ! testcmd curl && sudo pacman -S curl
elif testcmd dnf; then
    osVariant="rhel"
    ! testcmd curl && sudo dnf install -y curl
elif testcmd zypper; then
    osVariant="suse"
    ! testcmd curl && sudo zypper install -y curl
fi

if [ -z "$osVariant" ]; then
    echo "Error: Unable to detect your Operative System."
    exit -1
fi

echo "- $osVariant $arch"

if [ "$osVariant" == "osx" ]; then
    #############
    #   macOS   #
    #############
    macOS_version="$(sw_vers -productVersion)"
    appPath="/Applications/UVtools.app"

    if [ $(version $macOS_version) -lt $(version $macOS_least_version) ]; then
        echo "Error: Unable to install, UVtools requires at least macOS $macOS_least_version."
        exit -1
    fi

    if ! testcmd codesign && ! testcmd brew; then
        echo '- Codesign required, installing...'
        bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
        if [ -f "/opt/homebrew/bin/brew" -a -z "$(command -v brew)" ]; then
            echo '# Set PATH, MANPATH, etc., for Homebrew.' >> "$HOME/.zprofile"
            echo 'eval "$(/opt/homebrew/bin/brew shellenv)"' >> "$HOME/.zprofile"
            eval "$(/opt/homebrew/bin/brew shellenv)"
        fi
    fi

    echo '- Detecting download'

    download_url="$(curl -s "$api_url" \
    | grep "browser_download_url.*_${osVariant}-${archCode}_.*\.zip" \
    | head -1 \
    | cut -d : -f 2,3 \
    | tr -d \")"

    if [ -z "$download_url" ]; then
        echo 'Error: Unable to detect the download url.'
        exit -1
    fi

    downloaduvtools "$download_url"

    echo '- Removing old versions'
    rm -rf "$appPath"

    echo "- Inflating $filename to $appPath"
    unzip -q -o "$tmpfile" -d "/Applications"
    rm -f "$tmpfile"

    if [ -d "$appPath" ]; then
        echo '- Removing com.apple.quarantine security flag (gatekeeper)'
        find "$appPath" -print0 | xargs -0 xattr -d com.apple.quarantine &> /dev/null

        # Force codesign to allow the app to run directly
        echo '- Codesign app bundle'
        codesign --force --deep --sign - "$appPath"

        echo ''
        echo 'Installation was successful. UVtools will now run.'
        echo ''

        open -n "$appPath"
    else
        echo "Installation unsuccessful, unable to create '$appPath'."
        exit -1
    fi
else
    #############
    #   Linux   #
    #############
    requiredlddversion="2.31"
    lddversion="$(ldd --version | awk '/ldd/{print $NF}')"


    if [ $(version $lddversion) -lt $(version $requiredlddversion) ]; then
        echo ""
        echo "##########################################################"
        echo "Error: Unable to auto install the latest version."    
        echo "ldd version: $lddversion detected, but requires at least version $requiredlddversion."
        echo "Solutions:"
        echo "- Upgrade your system to the most recent version"
        echo "- Try to upgrade glibc to at least $requiredlddversion (Search about this as it can break your system)"
        echo "##########################################################"
        exit -1
    fi


    # Not required for mini
    #LDCONFIG=''
    #if testcmd ldconfig; then
    #    LDCONFIG='ldconfig'
    #else
    #    LDCONFIG="$(whereis ldconfig | awk '{ print $2 }')"
    #fi

    #if [ -n "$LDCONFIG" ]; then
    #    if [ -z "$($LDCONFIG -p | grep libgeotiff)" -o -z "$($LDCONFIG -p | grep libgdiplus)" ]; then
    #        echo "- Missing dependencies found, installing..."
    #        sudo bash -c "$(curl -fsSL $dependencies_url)"
    #    fi
    #else
    #    echo "Unable to detect for missing dependencies, ldconfig not found, however installation will continue."
    #fi

    echo '- Detecting download'
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
        echo 'Error: Unable to detect the download url.'
        exit -1
    fi

    downloaduvtools "$download_url"

    targetDir="$PWD"
    [ -d "$HOME/Applications" ] && targetDir="$HOME/Applications"
    targetFilePath="$targetDir/UVtools.AppImage"

    echo '- Removing old versions'
    rm -f "$targetDir/UVtools_"*".AppImage"

    echo "- Moving $filename to $targetDir"
    mv -f "$tmpfile" "$targetFilePath"
    rm -f "$tmpfile"

    echo '- Setting permissions'
    chmod -fv 775 "$targetFilePath"

    "$targetFilePath" &

    echo ''
    echo 'Installation was successful. UVtools will now run.'
    echo 'If prompt for "Desktop integration", click "Integrate and run"'
    echo ''
fi
