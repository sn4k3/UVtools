@echo off
SET DIR=%~dp0
SET INPUT_DIR=%AppData%\PrusaSlicer\printer
SET OUTPUT_DIR=%~dp0PrusaSlicer\printer

SET files[0]=EPAX X1.ini
SET files[1]=Phrozen Sonic Mini.ini
SET files[2]=Zortrax Inkspire.ini
SET files[3]=Nova3D Elfin.ini

echo PrusaSlicer Printers Instalation
echo This will replace printers, all changes will be discarded
echo %INPUT_DIR%
echo %OUTPUT_DIR%

for /F "tokens=2 delims==" %%s in ('set files[') do xcopy /y "%INPUT_DIR%\%%s" "%OUTPUT_DIR%"

REM xcopy /i /e /y %INPUT_DIR% %OUTPUT_DIR%
pause