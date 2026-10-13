- (Change) Use NuGet mini runtimes for Windows, macOS, Ubuntu 24.04 x64 and Debian 13 (Trixie) ARM64, with custom
  cvextern overrides from build/platforms when available.
- (Fix) Anycubic PWSZ: saved files could be rejected by Photon Workshop because optional resin names
  were added to legacy user resins; omit absent names and preserve existing values (#1155).
- (Upgrade) EmguCV to 5.0.0
- (Upgrade) AvaloniaUI from 12.1.3 to 12.1.4