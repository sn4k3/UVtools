# UVtools
**MSLA/DLP, file analysis, repair, conversion and manipulation**

This simple tool can give you insight of supports and find some failures. Did you forget what resin or other settings you used on a project? This can also save you, check every setting that were used with or simply change them!

* Facebook group: https://www.facebook.com/groups/uvtools

![GUI Screenshot](https://raw.githubusercontent.com/sn4k3/UVtools/master/UVtools.GUI/Images/Screenshots/UVtools_GUI.png)
![GUI Screenshot Islands](https://raw.githubusercontent.com/sn4k3/UVtools/master/UVtools.GUI/Images/Screenshots/UVtools_GUI_Islands.png)
![Convertion Screenshot](https://raw.githubusercontent.com/sn4k3/UVtools/master/UVtools.GUI/Images/Screenshots/SL1ToCbddlp.png)

## Why this project?
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

## Features

* View image layer by layer
* View and extract thumbnails
* View all used settings
* Edit print properties and save file
* Mutate and filter layers
* Check islands and repair/remove them as other issues
* Export file to a folder
* Convert format to another format
* Portable (No installation needed)

## Known Formats

* SL1 (PrusaSlicer)
* Zip (Chitubox)
* Photon (Chitubox)
* CBDDLP (Chitubox)
* CBT (Chitubox)
* PHZ (Chitubox)
* PWS (Photon Workshop)
* PW0 (Photon Workshop)
* ZCodex (Z-Suite)
* CWS (NovaMaker)

## Available printers for PrusaSlicer

* EPAX X1
* EPAX X10
* EPAX X133 4K Mono
* EPAX X156 4K Color
* Zortrax Inkspire
* Nova3D Elfin
* AnyCubic Photon
* AnyCubic Photon S
* AnyCubic Photon Zero
* Elegoo Mars
* Elegoo Mars Saturn
* Peopoly Phenom
* Peopoly Phenom L
* Peopoly Phenom Noir
* QIDI Shadow5.5
* QIDI Shadow6.0 Pro
* Phrozen Shuffle
* Phrozen Shuffle Lite
* Phrozen Shuffle XL
* Phrozen Shuffle 4K
* Phrozen Sonic
* Phrozen Sonic Mini
* Phrozen Transform
* Kelant S400
* Wanhao D7
* Wanhao D8
* Creality LD-002R

## Available profiles for PrusaSlicer

* Universal 0.1 Fast - Heavy Supports
* Universal 0.1 Fast - Medium Supports
* Universal 0.05 Normal - Heavy Supports
* Universal 0.05 Normal - Medium Supports
* Universal 0.035 Detail - Heavy Supports
* Universal 0.035 Detail - Medium Supports
* Universal 0.025 UltraDetail - Heavy Supports
* Universal 0.025 UltraDetail - Medium Supports

## Install and configure profiles under PrusaSlicer

1. Download and install PrusaSlicer from: https://www.prusa3d.com/prusaslicer/
1. Start and configure PrusaSlicer (Wizard)
    * Choose SL1 printer
1. Close PrusaSlicer
1. Open UVtools if not already
   * Under Menu click -> About -> Install profiles into PrusaSlicer
1. Open PrusaSlicer and check if profiles are there
1. To clean up interface remove printers that you will not use (OPTIONAL)
1. Duplicate and/or create your printer and tune the values if required
1. Look up under "Printer -> Notes" and configure parameters to the target slicer
1. Change only the value after the "_" (underscore)

## Custom "Printer Notes" keywords

* **FLIP_XY** Flip X with Y resolution, this is required in some cases, it will not affect Prusa output, only used for convertions to another format, use this if you have to use inverted XY under printer settings (Epax for example).

## File Convertion

I highly recommend open the converted file into original slicer and check if it's okay to print, on this beta stage never blind trust the program.
After some tests without failure you can increase your confidence and ignore this stage, or maybe not ;) 

## Requirements

### Windows

1. Windows 7 or greater
2. .NET Framework 4.8 installed (Comes pre-installed on Windows 10 with last updates)
3. 2 GB RAM or higher

### Mac and Linux

(Not tested nor compiled)
1. Latest Mono

## How to use

There are multiple ways to open your file:

1. Open UVtools.exe and load your file (CTRL + O) (File -> Open)
2. Open UVtools.exe and drag and drop your file inside window
3. Drag and drop file into UVtools.exe
4. Set UVtools.exe the default program to open your files

## Library -> Developers

Are you a developer? This project include a .NET Core library (PrusaSL1Reader) that can be referenced in your application to make use of my work. Easy to use calls that allow you work with the formats. For more information navigate main code.


## TODO
* More file formats
* Clean up (always)
* See features request under Github

## Support my work / Donate

All my work here is given for free (OpenSource), it took some hours to build, test and polish the program.
If you're happy to contribute for a better program and for my work i will appreciate the tip.

PP: https://paypal.me/SkillTournament
