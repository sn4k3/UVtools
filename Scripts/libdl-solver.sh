#!/bin/bash

# Attempts to create symbolic for libdl.so
# And to solve the OpenCV "unable to load shared library 'cvextern' or one of its dependencies" error
# Note: Run with sudo bash not sh!
#
# Author: Tiago Conceição
# Version: 1
#

if [[ $EUID -ne 0 ]]; then
   echo "This script must be run as root" 
   exit 1
fi

#####################
##  CONFIGURATION  ##
#####################
libdlso="libdl.so"
libdlso2="libdl.so.2"

directories="/lib"
directories="$directories /usr/lib64"
directories="$directories /lib/x86_64-linux-gnu"

#####################
##   DON'T TOUCH   ##
#####################
checks=0
links=0
echo "Attempting to create symbolic from '$libdlso2' to '$libdlso' on known path's:"
ldconfig -p | grep libdl
echo ""
for directory in $directories; do
    let "checks++"
    echo "Checking for $directory/$libdlso2"
    if [[ -f "$directory/$libdlso2" && ! -f "$directory/$libdlso" ]]; then
        echo "Match: Creating symbolic link to $libdlso"
        ln -s "$directory/$libdlso2" "$directory/$libdlso"
        let "links++"
    fi
done

echo ""
echo "######## Results #########"
echo "File checks: $checks"
echo "Created links: $links"
echo "Finished!"
echo "##########################"