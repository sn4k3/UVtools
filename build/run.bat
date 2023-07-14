@echo off
REM
REM This script just builds and runs UVtools on your current system with default configuration
REM If you want to see the compilation output, go to: UVtools.UI/bin/
REM
SET DIR=%~dp0
cd ../UVtools.UI

echo Compiling and run UVtools.
echo This windows will close once you quit UVtools program.
dotnet run
