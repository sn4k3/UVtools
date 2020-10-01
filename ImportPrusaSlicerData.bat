@echo off
SET DIR=%~dp0
SET INPUT_DIR=%AppData%\PrusaSlicer
SET OUTPUT_DIR=%~dp0PrusaSlicer

SET PRINT_DIR=sla_print
SET PRINTER_DIR=printer

SET files[0]=EPAX E6 Mono.ini
SET files[1]=EPAX E10 Mono.ini
SET files[2]=EPAX X1.ini
SET files[3]=EPAX X10.ini
SET files[4]=EPAX X10 4K Mono.ini
SET files[5]=EPAX X133 4K Mono.ini
SET files[6]=EPAX X156 4K Color.ini
SET files[7]=EPAX X1K 2K Mono.ini
SET files[8]=Zortrax Inkspire.ini
SET files[9]=Nova3D Elfin.ini
SET files[10]=Nova3D Bene4 Mono.ini
SET files[11]=AnyCubic Photon.ini
SET files[12]=AnyCubic Photon S.ini
SET files[13]=AnyCubic Photon Zero.ini
SET files[14]=Elegoo Mars.ini
SET files[15]=Elegoo Mars 2 Pro.ini
SET files[16]=Elegoo Mars C.ini
SET files[17]=Elegoo Saturn.ini
SET files[18]=Peopoly Phenom.ini
SET files[19]=Peopoly Phenom L.ini
SET files[20]=Peopoly Phenom Noir.ini
SET files[21]=QIDI Shadow5.5.ini
SET files[22]=QIDI Shadow6.0 Pro.ini
SET files[23]=QIDI S-Box.ini
SET files[24]=Phrozen Shuffle.ini
SET files[25]=Phrozen Shuffle Lite.ini
SET files[26]=Phrozen Shuffle XL.ini
SET files[27]=Phrozen Shuffle XL Lite.ini
SET files[28]=Phrozen Shuffle 16.ini
SET files[29]=Phrozen Shuffle 4K.ini
SET files[30]=Phrozen Sonic.ini
SET files[31]=Phrozen Sonic 4K.ini
SET files[32]=Phrozen Sonic Mighty 4K.ini
SET files[33]=Phrozen Sonic Mini.ini
SET files[34]=Phrozen Sonic Mini 4K.ini
SET files[35]=Phrozen Transform.ini
SET files[36]=Kelant S400.ini
SET files[37]=Wanhao D7.ini
SET files[38]=Wanhao D8.ini
SET files[39]=Creality LD-002R.ini
SET files[40]=Creality LD-002H.ini
SET files[41]=Creality LD-006.ini
SET files[42]=Voxelab Polaris.ini
SET files[43]=Voxelab Proxima.ini
SET files[44]=Longer Orange 10.ini
SET files[45]=Longer Orange 30.ini
SET files[46]=Longer Orange4K.ini

echo PrusaSlicer Printers Instalation
echo This will replace printers, all changes will be discarded
echo %INPUT_DIR%
echo %OUTPUT_DIR%

echo Importing Printers
for /F "tokens=2 delims==" %%s in ('set files[') do xcopy /d /y "%INPUT_DIR%\%PRINTER_DIR%\%%s" "%OUTPUT_DIR%\%PRINTER_DIR%\"

echo Importing Profiles
xcopy /i /y /d %INPUT_DIR%\%PRINT_DIR% %OUTPUT_DIR%\%PRINT_DIR%

REM /s Copies directories and subdirectories, unless they are empty. If you omit /s, xcopy works within a single directory.
REM /y Suppresses prompting to confirm that you want to overwrite an existing destination file.
REM /i If Source is a directory or contains wildcards and Destination does not exist, 
REM		xcopy assumes Destination specifies a directory name and creates a new directory. 
REM 	Then, xcopy copies all specified files into the new directory. 
REM 	By default, xcopy prompts you to specify whether Destination is a file or a directory.
REM /d Copies source files changed on or after the specified date only. 
REM		If you do not include a MM-DD-YYYY value, xcopy copies all Source files that are newer than existing Destination files. 
REM		This command-line option allows you to update files that have changed.

pause