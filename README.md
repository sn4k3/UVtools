# UVtools

[![License](https://img.shields.io/github/license/sn4k3/UVtools?style=for-the-badge)](https://github.com/sn4k3/UVtools/blob/master/LICENSE)
[![GitHub repo size](https://img.shields.io/github/repo-size/sn4k3/UVtools?style=for-the-badge)](#)
[![Code size](https://img.shields.io/github/languages/code-size/sn4k3/UVtools?style=for-the-badge)](#)
[![Total code](https://img.shields.io/tokei/lines/github/sn4k3/UVtools?style=for-the-badge)](#)
[![Nuget](https://img.shields.io/nuget/v/UVtools.Core?style=for-the-badge)](https://www.nuget.org/packages/UVtools.Core)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/sn4k3/UVtools?include_prereleases&style=for-the-badge)](https://github.com/sn4k3/UVtools/releases)
[![Downloads](https://img.shields.io/github/downloads/sn4k3/UVtools/total?style=for-the-badge)](https://github.com/sn4k3/UVtools/releases)
[![Chocolatey](https://img.shields.io/chocolatey/dt/uvtools?color=brown&label=Chocolatey&style=for-the-badge)](https://community.chocolatey.org/packages/uvtools)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/sn4k3?color=red&style=for-the-badge)](https://github.com/sponsors/sn4k3)

## Download the latest version at:
https://github.com/sn4k3/UVtools/releases/latest  

## To auto install on Windows via a package manager:
- **Winget:** `winget install -e --id PTRTECH.UVtools`
- **Chocolatey:** `choco install -y uvtools`

**Note:** Winget is included on Windows 11 by default.

## To auto install on MacOS via homebrew:
`brew install --cask uvtools`

# MSLA/DLP, file analysis, calibration, repair, conversion and manipulation

This simple tool can give you insight of supports and find some failures. Did you forget what resin or other settings you used on a project? This can also save you, check every setting that were used with or simply change them!

- Facebook group: https://www.facebook.com/groups/uvtools

![GUI Screenshot](https://raw.githubusercontent.com/sn4k3/UVtools/master/wiki/UI1.png)
![GUI Screenshot Islands](https://raw.githubusercontent.com/sn4k3/UVtools/master/wiki/UI2.png)
![Convertion Screenshot](https://raw.githubusercontent.com/sn4k3/UVtools/master/UVtools.GUI/Images/Screenshots/SL1ToCbddlp.png)

# Why this project?
I don't own a Prusa SL1 or any other resin printer, for now I’m only a FDM user with 
Prusa MK3 and a Ender3.
PrusaSlicer is my only choose, why? Because I think it's the best and feature more, 
at least for me, simple but powerful. 

So why this project? Well in fact I’m looking for a resin printer and i like to study 
and learn first before buy, get good and don't regret, and while inspecting i found that 
resin printers firmwares are not as universal as FDM, too many file formats and there 
before each printer can use their own property file, this of course limit the software selection,
for example, only PrusaSlicer can slice SL1 files. So with that in mind i'm preparing when I got
some resin printer in future I can use PrusaSlicer instead of others. 
I've explored the other slicers and again, no one give me joy, and i feel them unstable,
many users slice model on PrusaSlicer just to get those supports and export stl to load in another,
that means again PrusaSlicer is on the win side, the problem is they can't slice directly on PrusaSlicer,
so, in the end, my project aims to do almost that, configure a printer on PrusaSlicer, eg: EPAX X1,
slice, export file, convert SL1 to native printer file and print.

Please note i don't have any resin printer! All my work is virtual and calculated, 
so, use experimental functions with care! Once things got confirmed a list will show. 
But also, i need victims for test subject. Proceed at your own risk!

# Features

- View image layer by layer
- View and extract thumbnails
- View all used settings
- Edit print properties and save file
- Mutate and filter layers
- Check islands, overhangs, resin traps, empty layers and repair/remove them as other issues
- Export file to a folder
- Convert format to another format
- Calibration tests
- Portable (No installation needed)

# Known File Formats

- SL1 (PrusaSlicer)
- SL1S (PrusaSlicer)
- Zip (Chitubox)
- Photon (Chitubox)
- Photons (Chitubox)
- CBDDLP (Chitubox)
- CTB (Chitubox)
- PHZ (Chitubox)
- FDG (Voxelab)
- PWS (Photon Workshop)
- PW0 (Photon Workshop)
- PWX (Photon Workshop)
- DLP (Photon Workshop)
- DL2P (Photon Workshop)
- PWMO (Photon Workshop)
- PWMA (Photon Workshop)
- PWMS (Photon Workshop)
- PWMX (Photon Workshop)
- PWMB (Photon Workshop)
- PWSQ (Photon Workshop)
- PM3 (Photon Workshop)
- PM3M (Photon Workshop)
- PWC (Photon Workshop)
- JXS (GKone Slicer)
- ZCode (UnizMaker)
- ZCodex (Z-Suite)
- CWS (NovaMaker)
- RGB.CWS (Nova Bene4 Mono / Elfin2 Mono SE)
- XML.CWS (Wanhao Workshop)
- MDLP (Makerbase MKS-DLP v1)
- GR1 (GR1 Workshop)
- CXDLP (Creality Box)
- LGS (Longer Orange 10)
- LGS30 (Longer Orange 30)
- LGS120 (Longer Orange 120)
- LGS4K (Longer Orange 4K & mono)
- Flashforge SVGX
- ZIP (Generic / Phrozen Zip)
- VDA.ZIP (Voxeldance Additive)
- VDT (Voxeldance Tango)
- OSF (Vlare Open File Format)
- UVJ (Zip file format for manual manipulation)
- Image files (png, jpg, jpeg, jp2, tif, bmp, pbm, pgm, ras, sr)

# Available printers for PrusaSlicer

See at: https://github.com/sn4k3/UVtools/tree/master/PrusaSlicer/printer

# Available profiles for PrusaSlicer

* From 0.01mm to 0.20mm
* Light, Medium and Heavy Supports

# Install and configure profiles under PrusaSlicer

Complete guide: https://github.com/sn4k3/UVtools/wiki/Setup-PrusaSlicer

## Custom "Material Notes" and "Printer Notes" keywords for PrusaSlicer

Note that some variables will only work if the target format supports them, otherwise they will be ignored.
Replace the "xxx" by your desired value in the correct units

- **TransitionLayerCount_xxx:** Sets the number of transition layers
- **BottomLightOffDelay_xxx:** Sets the bottom light off delay time in seconds
- **LightOffDelay_xxx:** Sets the light off delay time in seconds
- **BottomWaitTimeBeforeCure_xxx:** Sets the bottom wait time before cure in seconds
- **WaitTimeBeforeCure_xxx:** Sets the wait time before cure in seconds
- **BottomWaitTimeAfterCure_xxx:** Sets the bottom wait time after cure in seconds
- **WaitTimeAfterCure_xxx:** Sets the wait time after cure in seconds
- **BottomLiftHeight_xxx:** Sets the bottom lift height in millimeters
- **BottomLiftSpeed_xxx:** Sets the bottom lift speed in millimeters/minute
- **LiftHeight_xxx:** Sets the lift height in millimeters
- **LiftSpeed_xxx:** Sets the lift speed in millimeters/minute
- **BottomWaitTimeAfterLift_xxx:** Sets the bottom wait time after lift in seconds
- **WaitTimeAfterLift_xxx:** Sets the wait time after lift in seconds
- **RetractSpeed_xxx:** Sets the retract speed in millimeters/minute
- **BottomLightPWM_xxx:** Sets the bottom LED light power (0-255)
- **LightPWM_xxx:** Sets the LED light power (0-255)
- **FILEVERSION_n:** Sets the output file format version/revision
- **FILEFORMAT_xxx:** Sets the output file format extension to be auto converted once open on UVtools

# File Convertion

https://github.com/sn4k3/UVtools/wiki/Sliced-File-Conversion

# Command-line

## UVtoolsCmd (Console) executable

```bash
Usage:
  UVtoolsCmd [command] [options]

Options:
  -q, --quiet     Make output silent but exceptions error will still show
  --no-progress   Show no progress
  --core-version  Show core version information
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  run <input-file> <files>                              Run operations and/or scripts
  convert <input-file> <target-type/ext> <output-file>  Convert input file into a output file format by a known type or
                                                        extension []
  extract <input-file> <output-folder>                  Extract file contents to a folder []
  copy-parameters <input-file> <target-files>           Copy print parameters from one file to another
  print-properties <input-file>                         Prints available properties
  print-layers <input-file>                             Prints layer(s) properties
  print-gcode <input-file>                              Prints the gcode of the file if available
  print-machines                                        Prints machine settings
```

Note: On each command you can use -? to see specific command help and extra options

## UVtools (UI) executable

- **Open file(s):**
   - **Syntax:** UVtools \<file1\> [file2] [file3] ...
   - **Example 1:** UVtools C:\model.osla
   - **Example 2:** UVtools C:\model.zip D:\other_model.osla
   - **Note:** When a invalid file is pass, the program will open as default.
- **Redirect a command to UVtoolsCmd:**
   - **Syntax:** UVtools --cmd \<commands ...\>
   - **Example 1:** UVtools --cmd convert C:\model.osla zip
   - **Note:** This can be used when UVtoolsCmd is not directly exposed, for example if you are running via a .AppImage.  
               All commands will be redirected to `UVtoolsCmd` and the UI will not run. It still shows the terminal window.

### Legacy

The following commands are the old way and commands under the UI executable, they will be removed in near future, try to not use them, please prefer **UVtoolsCmd**.

- **Convert a file into another type(s)**
   - **Syntax:** UVtools -c/--convert \<input_file\> \<output_file1_or_ext\> [output_file2_or_ext] ...
   - **Example 1:** UVtools -c model.zip osla
   - **Example 2:** UVtools -c model.zip model_converted.osla
   - **Example 3:** UVtools --convert model.zip model_converted.osla model_converted.zcode
   - **Note:** Nothing happen when providing wrong files, will quit.
- **Extract a file to a folder**
   - **Syntax:** UVtools -e/--extract \<input_file\> [output_folder]
   - **Example 1:** UVtools -e model.zip
   - **Example 2:** UVtools -e model.zip mymodel
   - **Example 3:** UVtools --extract model.zip .
   - **Note:** Nothing happen when providing wrong files/folder, will quit.
- **Export a file to a 3D mesh**
   - **Syntax:** UVtools --export-mesh \<input_file\> [output_mesh_file]
   - **Example 1:** UVtools --export-mesh model.zip
   - **Example 2:** UVtools --export-mesh model.zip model_exported.stl
   - **Note:** Nothing happen when providing wrong files, will quit.
- **Run a operation and save the file**
   - **Syntax:** UVtools --run-operation \<input_file\> \<operation_file.uvtop\>
   - **Example 1:** UVtools --run-operation model.zip MyMorph.uvtop
   - **Note:** Nothing happen when providing wrong files, will quit.
- **Run a script and save the file**
   - **Syntax:** UVtools --run-script \<input_file\> \<script_file.cs\>
   - **Example 1:** UVtools --run-script model.zip myScript.cs
   - **Note:** Nothing happen when providing wrong files, will quit.
- **Copy print parameters from one file to another**
   - **Syntax:** UVtools --copy-parameters \<from_file\> \<to_file\>
   - **Example 1:** UVtools --copy-parameters model.zip otherfile.zip
   - **Note:** Nothing happen when providing wrong files, will quit.

# Requirements

## Windows

1. Windows 7 SP1 or greater
   1. If on Windows 10/11 N or NK:
      - [Media Feature Pack](https://www.microsoft.com/download/details.aspx?id=48231) must be installed
      - Press Windows + R
      - Type: appwiz.cpl (and press Enter key)
      - Click on: Turn Windows features on or off
      - Check the "Media Extensions" and click Ok
<!-- 1. [.NET 6.0](https://dotnet.microsoft.com/download/dotnet/6.0) installed (Comes pre-installed on Windows 10 with last updates)!-->
1. 4GB RAM or higher
1. 1920 x 1080 @ 100% scale as minimum resolution

## Linux

1. 4GB RAM or higher
2. 64 bit System
1. 1920 x 1080 @ 100% scale as minimum resolution

### Ubuntu/Mint/Debian/Similars
*Install Dependencies*

- Ubuntu 20.04
```bash
sudo apt-get update
sudo apt-get install -y libjpeg-dev libpng-dev libgeotiff-dev libgeotiff5 libdc1394-22 libavcodec-dev libavformat-dev libswscale-dev libopenexr24 libtbb-dev libgl1-mesa-dev libgdiplus wget
```
- Ubuntu 22.04 (brings `libdc1394-25` instead of `-22` and `libopenexr25` instead of `libopenexr24`
```bash
sudo apt update
sudo apt install -y libjpeg-dev libpng-dev libgeotiff-dev libgeotiff5 libdc1394-25 libavcodec-dev libavformat-dev libswscale-dev libopenexr25 libtbb-dev libgl1-mesa-dev libgdiplus wget
```

- Fix libdl symlink
```
wget -qO - https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/libdl-solver.sh | sudo bash
```

### Arch/Manjaro/Similars

```bash
sudo pacman -Syu
sudo pacman -S openjpeg2 libjpeg-turbo libpng libgeotiff libdc1394 ffmpeg openexr tbb libgdiplus wget
wget -qO - https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/libdl-solver.sh | sudo bash
```

### RHEL/Fedora/CentOS


```bash
sudo yum update -y
sudo yum install -y https://download1.rpmfusion.org/free/fedora/rpmfusion-free-release-$(rpm -E %fedora).noarch.rpm
sudo yum install -y https://download1.rpmfusion.org/nonfree/fedora/rpmfusion-nonfree-release-$(rpm -E %fedora).noarch.rpm
sudo yum install -y libjpeg-devel libjpeg-turbo-devel libpng-devel libgeotiff-devel libdc1394-devel ffmpeg-devel tbb-devel mesa-libGL wget
wget -qO - https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/libdl-solver.sh | sudo bash
```


**To run UVtools open it folder on a terminal and call one of:**

- Double-click UVtools file
- `./UVtools`
- `bash UVtools.sh`
- `dotnet UVtools.dll` [For universal package only, requires dotnet-runtime]
- As a pratical alternative you can create a shortcut on Desktop

If you downloaded the **.AppImage** package variant you must set run permissions to it before attempt to run it:  
![AppImage permissions](https://github.com/sn4k3/UVtools/raw/master/wiki/linux_AppImage_permissions.png)

## Mac

1. macOS 10.15 Catalina or higher
1. 4GB RAM or higher
3. **For Mac M1 (ARM):**
   1. Install homebrew if not already:
      ```bash
      /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install.sh)"
      brew analytics off
      ```
   2. Downgrade ffmpeg:
      ```bash
      brew install cmake ffmpeg@4
      brew link ffmpeg@4
      ```
    3. Reboot

<!--- 
* Donwload and install: https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-6.0.101-macos-x64-installer
brew install libjpeg libpng libgeotiff libdc1394 ffmpeg openexr tbb
```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install.sh)"
brew analytics off
brew cask install dotnet
brew install --cask dotnet-sdk
brew install git cmake libjpeg libpng libgeotiff libdc1394 ffmpeg openexr tbb mono mono-libgdiplus libusb
```
-->

To run UVtools open it folder on a terminal and call one of:

- Double-click UVtools file
- `./UVtools.app/Contents/MacOS/UVtools`
- `bash UVtools.app/Contents/MacOS/UVtools.sh`
- As a pratical alternative you can create a shortcut on Desktop

# How to use

There are multiple ways to open your file in UVtools:

1. Open UVtools and load your file (CTRL + O) (File -> Open)
2. Open UVtools and drag and drop your file inside window
3. Drag and drop file into UVtools.exe
4. Set UVtools the default program to open your files

# Library -> Developers

Are you a developer? 
This project include a .NET 6.0 library (UVtools.Core) that can be referenced in your application to make use of my work. 
Easy to use calls that allow you work with the formats. For more information navigate main code to see some calls.

Nuget package: https://www.nuget.org/packages/UVtools.Core

[![Nuget](https://img.shields.io/nuget/v/UVtools.Core?style=for-the-badge)](https://www.nuget.org/packages/UVtools.Core)

```powershell
dotnet add package UVtools.Core
```

## Scripting:

- [Powershell](https://github.com/sn4k3/UVtools/tree/master/Scripts/UVtools.Powershell)
- [Python](https://github.com/sn4k3/UVtools/tree/master/Scripts/UVtools.Python)

## Develop and build from Source

[Build directions](https://github.com/sn4k3/UVtools/tree/master/build)  
The fastest way to compile the project is by run the `build/compile.bat`, however if you wish to develop with visual studio follow the following steps:

1. Install Visual Studio and include .NET development support
1. Install the .NET 6.0 SDK if not included on previous instalation
   - https://dotnet.microsoft.com/download/dotnet/6.0
1. Install the Avalonia for Visual Sutdio:
   - https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaforVisualStudio
1. Install the Wix Toolset: (Required only for MSI build, **optional**)
   1. https://wixtoolset.org/releases
   1. https://marketplace.visualstudio.com/items?itemName=WixToolset.WiXToolset
1. Open UVtools.sln
1. Build


# TODO
- More file formats
- Clean up & performance (always)
- See features request under Github


# Support my work / Donate

All my work here is given for free (OpenSource), it took some hours to build, test and polish the program.
If you're happy to contribute for a better program and for my work i will appreciate the tip.  
Use one of the following methods:

[![GitHub Sponsors](https://img.shields.io/badge/Donate-Sponsor-red?style=for-the-badge)](https://github.com/sponsors/sn4k3)
[![Donate PayPal](https://img.shields.io/badge/Donate-PayPal-blue?style=for-the-badge)](https://paypal.me/SkillTournament)

# Contributors 
[![GitHub contributors](https://img.shields.io/github/contributors/sn4k3/UVtools?style=for-the-badge)](https://github.com/sn4k3/UVtools/graphs/contributors)  
[![Contributors](https://contrib.rocks/image?repo=sn4k3/UVtools)](https://github.com/sn4k3/UVtools/graphs/contributors)
