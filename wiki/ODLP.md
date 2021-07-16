# Open source and universal binnary file format for mSLA/DLP (.odlp)

This file format aims for simplicity and universal file format for low-end printers that can't take
a ZIP + PNG slices approach due the incapacity of CPU to process such data schemes.

## Implementation requesites

- Open-source file format
- Document the changes and revisions, as well the reserved table if any
- Universal: the file and revisions should always be compatible with any firmware that take this file
- No encrypted files: The data should be plain and easy to access
- No property or changed variant of the file format should exists!
- Machine follows gcode commands

## Printer firmware checks (can print this file?)
1. Compare machine resolution with file resoltuion
2. Can use ImageDataType? For example, if processor is unable to process PNG images, trigger an error and dont allow to continue
3. Parse gcode and check for problems, such as, print on a position out of printer Z range

## Printer firmware layer data adquisiton

1. On gcode, when machine firmware detects M6054, it goes to the layer table at layer x index given by the command and gets the layer data to buffer the image


# Draft 1

```
#[HEADER]
Version=1 (ushort)
MachineZ=130 (float)
DisplayWidth=68.04 (float)
DisplayHeight=120.96 (float)
ResolutionX=1080 (uint)
ResolutionY=1080 (uint)
MirrorX=1 (byte) 0 = false | 1 = true
MirrorY=0 (byte) 0 = false | 1 = true
ImageDataType=PNG\0\0\0\0\0 (char[8]) compressed image data type, eg: RLE or PNG or JPG or BITMAP
ReservedBytes=4 (uint) Number reserved of bytes, this can be used for custom use of each brand, but not important for general file format
NumberOfPreviews=2 (byte) Number of previews/thumbnails on this file
LayerCount=1000 (uint) Total number of layers
LayerDefinitionsAddress=00000 (uint) Address for layer definition
GCodeAddress=000000 (uint) gcode text address
GCodeSize=sizeof(gcode) (uint) gcode text length
FileUpdateTime=1626451007 (uint) UNIX Timestamp
ModifiedBySize=sizeof(ModifiedBy) (uint)
ModifiedBy=UVtools v2.15.0 (text) program/slicer who last modified the file
MachineNameSize=sizeof(MachineName) (ushort)
MachineName=Phrozen Sonic Mini (string)

#[ReservedBytes]
# This can be used to store slicer specific settings or additional information
# Still both printer and software must not depend on this
ExampleOfCustomField=4 (uint)

#[Preview 1]
ResolutionX=400 (ushort)
ResolutionY=400 (ushort)
PreviewDataSize=sizeof(DATA) (uint)
RLE/PNG/JPG/BITMAP

#[Preview 2]
ResolutionX=400 (ushort)
ResolutionY=400 (ushort)
PreviewDataSize=sizeof(DATA) (uint)
RLE/PNG/JPG/BITMAP

#[LayerDefinitions]
#[Layer 1]
DataSize=sizeof(LAYER_DATA) (uint)
RLE/PNG/JPG/BITMAP

#[Layer 2]
DataSize=sizeof(LAYER_DATA) (uint)
RLE/PNG/JPG/BITMAP

[GCode] (text - dynamic size)
;START_GCODE_BEGIN
G21;Set units to be mm
G90;Absolute positioning
M106 S0;Turn LED OFF
M17;Enable motors
;<Slice> Blank
G28 Z0;Home Z
;END_GCODE_BEGIN

;LAYER_START:0
;PositionZ:0.05mm
M6054 0;Show image by layer index, to lookup on layer definition
G0 Z5.05 F60;Z Lift
G0 Z0.05 F150;Retract to layer height
G4 P10000;Stabilization delay
M106 S255;Turn LED ON
G4 P80000;Cure time/delay
M106 S0;Turn LED OFF
;<Slice> Blank
;LAYER_END

;START_GCODE_END
M106 S0;Turn LED OFF
G0 Z180 F150;Move Z
M18;Disable motors
;END_GCODE_END
;<Completed>
```


Notes:
1) Previews start address = header size + reserved bytes size
2) File header dont need much information, everything can be parsed from gcode!
