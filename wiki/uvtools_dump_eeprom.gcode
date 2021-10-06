; UVtools dump EEPROM settings to file
; https://github.com/sn4k3/UVtools/wiki/Rest-times-and-TSMC
;
; Instructions:
; 1. Put this file in the printer USB
; 2. Print this file
; 3. Take USB out and connect to PC
; 4. Open and read "memory_dump.txt"
; 
; NOTE: If you wish to use the dump file to restore on printer, you need to rename it with ".gcode" extension
;       ".txt" is kept to prevent acidental print and restore
;

M8512 "memory_dump.txt" ; Dump EEPROM settings to this file
