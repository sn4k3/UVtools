- (Add) Linux native packages for .deb, .rpm and .pkg.tar.zst and arm64 packages (maybe fix #1124)
- (Improvement) Rewrite the installers, use more keys on registry and registers the known file extensions to make double
  click on files to prompt for open with UVtools, it also allow to select it as default program (maybe fix #459)
- (Improvement) pixel editor drawing with smooth, interpolated strokes committed on pointer release by @jorgerobles
  (#1139)
- (Improvement) Rewrite the installer scripts, add windows install script and uninstall scripts
- (Improvement) Avoid eager pixel lists during island detection by @apullin (#1142)
- (Improvement) UI: Add `RelayCommand` attributes to various methods for improved command binding
- (Fix) Terminal error when sending a command (#1138)
- (Fix) 'Edit Print Paramters' menu item not visible in Tools (#1106)
- (Fix) empty-layer classification in linear time by @apullin (#1145)
- (Fix) Object reference not set to an instance of an object after clicking File -> Reload (#1141)
- (Fix) AdvancedImageBox: owned-image disposal, selection clamping, crop stride/alpha handling, zoom bounds, redraw
  invalidation, panning cursor reuse, and selection start bounds
- (Fix) Disposed generated tracker and ROI crop bitmaps
- (Breaking) AdvancedImageBox: `ZoomLevelCollection` now exposes `ICollection<int>` rather than positional
  `IList<int>` operations, because zoom levels are sorted and unique.
- (Upgrade) .NET from 10.0.10 to 10.0.11
- (Upgrade) AvaloniaUI from 12.1.1 to 12.1.2