- (Add) Zstd/Zstandard layer compression
- (Add) UI Scaling setting to allow to scale the UI to your preference
- (Change) Default compressor from `Brotli` to `Deflate`
- (Improvement) Move the `EmguCV` extensions and classes to a separate library `EmguExtensions` and rewrite most of it,
  providing a boost in performance and fixing bugs: #1118, #1119
- (Improvement) Better memory management when compressing layers
- (Improvement) Use `OptimalMaxDegreeOfParallelism` setting for `MaxDegreeOfParallelism` by default at core, this
  affects `UVtoolsCmd` and libraries that might use `UVtools.Core`
- (Improvement) Allow to set array of strings via reflection, this simplifies the use of `UVtoolsCmd` (#1127)
- (Improvement) Migrate `BindableBase` to `ObservableObject` from `CommunityToolkit.Mvvm`
- (Improvement) Migrate to `Nuke` builds and publish
- (Breaking change) As `EmguCV` extensions moved to a separate library, if you are using them in your own code/scripts
  you need to adapt and use the new naming and reference the new library
- (Fix) Anycubic zip format after save can not be loaded again in anycubic slicer (#1108)
- (Fix) Goo: Disallow open files with a version that is not known by decoder (#1114)
- (Fix) Calibrate - Exposure Finder: NullReferenceException when loading profiles (#1109)
- (Upgrade) .NET from 10.0.5 to 10.0.9
- (Upgrade) AvaloniaUI from 11.3.13 to 12.0.5
