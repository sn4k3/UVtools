# Prusa SL1 File Viewer

Open, view, extract and convert SL1 files generated from PrusaSlicer.

This simple tool can also give you insight of supports and find some failures. Did you forget what resin or orther settings you used on a project? This can also save you, check every setting that were used with.

Youtube video: https://www.youtube.com/watch?v=hJD2cgF5rxs

![GUI Screenshot](https://raw.githubusercontent.com/sn4k3/PrusaSL1Viewer/master/PrusaSL1Viewer/Images/Screenshots/PrusaSL1Viewer_GUI.png)
![Convertion Screenshot](https://raw.githubusercontent.com/sn4k3/PrusaSL1Viewer/master/PrusaSL1Viewer/Images/Screenshots/SL1ToCbddlp.png)

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
* Export file to a folder
* Convert SL1 format to another format
* Portable (No installation needed)

## Known Formats

* SL1 (Prusa SL1)
* CBDDLP (Chitubox DLP)
* Photon

## Configure printer under PrusaSlicer

1. Import Epax X1 printer (PrusaSlicer -> printers)
1. Duplicate and tune the values if required
1. Look up under "Printer -> Notes" and configure parameters from target slicer
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

### Mac and Linux

1. Latest Mono

## How to use

There are multiple ways to open your SL1 file:

1. Open PrusaSL1Viewer.exe and load your file (CTRL + O) (File -> Open)
2. Open PrusaSL1Viewer.exe and drag and drop your file inside window
3. Drag and drop sl1 file into PrusaSL1Viewer.exe
4. Set PrusaSL1Viewer.exe the default program to open .sl1 files

## Library -> Developers

Are you a developer? This project include a .NET Core library (PrusaSL1Reader) that can be referenced in your application to make use of my work. Easy to use calls that allow you work with the formats. For more information navigate main code.


## TODO
* Speed up layer preview
* Put convert operation under a task (No GUI freeze)
* More file formats
* Clean up (always)

## Support my work / Donate

All my work here is given for free (OpenSource), it took some hours to build, test and polish the program.
If you're happy to contribute for a better program and for my work i will appreciate the tip.

PP: https://paypal.me/SkillTournament
