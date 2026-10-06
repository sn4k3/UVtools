# Changelog

## 06/10/2026 - v7.0.2

- **File formats:**
  - (Improvement) Encoding is up to 10x faster for GR1 and MDLP, 2x for PWS, and 25-35% faster for cbddlp, N4/N7, and
    Photon S; decoding PWS, cbddlp, FDG, and PHZ is 25-45% faster. Runs are found with vectorized searches, and the
    duplicated CTB, anti-aliasing plane, and column line codecs are now shared
  - (Improvement) CTB, PHZ, and FDG layer encryption is processed 4 bytes at a time, and the CTB encrypted encoder
    writes layers in batches instead of keeping all of them in memory
  - (Improvement) Thumbnail encoding and decoding no longer compares strings or queries the channel count per pixel
  - (Improvement) QDT and SVGX decoding parse without allocating a string per token, SVGX decoding no longer searches
    the layer group linearly, and its encoding no longer creates a native wrapper per contour point
  - (Improvement) Truncated CTB RLE data now fails with a corrupted data error instead of an index out of range
  - (Fix) Anet: the last pixel of the layer was lost when it differed from the previous one, making the file unreadable
  - (Fix) OSF: encoding failed when the layer content touched the last row of the image, `Layer.LastPixelIndex` pointed
    one row below the last pixel
  - (Fix) cbddlp: layers with anti-aliasing 1 turned every gray pixel black, and identical layers did not copy the page
    number
  - (Fix) OSLA: the preview table padding used the layer table size
  - (Fix) Encoding layers to a zip with the cache enabled skipped the duplicated layers
  - (Fix) The hash helpers shared non-thread safe instances, and the AES padding bytes were not zeroed
  - (Fix) ZCode: wrong exception arguments for a too long G-code line; QDT: swapped resolution names on header errors
- (Fix) Crash `Cannot access a disposed object` on `Image.MeasureOverride` (#1150): the thumbnail and tool preview
  bitmaps are now disposed after the UI applied the new image, and the PCB exposure preview no longer leaks the previous
  bitmap
- (Fix) Error when executing UVtooldCmd run with a script (#1138)
- (Fix) `LayerCache` disposed the wrong `SkiaSurface` when the layer was changed, and did not release the framebuffer
- (Fix) Zstd: the fastest compression level was not really the fastest
- (Improvement) Update `NativeCompressions` and use it LZ4 codec
- (Update) Benchmark 13900K reference values
- (Drop) `K4os.Compression.LZ4` library

## 02/10/2026 - v7.0.1

- (Improvement) Add a Windows renderer preference in 3D preview settings to prefer native OpenGL (WGL) after restarting
  UVtools; ANGLE stays the default, with WGL tried before software rendering (#1152, #1153)
- (Add) Pixel Arithmetic options to apply changes only to model top surfaces, with an optional margin from walls (#1151)
- (Improvement) Pixel editor: applying supports and drain holes is much faster, all of them are now drawn in a single
  pass that decodes and encodes each layer only once, in parallel
- (Fix) Pixel editor: multiple supports or drain holes on the same layer no longer share the drawn layers counter, which
  made their radius grow faster than intended
- (Improvement) Issues detection is up to 5x faster for islands/overhangs and about 2x faster for resin traps:
  - Layers are decoded only for the area needed instead of the whole resolution
  - Islands use a run-length component labeling, and layers whose pixels are all supported skip it
  - Overhangs only erode and trace the area that differs from the previous layer
  - Resin traps air map is updated in a single multi-threaded SIMD pass, with layers decoded ahead while processing
  - Touching bounds only decodes the layer when it is actually close to a border
- (Fix) Issues detection: partially supported islands were always discarded when the overhang check was not computed for
  the layer (overhang detection disabled or layer outside its white list), after the enhanced detection
- (Fix) Issues detection: touching bounds skipped the first rows of the layer content beside the left/right borders
- (Fix) Issues detection: native contour vectors were not released when the detection was cancelled
- (Improvement) Repair layers: gap closing and noise removal only process the area around the layer content, empty
  layers
  are skipped, and layers that the repair did not change are no longer rewritten
- **3D layer preview:**
  - (Improvement) Building the model is up to 5x faster (detailed quality on a 4K file went from 9.3s to 1.7s), the
    faces are found with vectorized row by row passes and only the area of the model is decoded from each layer
  - (Improvement) Building the issues overlay is up to 10x faster, the layers are rasterized in parallel
  - (Improvement) Moving the clip slider decodes less per layer to rebuild the cut cross-section
  - (Improvement) Exporting the mesh to STL/OBJ no longer freezes the window
  - (Fix) Export clipped mesh to OBJ exported the whole mesh, and the clipped STL lost the walls crossing the clip and
    was left open on the cut; it now meshes only the shown layers, closed, and respects the cutaway
  - (Fix) Model volume, weight and resin cost were overestimated on a coarse detail, they now come from the layer pixels
  - (Fix) Base contact area was overestimated on a coarse detail, and a floating model was reported as touching the
    plate
  - (Fix) Clicking the model while clipped could pick the hidden part of a tall wall instead of the visible surface
  - (Fix) Parts of the model were cut or showed stripes depending on the camera angle: the framebuffer provided by
    Avalonia only has a 16 bit depth buffer, too coarse for faces a fraction of a millimeter apart. The scene is now
    drawn into a 24 bit depth framebuffer and copied to the control
  - (Fix) The camera near/far planes are now fitted to the model bounds from the camera point of view, they were
    estimated from a bounding sphere around the orbit target, which clipped the model or made its faces fight each other
    (cuts and stripes) when the camera was panned, focused on an issue, zoomed in, or the model was rebuilt
  - (Fix) Snapshot bitmaps were never released
- **Operations:**
  - (Improvement) Most tools skip empty layers and no longer clone every layer when there is no mask to apply
  - (Improvement) Morph, Box/Median blur, Solidify, and Pixel dimming only process the area around the layer content,
    Solidify and Repair layers also rewrite only the layers they changed
  - (Improvement) Infill: gyroid pattern is computed from per-axis tables instead of per pixel, the neighbor layers used
    to find the floor and ceil are only decoded where needed, and the search stops once nothing is left to infill
  - (Improvement) Raft relief traces the raft layers in parallel, Heat map and Skeleton exports accumulate in parallel,
    Mesh export skips the empty pixels faster, HTML export traces only the layer area, Remove layers and Pattern are
    faster, Change resolution only decodes the model area
  - (Improvement) Calibration tolerance shares the identical layers instead of allocating one per layer, which used to
    take gigabytes of memory on every parameter change of the preview
  - (Improvement) Pixel arithmetic skips empty layers and only clones the layer when it has to restore pixels
  - (Improvement) Layer import skips merging empty images, and fails with a clear message on unreadable images
  - (Improvement) Progress notifications from parallel operations are throttled, to not flood the UI
  - (Fix) Blur: stack blur gave random pixels when layers were blurred in parallel (the OpenCV implementation is not
    thread safe) and misbehaved when using a ROI; pyramid blur had no effect at all
  - (Fix) Infill: using a mask without a ROI always failed, and the mask is now the area where the infill happens
  - (Fix) Layer arithmetic and Heat map export failed or used a wrong mask when a ROI and a mask were used together
  - (Fix) Operations mask application when the layer, ROI, and mask have different sizes
  - (Fix) Redraw model: checked the wrong layer to skip empty ones, never released the redraw file, and did not complete
    the progress
  - (Fix) Raft relief: tabs on the left and right sides were limited by the layer width instead of the height
  - (Fix) Layer import, Lithophane, and Edit parameters did not complete the progress when layers were skipped
  - (Fix) Grayscale and Elephant foot calibrations hanged or threw with a step of 0, Elephant foot threw when the start
    was higher than the end, Lift height, Stress tower, and Exposure finder threw or looped forever with invalid
    divisors/steps (decrease factor, layer height, spirals, layer height step)
  - (Fix) Dynamic layer height: repeated the same erosion up to the maximum number of times for nothing
  - (Fix) Calibrate XYZ accuracy drain hole was centered using the object width instead of the height, and Blooming
    effect text was added for objects that were not created

## 19/09/2026 - v7.0.0

- **AdvancedImageBox**
  - (Add) `PanBoundsMode` property to choose the pan bounds mode, if `Padding` is selected it will add that padding to
    scroll content bounds (#959)
    disabled
  - (Fix) Owned-image disposal, selection clamping, crop stride/alpha handling, zoom bounds, redraw invalidation,
    panning cursor reuse, and selection start bounds
  - (Breaking) `ZoomLevelCollection` now exposes `ICollection<int>` rather than positional
    `IList<int>` operations, because zoom levels are sorted and unique.
  - (Improvement) Rewrite the installer scripts, add windows install script, and uninstall scripts
  - (Improvement) Remove `e.Handled = true;` from handlers to allow subscribers to handle the event in same conditions
- **Packaging:**
  - (Add) Linux native packages for .deb, .rpm and .pkg.tar.zst and arm64 packages (maybe fix #1124)
  - (Improvement) Rewrite the installers, use more keys on registry, and registers the known file extensions to make
    double click on files to prompt for open with UVtools, it also allow to select it as default program (maybe fix
    #459)
- **3D layer preview:** GPU-accelerated, rotatable 3D model generated from the layer stack (#38, #602)
  - (Add) Solid, X-Ray, and Wireframe render modes, with Camera/Studio/Flat lighting and configurable voxel/background
    colors
  - (Add) Color modes: overhang heatmap, layer-height zones, peel force & area risk, and bed adhesion footprint
  - (Add) Clip modes (top/bottom/slab, with adjustable slab thickness) and X/Y cutaway with an invertible slider, to
    inspect the model's interior
  - (Add) Model dimensions, bounding box, volume/weight/resin cost, center of mass & first-layer contact stability, and
    cross-section area/peel-force curve HUD overlays, each independently toggleable
  - (Add) Per-type issue highlighting in 3D, with next/prev navigation and in-place solidify/drill repair
  - (Add) Measure tool: click two points on the model to get the distance between them
  - (Add) Orbit cube navigation (faces/edges/corners, roll, turntable/auto-rotate, home, fit-to-view), full keyboard
    shortcuts, and click-to-select-layer
  - (Add) Print simulation playback with adjustable speed
  - (Add) Export: mesh to STL/OBJ (full or clipped), current cross-section to PNG, snapshot to clipboard or file, and
    360° turntable animation to GIF
- **2D layer preview:**
  - (Add) Elapsed print time readout
  - (Add) Measure tool: click two points on the model to get the distance between them
  - (Add) Cross-section area/peel-force curve HUD overlay
  - (Add) Setting to start layer number at 1 instead of 0 on layer slider and navigation panel (#948)
- (Add) Dynamic lifts: Configure separate bottom/normal retract speeds and two-stage retract distance percentages (#927)
- (Add) Configure bottom height and transition layers for wait time after cure (#1093)
- (Add) Panel Gamma setting to compensate Exposure Time calibration tool in Multiple Brightness / grayscale mode (#1104)
- (Fix) Align pixel editor line brush preview and applied position (#966)
- (Add) Diagnostics tab to About window
- (Add) Announcements manager and live messages
- (Improvement) UI safety: Guaranteed GUI re-enabling via `try / finally` across file operations, exports, suggestions,
  and send-to actions
- (Improvement) Optimize issues collection view caching and selection lookups in issues DataGrid
- (Fix) Prevent SkiaSurface and framebuffer leaks during pixel editor preview drawing in `LayerCache`
- (Fix) Dispose previous thumbnail bitmaps, native OpenCV Mat instances, and ensure explicit disposal of timers and
  cache on window close
- (Improvement) pixel editor drawing with smooth, interpolated strokes committed on pointer release by @jorgerobles
  (#1139)
- (Improvement) Avoid eager pixel lists during island detection by @apullin (#1142)
- (Improvement) UI: Add `RelayCommand` attributes to various methods for improved command binding
- (Improvement) Redetect issues when ran operations: Rotate, Flip, Lithophane, LayerImport (#879)
- (Fix) Terminal error when sending a command (#1138)
- (Fix) 'Edit Print Paramters' menu item not visible in Tools (#1106)
- (Fix) empty-layer classification in linear time by @apullin (#1145)
- (Fix) Object reference not set to an instance of an object after clicking File -> Reload (#1141)
- (Fix) Disposed generated tracker and ROI crop bitmaps
- (Fix) UVtoolsCmd: it always return 1 exit code
- (Fix) SL1 layer height round trips by @apullin (#1147)
- (Fix) Culture-invariant serialization and parsing for SL1, CWS, and JXS config files and reflection extensions
- (Fix) Clear stale detected issues after layer image changes, undo, and redo (#879)
- (Fix) Undo/redo failing to revert differential layer edits in ClipboardManager
- (Fix) MessageBox from bug report and update show repeated header icon
- (Fix) Prevent SkiaSurface and framebuffer leaks during pixel editor preview drawing in `LayerCache`
- (Fix) Dispose previous thumbnail bitmaps and ensure explicit disposal of timers and cache on window close
- (Upgrade) .NET from 10.0.10 to 10.0.12
- (Upgrade) AvaloniaUI from 12.1.1 to 12.1.2

## 06/08/2026 - v6.2.0

- **Layer repair:**
  - Fixed single-pass island and suction-cup re-detection.
  - Fixed the inclusive layer range so the final selected layer is processed.
  - Restricted repairs and island detection to the selected range.
  - Removed the early return that skipped morphology and empty-layer removal.
  - Indexed issues by layer instead of repeatedly scanning all issues.
  - Added deterministic Mat disposal and exception-safe attachment locking.
  - Fixed attachment of early layers and prevented source-layer modification.
