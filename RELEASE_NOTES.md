- **Layers:**
  - (Add) Brotli compression codec with good performance and compression ratio (Choose it if you have low available RAM)
  - (Improvement) Use `ResizableMemory` instead of `MemoryStream` for `GZip` and `Deflate` compressions, this results in faster compressions and less memory pressure
  - (Improvement) Changed how layers are cached into the memory, before they were compressed and save for the whole image. Now it crops the bitmap to the bounding rectangle of the current layer and save only that portion with pixels.
                  For example, if the image is empty the cached size will be 0 bytes, and in a 12K image but with a 100 x 100 usable area it will save only that area instead of requiring a 12K buffer.
                  The size of the buffer is now dynamic and will depends on layer data, as so, this method can reduce the memory usage greatly, specially when using large images with a lot of empty space, but also boosts the overall performance by relief the allocations and the required memory footprint.
                  Only in few special cases can have drawbacks, however they are very minimal and the performance impact is minimal in that case.
                  When decompressing, the full resolution image is still created and then the cached area is imported to the corresponding position, composing the final and original image. This is still faster than the old method because decompress a larger buffer is more costly.
                  In the end both writes/compresses and reads/decompresses are now faster and using less memory.
                  Note: When printing multiple objects it is recommended to place them close to each other as you can to take better advantage of this new method.
- **Issues Detection:**
  - (Fix) When detecting for Islands but not overhangs it will throw an exception about invalid roi
  - (Fix) Huge memory leak when detecting resin traps (#830)
- (Improvement) Core: Changed the way "Roi" method is returned and try to dispose all it instances
- (Fix) EncryptedCTB, GOO, SVGX: Huge memory leak when decoding files that caused the program to crash (#830)
- (Fix) UI: Missing theme styles
- (Fix) PrusaSlicer profiles for Creality Halot Mage's: Enable the "Horizontal" mirror under the "Printer" tab to produce the correct orientation when printing (#827)

