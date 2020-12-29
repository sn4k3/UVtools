#!/bin/bash
cd "$(dirname "$0")"
[ -f UVtools ] && ./UVtools || dotnet UVtools.dll
