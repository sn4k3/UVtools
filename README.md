# UVtools

[![License](https://img.shields.io/github/license/sn4k3/UVtools)](https://github.com/sn4k3/UVtools/blob/master/LICENSE)
[![GitHub repo size](https://img.shields.io/github/repo-size/sn4k3/UVtools)](https://github.com/sn4k3/UVtools)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/sn4k3/UVtools?include_prereleases)](https://github.com/sn4k3/UVtools/releases)
[![Downloads](https://img.shields.io/github/downloads/sn4k3/UVtools/total)](https://github.com/sn4k3/UVtools/releases)

**MSLA/DLP, file analysis, calibration, repair, conversion and manipulation**

This simple tool can give you insight of supports and find some failures. Did you forget what resin or other settings you used on a project? This can also save you, check every setting that were used with or simply change them!

* Facebook group: https://www.facebook.com/groups/uvtools

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

* View image layer by layer
* View and extract thumbnails
* View all used settings
* Edit print properties and save file
* Mutate and filter layers
* Check islands and repair/remove them as other issues
* Export file to a folder
* Convert format to another format
* Calibration tests
* Portable (No installation needed)

# Known File Formats

* SL1 (PrusaSlicer)
* Zip (Chitubox)
* Photon (Chitubox)
* Photons (Chitubox) [Read-only]
* CBDDLP (Chitubox)
* CBT (Chitubox)
* PHZ (Chitubox)
* FDG (Voxelab)
* PWS (Photon Workshop)
* PW0 (Photon Workshop)
* PWX (Photon Workshop)
* PWMO (Photon Workshop)
* PWMS (Photon Workshop)
* PWMX (Photon Workshop)
* ZCodex (Z-Suite)
* CWS (NovaMaker)
* LGS (Longer Orange 10)
* LGS30 (Longer Orange 30)
* UVJ (Zip file for manual manipulation format)
* Image files (png, jpg, jpeg, gif, bmp)

# Available printers for PrusaSlicer

* EPAX E6 Mono
* EPAX E10 Mono
* EPAX X1
* EPAX X10
* EPAX X10 4K Mono
* EPAX X133 4K Mono
* EPAX X156 4K Color
* EPAX X1K 2K Mono
* Zortrax Inkspire
* Nova3D Elfin
* Nova3D Bene4 Mono
* AnyCubic Photon
* AnyCubic Photon S
* AnyCubic Photon Zero
* Elegoo Mars
* Elegoo Mars 2 Pro
* Elegoo Mars C
* Elegoo Saturn
* Peopoly Phenom
* Peopoly Phenom L
* Peopoly Phenom XXL
* Peopoly Phenom Noir
* QIDI Shadow5.5
* QIDI Shadow6.0 Pro
* QIDI S-Box
* QIDI 3D ibox mono
* Phrozen Shuffle
* Phrozen Shuffle Lite
* Phrozen Shuffle XL
* Phrozen Shuffle XL Lite
* Phrozen Shuffle 16
* Phrozen Shuffle 4K
* Phrozen Sonic
* Phrozen Sonic 4K
* Phrozen Sonic Mighty 4K
* Phrozen Sonic Mini
* Phrozen Sonic Mini 4K
* Phrozen Transform
* Kelant S400
* Wanhao D7
* Wanhao D8
* Wanhao CGR Mini Mono
* Wanhao CGR Mono
* Creality LD-002R
* Creality LD-002H
* Creality LD-006
* Voxelab Polaris 5.5
* Voxelab Proxima 6
* Voxelab Ceres 8.9
* Longer Orange 10
* Longer Orange 30
* Longer Orange4K

# Available profiles for PrusaSlicer

* From 0.01mm to 0.20mm
* Light, Medium and Heavy Supports

# Install and configure profiles under PrusaSlicer

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

## Custom "Printer Notes" keywords

* **FLIP_XY** Flip X with Y resolution, this is required in some cases, it will not affect Prusa output, only used for convertions to another format, use this if you have to use inverted XY under printer settings (Epax for example).

# File Convertion

I highly recommend open the converted file into original slicer and check if it's okay to print, on this beta stage never blind trust the program.
After some tests without failure you can increase your confidence and ignore this stage, or maybe not ;) 

# Requirements

## Windows

1. Windows 7 or greater
   1. If on Windows 10 N or NK: [Media Feature Pack](https://www.microsoft.com/download/details.aspx?id=48231) must be installed
1. [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0) installed (Comes pre-installed on Windows 10 with last updates)
1. 4GB RAM or higher
1. 1980 x 1080 @ 100% scale as minimum resolution


## Linux

1. 4GB RAM or higher
2. 64 bit System
1. 1980 x 1080 @ 100% scale as minimum resolution

### Ubuntu/Mint/Debian/Similars

<!--- 
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y apt-transport-https
dotnet-runtime-5.0
!-->

```bash
sudo apt-get update
sudo apt-get install -y libjpeg-dev libpng-dev libgeotiff-dev libdc1394-22 libavcodec-dev libavformat-dev libswscale-dev libopenexr24 libtbb-dev
```

**After this if you run UVtools and got a error like:**
> System.DllNotFoundException: unable to load shared library 'cvextern' or one of its dependencies

This means you haven't the required dependencies to run the cvextern library, 
that may due system version and included libraries version, they must match the compiled version of libcvextern.

To know what is missing you can open a terminal on UVtools folder and run the following command: `ldd libcvextern.so |grep not` 
That will return the missing dependencies from libcvextern, you can try install them by other means if you can, 
but most of the time you will need compile the EmguCV to compile the dependencies and correct link them, 
this process is very slow but only need once, open a terminal on a folder of your preference and run the following commands:

```bash
sudo apt-get install -y git build-essential cmake
git clone https://github.com/emgucv/emgucv emgucv 
cd emgucv
git submodule update --init --recursive
cd platforms/ubuntu/20.04
./apt_install_dependency
./cmake_configure
cmake build
cd build; make; cd ..
```

Make sure all commands run with success.
After run these commands you can try run UVtools again,
if it runs then nothing more is needed and you can remove the emgucv folder, 
this means you only need the dependencies on your system.
 
Otherwise you need to copy the output libcvextern.so created by this compilation to the UVtools folder and replace the original. 
Keep a copy of file somewhere safe, you will need to replace it everytime you update UVtools.
Additionally you can share your libcvextern.so on UVtools GitHub with your system information (Name Version) to help others with same problem, 
anyone with same system version can make use of it without the need of the compilation process.

**Note:** You need to repeat this process everytime UVtools upgrades OpenCV version, keep a eye on changelog.


### Arch/Manjaro/Similars

```bash
sudo pacman -S openjpeg2 libjpeg-turbo libpng libgeotiff libdc1394 libdc1394 ffmpeg openexr tbb
```

To run UVtools open it folder on a terminal and call one of:

* Double-click UVtools file
* `./UVtools`
* `sh UVtools.sh`
* `dotnet UVtools.dll` [For universal package only, requires dotnet-runtime]
* As a pratical alternative you can create a shortcut on Desktop

## Mac

1. macOS 10.12 Sierra
   1. If on a previous macOS version, use the universal UVtools package 
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

# How to use

There are multiple ways to open your file:

1. Open UVtools and load your file (CTRL + O) (File -> Open)
2. Open UVtools and drag and drop your file inside window
3. Drag and drop file into UVtools.exe
4. Set UVtools the default program to open your files

# Library -> Developers

Are you a developer? This project include a .NET 5.0 library (UVtools.Core) that can be referenced in your application to make use of my work. Easy to use calls that allow you work with the formats. For more information navigate main code.


# TODO
* More file formats
* Clean up (always)
* See features request under Github

# Support my work / Donate

All my work here is given for free (OpenSource), it took some hours to build, test and polish the program.
If you're happy to contribute for a better program and for my work i will appreciate the tip.

PP: https://paypal.me/SkillTournament
