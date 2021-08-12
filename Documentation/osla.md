# Open source and universal binary file format for mSLA/DLP (.osla)

This file format aims for simplicity, versatility, cleanest as possible and universal format for low-end printers that can't handle
a ZIP + PNG slices approach due the incapacity of CPU to process such data schemes. 

## File extensions

This file format share and reserve the following file extensions:
- .osla (Open SLA)
- .odlp (Open DLP)
- .omsla (Open mSLA)

## Implementation requesites

- Open-source file format
- Document the changes and revisions, as well the reserved table if any
- Universal: the file and revisions should always be compatible with any firmware that take this file
- No encrypted files: The data should be plain and easy to access
- No property or changed variant of the file format should exists!
- Machine follows sequential gcode commands or in alternative follows the layer def table
- Endianness: Little-Endian (LE)

## Printer - Firmware checks (can print this file?)

1. Marker == "OSLATiCo" - This is a double check if file is really a .osla beside it extension, case sensitive compare.
1. Compare machine resolution with file resoltuion.
2. Can use ImageDataType? For example, if processor is unable to process PNG images, trigger an error and dont allow to continue.
3. Parse gcode/layer def and check for problems, such as, print on a position out of printer Z range.

## Printer - Print sequence

1. Home
2. Go to next layer `PositionZ`
3. Display the layer image
3. Lift sequence (If `SUM(LiftHeight + LiftHeight2)` > 0mm)
   1. `LiftHeight` @ `LiftSpeed` (if > 0mm)
   2. `LiftHeight2` @ `LiftSpeed2` (if > 0mm)
   3. `WaitTimeAfterLift` (if > 0s)
   4. `RetractHeight2` @ `RetractSpeed2` (if > 0mm)
   5. Retract to layer `PositionZ` at `RetractSpeed`
4. Wait `WaitTimeBeforeCure`
6. Turn on LED @ `LightPWM`
7. Wait `ExposureTime`
8. Turn off LED
9. Wait `WaitTimeAfterCure`
10. Repeat from 2.

## Printer - Firmware layer data adquisiton (GCode)

1. Machine firmware detects `M6054 i` gcode command.
2. Goes to the layer definition table at the `i` index given by the `M6054` command.
3. Gets the image address and jumps to it.
4. Start to read and buffer the image to the LCD.
5. Jump back to the resume position and continue to execute the gcode.

**Note:** Printer only continue to execute the next gcode command after the image is displayed/buffered. 
When using a sencond board to display the image, it must send a OK back in order to resume the gcode execution.
But a custom flag or M code command should exist to buffer the image in the background, eg: M6055.

## Software - Data adquisiton (GCode)

For slicers and other programs, data and per layer settings should be parsed from gcode.  
For example, take UVtools [GCodeBuilder.cs](https://github.com/sn4k3/UVtools/blob/master/UVtools.Core/GCode/GCodeBuilder.cs) as example and search for `ParseLayersFromGCode` method

## In file optimizations

1. When generating the file, layers that share the same image data, may reuse that data instead of duplicate the image. 
This allow to spare a good amount of data when file contains multiple layers that share same image over and over, for example, functional parts.
See example on file sample bellow at `[Layer 3]`.  
While this is optional and either way it must be valid to print, is highly recommended to hash the layers.

## Image data type

1. This file format allows common and custom image data types, such as: 
   - PNG
   - JPG/JPEG
   - TIF/TIFF
   - BMP
   - SVG
   - LINE-XXY
   - LINE-XYY
   - RGB555
   - RGB565
   - RGB888
   - BGR555
   - BGR565
   - BGR888
   - RLE-XXXX
   - Custom
2. Having different data types for previews and layers are allowed.
eg: `PreviewDataType=RGB565` and `LayerDataType=PNG`
3. The only requesite is to correct define the type of image data on the [Header] and respect that.
4. Custom image data types are possible, you must take a unique non conflicting ID but the documentation how to read and write are required in order to use a custom type. 
5. If you want to apply and register your data type ID to be globaly used on firmwares, you must send us the draft for approval and get that ID reserved for the applied data scheme.

## Previews

1. Able to save 0 or more previews
2. Previews must all use same image data type
3. Previews must be sorted from bigger to smaller

## Layers

1. Layer table must contain a copy of the used values even if running from gcode
2. If gcode is not present (`GCodeAddress=0` or `GCodeSize=0`) or machine not able to follow gcode, the layer table must be followed instead
3. A slicer can skip gcode generation for a printer with `GCodeAddress=0` and printer will follow the layer table instead

### Rules: 

* All printers must implement and know how to print by layer table definitions. 
If printer able to gcode, that can be done by issue the gcode equivalent commands while going
* If is able to print with gcode, and if present, gcode is preferred
* If able to print with gcode, but if not present, layer table definitions must be used to print

## Offsets

- Preview n = `sizeof(File) + sizeof(Header) + sizeof(CustomTable) + n * PreviewTableSize + SUM(sizeof(..n PreviewDataSize))`
- Layer n = `LayerDefinitionsAddress + n * LayerTableSize`

# Draft 1

## Structure

1. [File] (150 bytes)
2. [Header] (199 bytes)
3. [Custom table] (4 bytes + 0 or more bytes)
4. [Previews] (0 or more)
   - Preview 1 (8 bytes + sizeof(preview image data))
   - Preview 2 (8 bytes + sizeof(preview image data))
   - Preview n (8 bytes + sizeof(preview image data))
5. [Layer definitions]
   - Layer 0 (69 bytes)
   - Layer 1 (69 bytes)
6. [Layer image data]
   - Layer 0 data (n bytes) 
   - Layer 1 data (n bytes) 
7. [GCode] (4 bytes + sizeof(gcode text))


```ini
[File]
Marker=OSLATiCo (char[8]) Extra validation beside file extension, constant
Version=1 (ushort) File format revision
CreatedDateTime=2021-07-10 10:00:00Z (char[20] / DateTime) Universal sortable date and time string, as defined by ISO 8601 (yyyy-MM-dd HH:mm:ssZ)
CreatedBy=UVtools v2.14.0\0\0\0\0\0\0\0.. (char[50] fixed!) program/slicer who created the file (Set once)
ModifiedDateTime=2021-08-10 12:00:00Z (char[20] / DateTime) Universal sortable date and time string, as defined by ISO 8601 (yyyy-MM-dd HH:mm:ssZ)
ModifiedBy=UVtools v2.15.0\0\0\0\0\0\0\0.. (char[50] fixed!) program/slicer who last modified the file

[Header]
HeaderTableSize=sizeof(header) (uint), excluding this field
ResolutionX=1080 (uint)
ResolutionY=1080 (uint)
MachineZ=130.00 (float)
DisplayWidth=68.04 (float)
DisplayHeight=120.96 (float)
DisplayMirror=0 (byte) 0 = No mirror | 1 = Horizontally | 2 = Vertically | 3 = Horizontally+Vertically | >3 = No mirror
PreviewDataType=RGB565\0\0\0\0\0\0\0\0\0\0 (char[16]) compressed image data type, eg: RLE-XXX or RGB565 or PNG or JPG or BMP OR other?
LayerDataType=PNG\0\0\0\0\0\0\0\0\0\0\0\0\0 (char[16]) compressed image data type, eg: RLE-XXX or PNG or JPG or BMP OR other?
PreviewTableSize=8 (uint), Size of each preview table
PreviewCount=2 (byte) Number of previews/thumbnails on this file
LayerHeight=0.05 (float) Layer height in mm
BottomLayerCount=4 (ushort) Total number of bottom/burn layers
LayerCount=1000 (uint) Total number of layers
LayerTableSize=69 (uint), Size of each layer table
LayerDefinitionsAddress=00000 (uint) Address for layer definition start
GCodeAddress=000000 (uint) Address for gcode definition start
PrintTime=12345 (uint) Print time in seconds
MaterialMilliliters=36.50 (float) Material used in milliliters
MaterialCost=3.40 (float) Material cost in currency
MaterialName=Generic Crystal Clear Water Washable @ 100um\0\0\0\0\0\0 (char[50])
MachineName=Super Advanted 1000K Ultimate Edition\0\0\0... (char[50])
# HEADER ends here

[CustomTable]
CustomTableSize=4 (uint) Number reserved of bytes for a custom table, this can be used for custom use of each brand, but not important for general file format
# This can be used to store slicer specific settings or additional information
# Still both printer and software must not depend on this
ExampleOfCustomField=1001 (uint)

[Preview 1]
ResolutionX=400 (ushort)
ResolutionY=400 (ushort)
PreviewDataSize=sizeof(DATA) (uint)
RLE/RGB16/PNG/JPG/BMP

[Preview 2]
ResolutionX=400 (ushort)
ResolutionY=800 (ushort)
PreviewDataSize=sizeof(DATA) (uint)
RLE/RGB16/PNG/JPG/BMP

[LayerDefinitions]
[Layer 1]
DataAddress=0000            (uint)
PositionZ=0.05              (float) Z height of this layer in mm
LiftHeight=5                (float) Distance in mm to raise from PositionZ
LiftSpeed=100               (float) Speed in mm/min
LiftHeight2=0               (float) Extra distance in mm to raise from current position
LiftSpeed2=50               (float) Speed in mm/min
WaitTimeAfterLift=0         (float) Time to wait in seconds after lift / before retract
RetractSpeed=100            (float) Speed in mm/min
RetractHeight2=0            (float) Offset retract distance in mm to perform
RetractSpeed2=150           (float) Speed in mm/min
WaitTimeBeforeCure=2.5      (float) Time to wait in seconds before cure the layer / turn on LED
ExposureTime=2.8            (float) Time in seconds while curing the layer
WaitTimeAfterCure=1         (float) Time to wait in seconds after cure the layer / turn off LED
LightPWM=255                (byte)  PWM value of the light source
BoundingRectangleX=300      (uint)  Start X position where first pixels are [Optional, can be 0]
BoundingRectangleX=500      (uint)  Start Y position where first pixels are [Optional, can be 0]
BoundingRectangleWidth=500  (uint)  Rectangle width [Optional, can be 0]
BoundingRectangleHeight=500 (uint)  Rectangle height [Optional, can be 0]
[Layer 2]
DataAddress=1111 (uint)
...
[Layer 3]
DataAddress=1111 (uint) (Identical layers can point to the same data if they share the same image, sparing space on file)
...

DataSize=sizeof(RLE) (uint)
RLE/PNG/JPG/BMP of layer 1
DataSize=sizeof(RLE) (uint)
RLE/PNG/JPG/BMP of layer 2

[GCode]
GCodeSize=sizeof(gcode) (uint) gcode text length
# Following by raw text
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

https://github.com/sn4k3/UVtools/blob/master/Scripts/010%20Editor/osla.bt