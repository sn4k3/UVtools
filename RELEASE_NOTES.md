- **Tools:**
   - (Add) PCB exposure: Converts a gerber file to a pixel perfect image given your printer LCD/resolution to exposure the copper traces.
   - (Improvement) Export settings now indent the XML to be more user friendly to edit
   - (Improvement) Layer import: Allow to have profiles
   - (Improvement) Layer import: Validates if selected files exists before execute
   - (Fix) Lithophane: Disallow having start threshold equal to end threshold
- (Add) Windows explorer: Right-click on files will show "Open with UVtools" on context menu which opens the selected file on UVtools (Windows MSI only)
- (Improvement) Island and overhang detection: Ignore detection on all layers that are in direct contact with the plate (On same first layer position)
- (Improvement) Cmd: Better error messages for convert command when using shared extensions and no extension

