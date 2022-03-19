@echo off
REM
REM This script just builds and runs UVtools on your current system with default configuration
REM If you want to see the compilation output, go to: UVtools.WPF/bin/
REM
SET DIR=%~dp0
cd ..

REM if exist "%SYSTEMROOT%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" SET MSBUILD_PATH="%SYSTEMROOT%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" SET MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" SET MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" SET MSBUILD_PATH="%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"

IF [%MSBUILD_PATH%] == [] GOTO noMSBuild

echo UVtools.sln Compile
echo %MSBUILD_PATH%
%MSBUILD_PATH% -p:Configuration=Release UVtools.sln
GOTO end


:noMSBuild
    echo MSBuild.exe path not found! trying 'dotnet' instead
    dotnet build 

:end
    pause
