- **PCB Exposure:**
   - (Add) Parse of deprecated commands (G70, G71, G90, G91)
   - (Fix) Able to have parameterless macro apertures (#503)
- **UI:**
   - (Add) Menu -> File -> Free unused RAM: Force the garbage collection of all unused objects within the program to free unused memory (RAM).
                                            It's never required for the end user run this. The program will automatically take care of it when required.
                                            This function is for debug purposes.
   - (Improvement) Window title bar: Show elapsed minutes and seconds instead of total seconds minutes and second
- (Fix) Tool - Mask: Loaded image resolution shows as (unloaded)
- (Fix) Applying a large set of modifications in layer depth with pixel editor cause huge memory spike due layer aggregation without disposing, leading to program crash on most cases where RAM is insufficient (#506)
- (Upgrade) AvaloniaUI from 0.10.15 to 0.10.16
- (Upgrade) .NET from 6.0.6 to 6.0.7
