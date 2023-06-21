- (Add) File - Rename: Allow to rename the current file with a new name (Ctrl + F2)
- (Improvement) Tool - Edit print parameters: It now apply settings without close the window, allowing user to do continuous work. After all editing is done the user must manually close the window (#731)
- (Improvement) Resin traps and suction cups: Optimization of contour grouping will now make the detection faster if it contain a large number of contours
- (Change) Lower the default setting for binary threshold for resin traps, from 127 to 100
- (Fix) macOS: Unable to have settings on Monterey or above due the settings folder no longer exists on recent systems. (#728)
        Your current settings will not be automatically transferred to the new location, to do such please copy them over or use the following command before upgrade: `mv "$HOME/.local/share/UVtools" "$HOME/Library/Application Support"`
        If you already ran UVtools and would like to transfer old settings, use: `cp -Rf "$HOME/.local/share/UVtools/" "$HOME/Library/Application Support/UVtools/"`
- (Upgrade) .NET from 6.0.16 to 6.0.18

