#!/bin/bash
#
# This script just builds UVtools on your current system with default configuration
# If you want to see the compilation output, go to: UVtools.WPF/bin/
#
cd "$(dirname "$0")"
cd ..
dotnet build