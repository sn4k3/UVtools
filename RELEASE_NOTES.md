- **Settings:**
   - (Add) Remove source file after automatic conversion (#444)
   - (Add) Remove source file after manual conversion (#444)
   - (Add) **Average resin bottle cost:** The average cost per one resin bottle of 1000ml.
           Used to calculate the material cost when the file lacks that information.
           Use 0 to disable this feature and only show the cost if file have that information.
           If this value is changed, you need to reload the current file to update the cost.
   - (Change) Move "Expand and show tool descriptions by default" to From `General` to `Tools` tab (Setting will reset to default)
- **File formats:**
   - (Add) Property `StartingMaterialMilliliters`: Gets the starting material milliliters when the file was loaded
   - (Add) Property `StartingMaterialCost`: Gets the starting material cost when the file was loaded
   - (Add) Property `MaterialMilliliterCost`: Gets the material cost per one milliliter
   - (Improvement) Update `MaterialCost` when `MaterialMilliliters` changes (#449)
- **Raft relief:**
   - (Add) Linked lines: Remove the raft, keep supports and link them with lines
   - (Improvement) Change the supports detection parameters to be more effective and precise on detect the starting layer
   - (Fix) Brightness percentage not getting updated
   - (Fix) Remove anti-aliased edges from Tabs
- (Improvement) Core: Minor clean-up
- (Fix) Issue repair error when 'Auto repair layers and issues on file load' is enabled (#446)
- (Fix) UI: Selecting a object with ROI when flip is verically, will cause 1px up-shift on the preview
- (Fix) macOS permission error due loss of attributes on the build script, WSL have bug with mv, using ln&rm instead

