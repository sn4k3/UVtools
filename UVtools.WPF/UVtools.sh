#!/bin/bash
path="$(dirname "$0")"
arch="$(uname -m)" # x86_64 or arm64
args="$@"

run_app_normal(){
    "$path/UVtools" $args
}

run_app_dotnet(){ 
    if [ -z "$(command -v dotnet)" ]; then
        echo "$(uname) $arch requires dotnet in order to run."
        echo 'Please use the auto installer script to install all the dependencies.'
        exit -1
    fi

    if [ -f "$path/UVtools.dll" ]; then
        dotnet "$path/UVtools.dll" $args
    else
        echo "Error: UVtools.dll not found."
        exit -1
    fi
}

if [ "${OSTYPE:0:6}" == "darwin" ]; then
    if [ "$arch" == "arm64" ]; then
        run_app_dotnet
    elif [ -d "$path/../../../UVtools.app" ]; then
        open -n "$path/../../../UVtools.app" --args $args
    else
        run_app_normal
    fi
elif [ -f "$path/UVtools" ]; then
    run_app_normal
elif [ -f "$path/UVtools.dll" ]; then
    run_app_dotnet
else
    echo 'Error: Unable to detect UVtools or to run it.'
    echo 'Please use the auto installer script to re/install UVtools.'
    exit -1
fi
