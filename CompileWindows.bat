@echo off
SET DIR=%~dp0

REM if exist "%SYSTEMROOT%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" SET MSBUILD_PATH="%SYSTEMROOT%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" SET MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" SET MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"

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
