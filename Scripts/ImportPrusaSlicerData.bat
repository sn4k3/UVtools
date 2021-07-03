@echo off
SET DIR=%~dp0
cd ..
SET INPUT_DIR=%AppData%\PrusaSlicer
SET OUTPUT_DIR=PrusaSlicer

SET PRINT_DIR=sla_print
SET PRINTER_DIR=printer

SET files[0]=UVtools Prusa SL1.ini
SET files[1]=UVtools Prusa SL1S Speed.ini
SET files[2]=EPAX E6 Mono.ini
SET files[3]=EPAX E10 Mono.ini
SET files[4]=EPAX X1.ini
SET files[5]=EPAX X10.ini
SET files[6]=EPAX X10 4K Mono.ini
SET files[7]=EPAX X133 4K Mono.ini
SET files[8]=EPAX X156 4K Color.ini
SET files[9]=EPAX X1K 2K Mono.ini
SET files[10]=Zortrax Inkspire.ini
SET files[11]=Nova3D Elfin.ini
SET files[12]=Nova3D Bene4 Mono.ini
SET files[13]=AnyCubic Photon.ini
SET files[14]=AnyCubic Photon S.ini
SET files[15]=AnyCubic Photon Zero.ini
SET files[16]=AnyCubic Photon X.ini
SET files[17]=AnyCubic Photon Mono.ini
SET files[18]=AnyCubic Photon Mono SE.ini
SET files[19]=AnyCubic Photon Mono X.ini
SET files[20]=Elegoo Mars.ini
SET files[21]=Elegoo Mars 2 Pro.ini
SET files[22]=Elegoo Mars C.ini
SET files[23]=Elegoo Saturn.ini
SET files[24]=Peopoly Phenom.ini
SET files[25]=Peopoly Phenom L.ini
SET files[26]=Peopoly Phenom Noir.ini
SET files[27]=Peopoly Phenom XXL.ini
SET files[28]=QIDI Shadow5.5.ini
SET files[29]=QIDI Shadow6.0 Pro.ini
SET files[30]=QIDI S-Box.ini
SET files[31]=QIDI I-Box Mono.ini
SET files[32]=Phrozen Shuffle.ini
SET files[33]=Phrozen Shuffle Lite.ini
SET files[34]=Phrozen Shuffle XL.ini
SET files[35]=Phrozen Shuffle XL Lite.ini
SET files[36]=Phrozen Shuffle 16.ini
SET files[37]=Phrozen Shuffle 4K.ini
SET files[38]=Phrozen Sonic.ini
SET files[39]=Phrozen Sonic 4K.ini
SET files[40]=Phrozen Sonic Mighty 4K.ini
SET files[41]=Phrozen Sonic Mini.ini
SET files[42]=Phrozen Sonic Mini 4K.ini
SET files[43]=Phrozen Transform.ini
SET files[44]=Kelant S400.ini
SET files[45]=Wanhao D7.ini
SET files[46]=Wanhao D8.ini
SET files[47]=Wanhao CGR Mini Mono.ini
SET files[48]=Wanhao CGR Mono.ini
SET files[49]=Creality LD-002R.ini
SET files[50]=Creality LD-002H.ini
SET files[51]=Creality LD-006.ini
SET files[52]=Creality HALOT-ONE CL-60.ini
SET files[53]=Creality HALOT-SKY CL-89.ini
SET files[54]=Voxelab Polaris 5.5.ini
SET files[55]=Voxelab Proxima 6.ini
SET files[56]=Voxelab Ceres 8.9.ini
SET files[57]=Longer Orange 10.ini
SET files[58]=Longer Orange 30.ini
SET files[59]=Longer Orange 120.ini
SET files[60]=Longer Orange 4K.ini
SET files[61]=Uniz IBEE.ini

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