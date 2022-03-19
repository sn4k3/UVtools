#!/bin/bash
#
# Script to publish and pack UVtools to a platform
# Clone the repo first from github:
# git clone https://github.com/sn4k3/UVtools
# Then run this script
# usage 1: ./CreateRelease.sh clean
# usage 2: ./CreateRelease.sh osx-x64
# usage 3: ./CreateRelease.sh -z osx-x64
#
cd "$(dirname "$0")"
cd ..
[ ! -d "UVtools.Core" ] && echo "UVtools.Core not found!" && exit -1

#runtime=$1
for runtime in $@; do :; done # Get last argument
rootDir="$(pwd)"
coreDir="$(pwd)/UVtools.Core"
version="$(grep -oP '<Version>\K(\d\.\d\.\d)(?=<\/Version>)' $coreDir/UVtools.Core.csproj)"
platformsDir="$(pwd)/UVtools.Platforms"
runtimePlatformDir="$platformsDir/$runtime"
publishName="UVtools_${runtime}_v$version"
publishDir="$(pwd)/publish"
publishRuntimeDir="$publishDir/$publishName"
buildProject="UVtools.WPF"
buildWith="Release"
projectDir="$(pwd)/$buildProject"
netVersion="6.0"

if [[ $runtime = "clean" ]]; then
    echo "Cleaning publish directory"
    rm -rf "$publishDir" 2>/dev/null
    exit
fi

keepNetPublish=false
zipPackage=false
while getopts 'zk' flag; do
  case "${flag}" in
    k) keepNetPublish=true ;;
    z) zipPackage=true ;;
    *) echo "Usage:"
        echo "clean Cleans the publish folder"
        echo "-k    Keep the .NET publish for the packed runtimes when bundling .App and .AppImage"
        echo "-z    Zip published package"
        exit 1 ;;
  esac
done


# Checks
if ! command -v dotnet &> /dev/null
then
    echo "dotnet not installed, please install .NET SDK $netVersion.x, dotnet-sdk or dotnet-sdk-$netVersion"
    exit
fi

if [ -z "$version" ]; then
    echo "UVtools version not found, please check path and script"
    exit -1
fi

if [ -z "$runtime" ]; then
    echo "No runtime selected, please pick one of the following:"
    ls "$platformsDir"
    exit -1
fi

if [[ ! $runtime = *-* && ! $runtime = *-arm && ! $runtime = *-x64 && ! $runtime = *-x86 ]]; then
    echo "The runtime '$runtime' is not valid, please pick one of the following:"
    ls "$platformsDir"
    exit -1
fi

if [ ! $runtime = "win-x64" -a ! -d "$platformsDir/$runtime" ]; then
    echo "The runtime '$runtime' is not valid, please pick one of the following:"
    ls "$platformsDir"
    exit -1
fi


echo "1. Preparing the environment"
rm -rf "$publishRuntimeDir" 2>/dev/null
[ "$zipPackage" = true ] && rm -f "$publishDir/$publishName.zip" 2>/dev/null


echo "2. Publishing UVtools v$version for: $runtime"
dotnet publish $buildProject -o "$publishRuntimeDir" -c $buildWith -r $runtime -p:PublishReadyToRun=true --self-contained

echo "3. Copying dependencies"
echo $runtime > "$publishRuntimeDir/runtime_package.dat"
find "$runtimePlatformDir" -type f | grep -i lib | xargs -i cp -fv {} "$publishRuntimeDir/"

echo "4. Cleaning up"
rm -rf "$projectDir/bin/$buildWith/net$netVersion/$runtime" 2>/dev/null
rm -rf "$projectDir/obj/$buildWith/net$netVersion/$runtime" 2>/dev/null

echo "5. Setting Permissions"
chmod -fv a+x "$publishRuntimeDir/UVtools"
chmod -fv a+x "$publishRuntimeDir/UVtools.sh"

if [[ $runtime = osx-* ]]; then
    echo "6. macOS: Creating app bundle"
    osxApp="$publishDir/$publishName.app"
    rm -rf "$osxApp" 2>/dev/null
    mkdir -p "$osxApp/Contents/MacOS"
    mkdir -p "$osxApp/Contents/Resources"

    cp -f "$rootDir/UVtools.CAD/UVtools.icns" "$osxApp/Contents/Resources/"
    cp -f "$platformsDir/osx/Info.plist" "$osxApp/Contents/"
    cp -f "$platformsDir/osx/UVtools.entitlements" "$osxApp/Contents/"
    cp -a "$publishRuntimeDir/." "$osxApp/Contents/MacOS"
    sed -i "s/#VERSION/$version/g" "$osxApp/Contents/Info.plist"

    # Remove the base publish if able
    [ "$keepNetPublish" = false ] && rm -rf "$publishRuntimeDir" 2>/dev/null

    # Packing AppImage
    if [ "$zipPackage" = true -a -d "$osxApp" ] ; then
        echo "7. Compressing '$publishName.app' to '$publishName.zip'"
        cd "$publishDir"
        mv "$publishName.app" "UVtools.app"
        zip -r "$publishDir/$publishName.zip" "UVtools.app"
        mv "UVtools.app" "$publishName.app"
        cd "$rootDir"
        zipPackage=false
    else
        echo "7. Skipping Zip"
    fi
elif [[ $runtime = win-* ]]; then
    echo "6. Windows should be published in a windows machine!"
else
    echo "6. Linux: Creating AppImage bundle"
    linuxApp="$publishDir/$publishName.AppDir"
    linuxAppImage="$publishDir/$publishName.AppImage"
    rm -f "$linuxAppImage" 2>/dev/null
    rm -rf "$linuxApp" 2>/dev/null
    
    cp -rf "$platformsDir/AppImage/." "$linuxApp"
    mkdir -p "$linuxApp/usr/bin"
    cp -a "$publishRuntimeDir/." "$linuxApp/usr/bin"   

    # Download the AppImage creation tool and make it executable
    wget "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
    chmod a+x "appimagetool-x86_64.AppImage"
    # Extract AppImage so it can be run in Docker containers and on  machines that don't have FUSE installed
    # Note: Extracting requires libglib2.0-0 to be installed
    ./appimagetool-x86_64.AppImage --appimage-extract

    # Create the AppImage
    ARCH=x86_64 ./squashfs-root/AppRun "$linuxApp" "$linuxAppImage"
    chmod a+x "$linuxAppImage"

    # Remove the tool
    rm -f "appimagetool-x86_64.AppImage"
    rm -rf "./squashfs-root"

    # Remove the base publish if able
    [ "$keepNetPublish" = false ] && rm -rf "$publishRuntimeDir" 2>/dev/null

    # Packing AppImage
    if [ "$zipPackage" = true -a -f "$linuxAppImage" ] ; then
        echo "7. Compressing '$publishName.AppImage' to '$publishName.zip'"
        cd "$publishDir"
        zip "$publishDir/$publishName.zip" "$publishName.AppImage"
        printf "@ $publishName.AppImage\n@=UVtools.AppImage\n" | zipnote -w "$publishDir/$publishName.zip"
        cd "$rootDir"
        zipPackage=false
    else
        echo "7. Skipping Zip"
    fi
fi

# Zip generic builds
if [ "$zipPackage" = true -a -d "$publishRuntimeDir" ] ; then
    echo "7. Compressing '$runtime' to '$publishName.zip'"
    cd "$publishRuntimeDir"
    zip -r "$publishDir/$publishName.zip" .
    cd "$rootDir"
else
     echo "7. Skipping Zip"
fi

#while true; do
#    read -p "6. Would you like to Zip the package? [Y/N] " yn
#    case $yn in
#        [Yy]* ) 
#        [Nn]* ) echo "Skipping Zip"; break;;
#        * ) echo "Please answer yes or no.";;
#    esac
#done

echo "8. Completed"
