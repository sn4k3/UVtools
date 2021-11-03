; UVtools Acceleration Guide Recommendation (Template profile)
; https://github.com/sn4k3/UVtools/wiki/Rest-times-and-TSMC
;
; Changelog:
; Version 3: (1/1/10/4/NOTSMC) @ 03/11/2021
; Version 2: (1/1/400/5) @ 02/11/2021
; Version 1: (0.7/0.4/400/5) @ 28/08/2021
; 
; Instructions:
; 1. Put this and the "uvtools_dump_eeprom.gcode" files in the printer USB
; 2. Print the "uvtools_dump_eeprom.gcode" to store the old configuration on USB (Optional but safer!)
; 3. Print this file
; 4. Reboot the printer to fetch the new configuration
; 

M8006 I1   ; Maximum starting speed mm/s     (60mm/min) [Should be 0.7 but firmware constrains anything lower than 1]
M8007 I1   ; Jerk mm/s                       (60mm/min) [Should be 0.4 but firmware constrains anything lower than 1]
M8008 I10  ; Acceleration mm/s2
M8013 I4   ; Maximum speed of Z motion mm/s  (240mm/min)
M8016 I1   ; Second home speed: Decreasing the second home speed improve the positioning accuracy of the home limit
M8070 S0.0 ; Disable the slow rising         (Disable in firmware TSMC)
M8021 S0.0 ; Disable the slow retract        (Disable in firmware TSMC)
M8015 T4   ; Slow maximum limit rise speed   (240mm/min)
M8016 T4   ; Fast maximum limit fall speed   (240mm/min)
M8015 P4.0 ; Slow maximum limit rise speed   (240mm/min)
M8028 S4.0 ; Fast maximum limit rise speed   (240mm/min)
M8020 S4.0 ; Slow maximum limit fall speed   (240mm/min)
M8016 P4.0 ; Fast maximum limit fall speed   (240mm/min)


M8500      ; Save configuration permanently
