#!/bin/bash
#
# This script just builds and runs UVtools on your current system with default configuration
# If you want to see the compilation output, go to: UVtools.UI/bin/
#
cd "$(dirname "$0")"
cd ../UVtools.UI
dotnet run