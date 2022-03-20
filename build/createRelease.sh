#!/bin/bash
#
# Script to publish and pack UVtools to a platform
# Clone the repo first from github:
# git clone https://github.com/sn4k3/UVtools
# Then run this script
# usage 1: ./createRelease.sh clean
# usage 2: ./createRelease.sh -b osx-x64
# usage 3: ./createRelease.sh -b -z osx-x64
#
cd "$(dirname "$0")"
cd ..
[ ! -d "UVtools.Core" ] && echo "UVtools.Core not found!" && exit -1

#runtime=$1
for runtime in $@; do :; done # Get last argument
rootDir="$(pwd)"
buildDir="$rootDir/build"
coreDir="$rootDir/UVtools.Core"
#version="$(grep -oP '<Version>\K(\d\.\d\.\d)(?=<\/Version>)' "$coreDir/UVtools.Core.csproj")" # Not supported on recent macos!
version="$(perl -nle'print $& while m{<Version>\K(\d\.\d\.\d)(?=<\/Version>)}g' "$coreDir/UVtools.Core.csproj")"
platformsDir="$buildDir/platforms"
runtimePlatformDir="$platformsDir/$runtime"
publishName="UVtools_${runtime}_v$version"
publishDir="$rootDir/publish"
publishRuntimeDir="$publishDir/$publishName"
buildProject="UVtools.WPF"
buildWith="Release"
projectDir="$rootDir/$buildProject"
netVersion="6.0"

if [[ $runtime == "clean" ]]; then
    echo "Cleaning publish directory"
    rm -rf "$publishDir" 2>/dev/null
    exit
fi

bundlePublish=false
keepNetPublish=false
zipPackage=false
while getopts 'bzk' flag; do
  case "${flag}" in
    b) bundlePublish=true ;;
    k) keepNetPublish=true ;;
    z) zipPackage=true ;;
    *) echo "Usage:"
        echo "clean Cleans the publish folder and it's contents"
        echo "-b    Bundle and wrap the appication into (.App for mac) or (.AppImage for linux)"
        echo "-k    Keeps the publish contents for the packed runtimes when bundling .App and .AppImage (-b)"
        echo "-z    Zip published package"
        exit 1 ;;
  esac
done

# Checks
if ! command -v dotnet &> /dev/null
then
    echo "Error: dotnet not installed, please install .NET SDK $netVersion.x, dotnet-sdk or dotnet-sdk-$netVersion"
    exit
fi

if [ -z "$version" ]; then
    echo "Error: UVtools version not found, please check path and script"
    exit -1
fi

if [ -z "$runtime" ]; then
    echo "Error: No runtime selected, please pick one of the following:"
    ls "$platformsDir"
    exit -1
fi

if [[ ! $runtime == *-* && ! $runtime == *-arm && ! $runtime == *-x64 && ! $runtime == *-x86 ]]; then
    echo "Error: The runtime '$runtime' is not valid, please pick one of the following:"
    ls "$platformsDir"
    exit -1
fi

if [ ! $runtime == "win-x64" -a ! -d "$platformsDir/$runtime" ]; then
    echo "Error: The runtime '$runtime' is not valid, please pick one of the following:"
    ls "$platformsDir"
    exit -1
fi


echo "1. Preparing the environment"
rm -rf "$publishRuntimeDir" 2>/dev/null
[ "$zipPackage" == true ] && rm -f "$publishDir/$publishName.zip" 2>/dev/null


echo "2. Publishing UVtools v$version for: $runtime"
dotnet publish $buildProject -o "$publishRuntimeDir" -c $buildWith -r $runtime -p:PublishReadyToRun=true --self-contained

echo "3. Copying dependencies"
echo $runtime > "$publishRuntimeDir/runtime_package.dat"
#find "$runtimePlatformDir" -type f | grep -i lib | xargs -i cp -fv {} "$publishRuntimeDir/"
#cp -afv "$runtimePlatformDir/." "$publishRuntimeDir/"
[ -f "$runtimePlatformDir/libcvextern.zip" ] && unzip "$runtimePlatformDir/libcvextern.zip" -d "$publishRuntimeDir"

echo "4. Cleaning up"
rm -rf "$projectDir/bin/$buildWith/net$netVersion/$runtime" 2>/dev/null
rm -rf "$projectDir/obj/$buildWith/net$netVersion/$runtime" 2>/dev/null

echo "5. Setting Permissions"
chmod -fv a+x "$publishRuntimeDir/UVtools"
chmod -fv a+x "$publishRuntimeDir/UVtools.sh"

if [[ $runtime == win-* ]]; then
    echo "6. Windows should be published in a windows machine!"
elif [[ $runtime == osx-* ]]; then
    if [ $bundlePublish == true ]; then
        echo "6. macOS: Creating app bundle"
        osxApp="$publishDir/$publishName.app"
        rm -rf "$osxApp" 2>/dev/null
        mkdir -p "$osxApp/Contents/MacOS"
        mkdir -p "$osxApp/Contents/Resources"

        cp -af "$rootDir/UVtools.CAD/UVtools.icns" "$osxApp/Contents/Resources/"
        cp -af "$platformsDir/osx/Info.plist" "$osxApp/Contents/"
        cp -af "$platformsDir/osx/UVtools.entitlements" "$osxApp/Contents/"
        cp -a "$publishRuntimeDir/." "$osxApp/Contents/MacOS"
        sed -i '' "s/#VERSION/$version/g" "$osxApp/Contents/Info.plist"

        # Remove the base publish if able
        [ "$keepNetPublish" == false ] && rm -rf "$publishRuntimeDir" 2>/dev/null

        # Packing AppImage
        if [ "$zipPackage" == true -a -d "$osxApp" ] ; then
            echo "7. Compressing '$publishName.app' to '$publishName.zip'"
            cd "$publishDir"
            mv "$publishName.app" "UVtools.app"
            zip -rq "$publishDir/$publishName.zip" "UVtools.app"
            mv "UVtools.app" "$publishName.app"
            cd "$rootDir"
            zipPackage=false
        fi
    fi
else
    if [ $bundlePublish == true ]; then
        echo "6. Linux: Creating AppImage bundle"
        linuxAppDir="$publishDir/$publishName.AppDir"
        linuxAppImage="$publishDir/$publishName.AppImage"
        rm -f "$linuxAppImage" 2>/dev/null
        rm -rf "$linuxAppDir" 2>/dev/null
        
        cp -arf "$platformsDir/linux/AppImage/." "$linuxAppDir"
        cp -af "$rootDir/UVtools.CAD/UVtools.png" "$linuxAppDir/"
        mkdir -p "$linuxAppDir/usr/bin"
        cp -a "$publishRuntimeDir/." "$linuxAppDir/usr/bin"   

        # Download the AppImage creation tool and make it executable
        tempDir="$(mktemp -d /tmp/uvtoolspublish.XXXXXX)" # Allow parallel publishes
        appImageFile="appimagetool-x86_64"
        wget -O "$tempDir/appimagetool-x86_64.AppImage" "https://github.com/AppImage/AppImageKit/releases/download/continuous/$appImageFile.AppImage"
        chmod a+x "$tempDir/$appImageFile.AppImage"
        # Extract AppImage so it can be run in Docker containers and on  machines that don't have FUSE installed
        # Note: Extracting requires libglib2.0-0 to be installed
        cd "$tempDir"
        "./$appImageFile.AppImage" --appimage-extract
        cd "$rootDir"

        # Create the AppImage
        ARCH=x86_64 "$tempDir/squashfs-root/AppRun" "$linuxAppDir" "$linuxAppImage"
        chmod a+x "$linuxAppImage"

        # Remove the base publish if able
        #[ "$keepNetPublish" == false ] && rm -rf "$publishRuntimeDir" 2>/dev/null
        #
        # Packing AppImage
        #if [ "$zipPackage" == true -a -f "$linuxAppImage" ] ; then
        #    echo "7. Compressing '$publishName.AppImage' to '$publishName.zip'"
        #    cd "$publishDir"
        #    zip -q "$publishDir/$publishName.zip" "$publishName.AppImage"
        #    printf "@ $publishName.AppImage\n@=UVtools.AppImage\n" | zipnote -w "$publishDir/$publishName.zip"
        #    cd "$rootDir"
        #    zipPackage=false
        #else
        #    echo "7. Skipping Zip"
        #fi

        # Remove the tool & cleanup
        rm -rf "$tempDir"
        rm -rf "$linuxAppDir" 2>/dev/null
    fi
fi

# Zip generic builds
if [ "$zipPackage" == true -a -d "$publishRuntimeDir" ]; then
    echo "7. Compressing '$runtime' to '$publishName.zip'"
    cd "$publishRuntimeDir"
    zip -rq "$publishDir/$publishName.zip" .
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
