@echo off
REM
REM This script just builds and runs UVtools on your current system with default configuration
REM If you want to see the compilation output, go to: UVtools.WPF/bin/
REM
SET DIR=%~dp0
cd ../UVtools.WPF

echo Compiling and run UVtools.
echo This windows will close once you quit UVtools program.
dotnet run
