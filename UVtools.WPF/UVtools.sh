#!/bin/bash
path="$(dirname "$0")"
arch="$(uname -m)" # x86_64 or arm64

testcmd() { command -v "$1" &> /dev/null; }

run_app_normal(){
    exec "$path/UVtools" $@
}

run_app_dotnet(){ 
    if ! testcmd dotnet; then
        echo "$(uname) $arch requires dotnet in order to run."
        echo 'Please use the auto installer script to install all the dependencies.'
        exit -1
    fi

    if [ -f "$path/UVtools.dll" ]; then
        exec dotnet "$path/UVtools.dll" $@
    else
        echo "Error: UVtools.dll not found."
        exit -1
    fi
}

if [ "${OSTYPE:0:6}" == "darwin" ]; then
    if [ -d "$path/../../../UVtools.app" ]; then
        open -n "$path/../../../UVtools.app" --args $@
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
