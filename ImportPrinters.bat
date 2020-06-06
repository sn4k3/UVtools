@echo off
SET DIR=%~dp0
SET INPUT_DIR=%AppData%\PrusaSlicer\printer
SET OUTPUT_DIR=%~dp0PrusaSlicer\printer

SET files[0]=EPAX X1.ini
SET files[1]=EPAX X10.ini
SET files[2]=EPAX X133 4K Mono.ini
SET files[3]=EPAX X156 4K Color.ini
SET files[4]=Zortrax Inkspire.ini
SET files[5]=Nova3D Elfin.ini
SET files[6]=AnyCubic Photon.ini
SET files[7]=Elegoo Mars.ini
SET files[8]=Elegoo Mars Saturn.ini
SET files[9]=Peopoly Phenom.ini
SET files[10]=Peopoly Phenom L.ini
SET files[11]=Peopoly Phenom Noir.ini
SET files[12]=QIDI Shadow5.5.ini
SET files[13]=QIDI Shadow6.0 Pro.ini
SET files[14]=Phrozen Shuffle.ini
SET files[15]=Phrozen Shuffle Lite.ini
SET files[16]=Phrozen Shuffle XL.ini
SET files[17]=Phrozen Shuffle 4K.ini
SET files[18]=Phrozen Sonic.ini
SET files[19]=Phrozen Sonic Mini.ini
SET files[20]=Phrozen Transform.ini

echo PrusaSlicer Printers Instalation
echo This will replace printers, all changes will be discarded
echo %INPUT_DIR%
echo %OUTPUT_DIR%

for /F "tokens=2 delims==" %%s in ('set files[') do xcopy /y "%INPUT_DIR%\%%s" "%OUTPUT_DIR%"

REM xcopy /i /e /y %INPUT_DIR% %OUTPUT_DIR%
pause