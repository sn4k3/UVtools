# UVtools

[![License](https://img.shields.io/github/license/sn4k3/UVtools?style=flat-square)](https://github.com/sn4k3/UVtools/blob/master/LICENSE)
[![GitHub repo size](https://img.shields.io/github/repo-size/sn4k3/UVtools?style=flat-square)](#)
[![Code size](https://img.shields.io/github/languages/code-size/sn4k3/UVtools?style=flat-square)](#)
[![Total code](https://img.shields.io/tokei/lines/github/sn4k3/UVtools?style=flat-square)](#)
[![Nuget](https://img.shields.io/nuget/v/UVtools.Core?style=flat-square)](https://www.nuget.org/packages/UVtools.Core)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/sn4k3/UVtools?include_prereleases&style=flat-square)](https://github.com/sn4k3/UVtools/releases)
[![Downloads](https://img.shields.io/github/downloads/sn4k3/UVtools/total?style=flat-square)](https://github.com/sn4k3/UVtools/releases)


**MSLA/DLP, file analysis, calibration, repair, conversion and manipulation**

This simple tool can give you insight of supports and find some failures. Did you forget what resin or other settings you used on a project? This can also save you, check every setting that were used with or simply change them!

- Facebook group: https://www.facebook.com/groups/uvtools

![GUI Screenshot](https://raw.githubusercontent.com/sn4k3/UVtools/master/UVtools.GUI/Images/Screenshots/UVtools_GUI.png)
![GUI Screenshot Islands](https://raw.githubusercontent.com/sn4k3/UVtools/master/UVtools.GUI/Images/Screenshots/UVtools_GUI_Islands.png)
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
- Check islands, overhangs, resin traps and repair/remove them as other issues
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
- CBT (Chitubox)
- PHZ (Chitubox)
- FDG (Voxelab)
- PWS (Photon Workshop)
- PW0 (Photon Workshop)
- PWX (Photon Workshop)
- PWMO (Photon Workshop)
- PWMS (Photon Workshop)
- PWMX (Photon Workshop)
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
- VDA.ZIP (Voxeldance Additive)
- VDT (Voxeldance Tango)
- UVJ (Zip file format for manual manipulation)
- Image files (png, jpg, jpeg, gif, bmp)

# Available printers for PrusaSlicer

- **EPAX**
  - E6 Mono
  - E10 Mono
  - X1
  - X10
  - X10 4K Mono
  - X133 4K Mono
  - X156 4K Color
  - X1K 2K Mono
- **Nova3D**
  - Elfin
  - Bene4 Mono
- **AnyCubic**
  - Photon
  - Photon S
  - Photon Zero
- **Elegoo**
  - Mars
  - Mars 2 Pro
  - Mars C
  - Saturn
- **Peopoly**
  - Phenom
  - Phenom L
  - Phenom XXL
  - Phenom Noir
- **QIDI**
  - Shadow5.5
  - Shadow6.0 Pro
  - S-Box
  - 3D ibox mono
- **Phrozen**
  - Shuffle
  - Shuffle Lite
  - Shuffle XL
  - Shuffle XL Lite
  - Shuffle 16
  - Shuffle 4K
  - Sonic
  - Sonic 4K
  - Sonic Mighty 4K
  - Sonic Mini
  - Sonic Mini 4K
  - Transform
- **Kelant S400**
- **Wanhao**
  - D7
  - D8
  - CGR Mini Mono
  - CGR Mono
- **Creality**
  - LD-002R
  - LD-002H
  - LD-006
  - HALOT-ONE CL-60
  - HALOT-SKY CL-89
- **Voxelab**
  - Polaris 5.5
  - Proxima 6
  - Ceres 8.9
- **Longer**
  - Orange 10
  - Orange 30
  - Orange 120
  - Orange 4K
- **Uniz IBEE**
- **Zortrax Inkspire**

# Available profiles for PrusaSlicer

* From 0.01mm to 0.20mm
* Light, Medium and Heavy Supports

# Install and configure profiles under PrusaSlicer

Complete guide: https://github.com/sn4k3/UVtools/wiki/Setup-PrusaSlicer

1. Download and install PrusaSlicer from: https://www.prusa3d.com/prusaslicer/
1. Start and configure PrusaSlicer (Wizard)
    * Choose SL1 printer
1. Close PrusaSlicer
1. Open UVtools if not already
   * Under Menu click -> Help -> Install profiles into PrusaSlicer
1. Open PrusaSlicer and check if profiles are there
1. To clean up interface remove printers that you will not use (OPTIONAL)
1. Duplicate and/or create your printer and tune the values if required
1. Look up under "Printer -> Notes" and configure parameters to the target slicer
1. Change only the value after the "_" (underscore)

## Custom "Material Notes" and "Printer Notes" keywords for PrusaSlicer

Note that some variables will only work if the target format supports them, otherwise they will be ignored.
Replace the "xxx" by your desired value in the correct units

* **BottomLightOffDelay_xxx:** Sets the bottom light off delay time in seconds
* **LightOffDelay_xxx:** Sets the light off delay time in seconds
* **BottomLiftHeight_xxx:** Sets the bottom lift height in millimeters
* **BottomLiftSpeed_xxx:** Sets the bottom lift speed in millimeters/minute
* **LiftHeight_xxx:** Sets the lift height in millimeters
* **LiftSpeed_xxx:** Sets the lift speed in millimeters/minute
* **RetractSpeed_xxx:** Sets the retract speed in millimeters/minute
* **BottomLightPWM_xxx:** Sets the bottom LED light power (0-255)
* **LightPWM_xxx:** Sets the LED light power (0-255)
* **FILEFORMAT_xxx:** Sets the output file format extension to be auto converted once open on UVtools

# File Convertion

https://github.com/sn4k3/UVtools/wiki/Sliced-File-Conversion

# Requirements

## Windows

1. Windows 7 SP1 or greater
   1. If on Windows 10 N or NK: [Media Feature Pack](https://www.microsoft.com/download/details.aspx?id=48231) must be installed
<!-- 1. [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0) installed (Comes pre-installed on Windows 10 with last updates)!-->
1. 4GB RAM or higher
1. 1920 x 1080 @ 100% scale as minimum resolution


## Linux

1. 4GB RAM or higher
2. 64 bit System
1. 1920 x 1080 @ 100% scale as minimum resolution

### Ubuntu/Mint/Debian/Similars

<!--
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y apt-transport-https
dotnet-runtime-5.0
!-->

```bash
sudo apt-get update
sudo apt-get install -y libjpeg-dev libpng-dev libgeotiff-dev libdc1394-22 libavcodec-dev libavformat-dev libswscale-dev libopenexr24 libtbb-dev libgdiplus
```


### Compile libcvextern.so:

**After this if you run UVtools and got a error like:**
> System.DllNotFoundException: unable to load shared library 'cvextern' or one of its dependencies

This means you haven't the required dependencies to run the cvextern library, 
that may due system version and included libraries version, they must match the compiled version of libcvextern.

To know what is missing you can open a terminal on UVtools folder and run the following command: `ldd libcvextern.so |grep not` 
That will return the missing dependencies from libcvextern, you can try install them by other means if you can, 
but most of the time you will need compile the EmguCV to compile the dependencies and correct link them, 
this process is very slow but only need to run once. Open a terminal on any folder of your preference and run the following commands:

```bash
sudo apt-get install -y git build-essential cmake
git clone https://github.com/emgucv/emgucv emgucv 
cd emgucv
git submodule update --init --recursive
cd platforms/ubuntu/20.04
./apt_install_dependency.sh
./cmake_configure.sh
cmake build
```

Make sure all commands run with success.
After run these commands you can try run UVtools again,
if it runs then nothing more is needed and you can remove the emgucv folder, 
this means you only need the dependencies on your system.
 
Otherwise you need to copy the output 'emgucv/libs/x64/libcvextern.so' file created by this compilation to the UVtools folder and replace the original. 
Keep a copy of file somewhere safe, you will need to replace it everytime you update UVtools.
Additionally you can share your libcvextern.so on UVtools GitHub with your system information (Name Version) to help others with same problem, 
anyone with same system version can make use of it without the need of the compilation process.

**Note:** You need to repeat this process everytime UVtools upgrades OpenCV version, keep a eye on changelog.


### Arch/Manjaro/Similars

```bash
sudo pacman -S openjpeg2 libjpeg-turbo libpng libgeotiff libdc1394 libdc1394 ffmpeg openexr tbb libgdiplus
```

To run UVtools open it folder on a terminal and call one of:

* Double-click UVtools file
* `./UVtools`
* `sh UVtools.sh`
* `dotnet UVtools.dll` [For universal package only, requires dotnet-runtime]
* As a pratical alternative you can create a shortcut on Desktop

### Compile libcvextern.so:

**After this if you run UVtools and got a error like:**
> System.DllNotFoundException: unable to load shared library 'cvextern' or one of its dependencies

This means you haven't the required dependencies to run the cvextern library, 
that may due system version and included libraries version, they must match the compiled version of libcvextern.

To know what is missing you can open a terminal on UVtools folder and run the following command: `ldd libcvextern.so |grep not` 
That will return the missing dependencies from libcvextern, you can try install them by other means if you can, 
but most of the time you will need compile the EmguCV to compile the dependencies and correct link them, 
this process is very slow but only need to run once. Open a terminal on any folder of your preference and run the following commands:

```bash
sudo pacman -Syu
sudo pacman -S base-devel git cmake msbuild
git clone https://github.com/emgucv/emgucv emgucv 
cd emgucv
git submodule update --init --recursive
cd platforms/ubuntu/20.04
./cmake_configure.sh
cmake build
```

Make sure all commands run with success.
After run these commands you can try run UVtools again,
if it runs then nothing more is needed and you can remove the emgucv folder, 
this means you only need the dependencies on your system.
 
Otherwise you need to copy the output 'emgucv/libs/x64/libcvextern.so' file created by this compilation to the UVtools folder and replace the original. 
Keep a copy of file somewhere safe, you will need to replace it everytime you update UVtools.
Additionally you can share your libcvextern.so on UVtools GitHub with your system information (Name Version) to help others with same problem, 
anyone with same system version can make use of it without the need of the compilation process.

**Note:** You need to repeat this process everytime UVtools upgrades OpenCV version, keep a eye on changelog.

### RHEL/Fedora/CentOS


```bash
sudo yum update -y
sudo yum install -y https://download1.rpmfusion.org/free/fedora/rpmfusion-free-release-$(rpm -E %fedora).noarch.rpm
sudo yum install -y https://download1.rpmfusion.org/nonfree/fedora/rpmfusion-nonfree-release-$(rpm -E %fedora).noarch.rpm
sudo yum install -y libjpeg-devel libjpeg-turbo-devel libpng-devel libgeotiff-devel libdc1394-devel ffmpeg-devel tbb-devel
```


### Compile libcvextern.so:

**After this if you run UVtools and got a error like:**
> System.DllNotFoundException: unable to load shared library 'cvextern' or one of its dependencies

This means you haven't the required dependencies to run the cvextern library, 
that may due system version and included libraries version, they must match the compiled version of libcvextern.

To know what is missing you can open a terminal on UVtools folder and run the following command: `ldd libcvextern.so |grep not` 
That will return the missing dependencies from libcvextern, you can try install them by other means if you can, 
but most of the time you will need compile the EmguCV to compile the dependencies and correct link them, 
this process is very slow but only need to run once. Open a terminal on any folder of your preference and run the following commands:

```bash
sudo yum groupinstall -y "Development Tools" "Development Libraries"
sudo yum install -y cmake gcc-c++ dotnet-sdk-5.0
git clone https://github.com/emgucv/emgucv emgucv 
cd emgucv
git submodule update --init --recursive
cd platforms/ubuntu/20.04
./cmake_configure.sh
cmake build
```

Make sure all commands run with success.
After run these commands you can try run UVtools again,
if it runs then nothing more is needed and you can remove the emgucv folder, 
this means you only need the dependencies on your system.
 
Otherwise you need to copy the output 'emgucv/libs/x64/libcvextern.so' file created by this compilation to the UVtools folder and replace the original. 
Keep a copy of file somewhere safe, you will need to replace it everytime you update UVtools.
Additionally you can share your libcvextern.so on UVtools GitHub with your system information (Name Version) to help others with same problem, 
anyone with same system version can make use of it without the need of the compilation process.

**Note:** You need to repeat this process everytime UVtools upgrades OpenCV version, keep a eye on changelog.

## Mac

1. macOS 10.13 High Sierra
1. 4GB RAM or higher

<!--- 
* Donwload and install: https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-5.0.101-macos-x64-installer
brew install libjpeg libpng libgeotiff libdc1394 ffmpeg openexr tbb
```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install.sh)"
brew analytics off
brew cask install dotnet
```
-->

To run UVtools open it folder on a terminal and call one of:

* Double-click UVtools file
* `./UVtools`
* `sh UVtools.sh`
* `dotnet UVtools.dll` [For universal package only, requires dotnet-runtime]
* As a pratical alternative you can create a shortcut on Desktop


### Compile libcvextern.dylib: 

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install.sh)"
brew analytics off
brew install git cmake libjpeg libpng libgeotiff libdc1394 ffmpeg openexr tbb mono-libgdiplus
brew install --cask dotnet-sdk
git clone https://github.com/emgucv/emgucv emgucv 
cd emgucv
git submodule update --init --recursive
cd platforms/macos
./configure
```

# How to use

There are multiple ways to open your file:

1. Open UVtools and load your file (CTRL + O) (File -> Open)
2. Open UVtools and drag and drop your file inside window
3. Drag and drop file into UVtools.exe
4. Set UVtools the default program to open your files

# Library -> Developers

Are you a developer? 
This project include a .NET 5.0 library (UVtools.Core) that can be referenced in your application to make use of my work. 
Easy to use calls that allow you work with the formats. For more information navigate main code to see some calls.

Nuget package: https://www.nuget.org/packages/UVtools.Core

[![Nuget](https://img.shields.io/nuget/v/UVtools.Core?style=flat-square)](https://www.nuget.org/packages/UVtools.Core)

```powershell
dotnet add package UVtools.Core
```


## Develop and build from Source

The fastest way to compile the project is by run the `build/CompileWindows.bat`, however if you wish to develop with visual studio follow the following steps:

1. Install Visual Studio and include .NET development support
1. Install the .NET 5.0 SDK if not included on previous instalation
   - https://dotnet.microsoft.com/download/dotnet/5.0
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

- Sponsor: https://github.com/sponsors/sn4k3
- PayPal: https://paypal.me/SkillTournament
