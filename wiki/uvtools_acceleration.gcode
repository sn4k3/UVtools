; UVtools Acceleration Guide Recommendation (Template profile)
; https://github.com/sn4k3/UVtools/wiki/Rest-times-and-TSMC
;
; Changelog:
; Version 2: (1/1/400/5) @ 02/11/2021
; Version 1: (0.7/0.4/400/5) @ 28/08/2021
; 
; Instructions:
; 1. Put this and the "uvtools_dump_eeprom.gcode" files in the printer USB
; 2. Print the "uvtools_dump_eeprom.gcode" to store the old configuration on USB (Optional but safer!)
; 2. Print this file
; 3. Reboot the printer to fetch the new configuration
; 

;
; M8006 I0.7 ; Maximum starting speed mm/s
; M8007 I0.4 ; Jerk mm/s
; It seens the float numbers are not allowed and firmware constrains anything lower than 1 and set
; to a default value, given that, i will default to I1, but keep in mind it should be a bit lower like above...
;

M8006 I1   ; Maximum starting speed mm/s
M8007 I1   ; Jerk mm/s
M8008 I400 ; Acceleration mm/s2
M8013 I5   ; Maximum speed of Z motion mm/s
M8500      ; Save configuration permanently
