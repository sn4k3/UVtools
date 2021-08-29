; UVtools Acceleration Guide Recommendation (Template profile)
; https://github.com/sn4k3/UVtools/wiki/Rest-times-and-TSMC
;
; Changelog:
; Version 1: (0.7/0.4/400/5) @ 28/08/2021
; 
; Instructions:
; 1. Put this file in the printer USB
; 2. Print this file
; 3. Reboot the printer to fetch the new configuration
; 


M8006 I0.7 ; Maximum starting speed mm/s
M8007 I0.4 ; Jerk mm/s
M8008 I400 ; Acceleration mm/s2
M8013 I5   ; Maximum speed of Z motion mm/s
M8500      ; Save configuration permanently
