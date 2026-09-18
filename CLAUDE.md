# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Follow the repository-wide agent instructions in `AGENTS.md` in addition to this file.

## Project Overview

UVtools is a cross-platform (Windows, Linux, macOS) MSLA/DLP resin printer file analysis, calibration, repair,
conversion, and manipulation application. It supports 35+ proprietary file formats (CTB, SL1, GOO, PWS, etc.) and
provides both a desktop GUI and a CLI.

## Build, Run & Test

The solution file is `UVtools.slnx` (XML solution format — there is no `.sln`).

```bash
dotnet build                                   # whole solution
dotnet build -c Release
dotnet run --project UVtools.UI                # desktop GUI (assembly name: UVtools)
dotnet run --project UVtools.Cmd -- <args>     # CLI (assembly name: UVtoolsCmd)

dotnet test tests/UVtools.Tests                                   # all tests (xUnit)
dotnet test tests/UVtools.Tests --filter "FullyQualifiedName~SL1RoundTrip"   # single class/test
```

Notes:

- `nuget.config` clears inherited sources and adds the **Avalonia nightly feed** — restore fails without it (SukiUI and
  Avalonia 12 preview packages come from there).
- Assemblies are signed with `UVtools.snk` **at the repo root** (`Directory.Build.props` → `AssemblyOriginatorKeyFile`).
  This file must be present to build.
- Build output goes to `artifacts/` at the repo root (`ArtifactsPath`).
- `build.ps1` / `build.sh` / `build.cmd` bootstrap the SDK and run `build/build.csproj`, a Fallout/StageKit build
  (`build/Build.cs`, default target `Compile`). That project is what produces the real release artifacts: portable zip,
  Windows installer, AppImage, deb, rpm, Arch package, macOS app bundle, plus file associations derived from
  `FileFormat.AllFileExtensions`. For day-to-day work plain `dotnet build` is enough; `build/createRelease.(ps1|sh)`
  packs a release for one runtime. See `build/README.md`, including the `cvextern`/`libcvextern` native-library notes.

## Projects

| Project                            | Purpose                                                      |
|------------------------------------|--------------------------------------------------------------|
| `UVtools.Core`                     | Core library: file formats, layers, operations, image ops     |
| `UVtools.UI`                       | Avalonia desktop GUI (references Core **and** Cmd)            |
| `UVtools.Cmd`                      | CLI built on `System.CommandLine`                             |
| `UVtools.AvaloniaControls`         | Reusable Avalonia controls, published as its own NuGet package |
| `UVtools.WixInstaller`             | WiX MSI installer for Windows                                 |
| `Scripts/UVtools.ScriptSample`     | Reference project for the built-in C# scripting engine        |
| `tests/UVtools.Tests`              | xUnit tests against `UVtools.Core`                            |

`Directory.Build.props` centralizes everything global: `net10.0`, nullable enabled, platforms `AnyCPU;x64;ARM64`,
signing, artifact paths, and the two version knobs `<UVtoolsVersion>` and `<AvaloniaVersion>`. There is no
`Directory.Packages.props`; package versions live in each `.csproj`. `UVtools.AvaloniaControls` is the exception to the
single TFM — it multi-targets `net8.0;net9.0;net10.0` and carries its own version and `.snk`. `UVtools.UI` uses
`LangVersion=preview`.

`UVtools.Installer/` is a leftover directory with no project file — the live installer project is `UVtools.WixInstaller`.

## Architecture

### `FileFormat` — `UVtools.Core/FileFormats/FileFormat.cs`

Abstract base for every supported printer file, and the largest/most central type in the codebase (~7k lines). It
implements `IList<Layer>` and owns the decode/encode lifecycle, print settings and per-layer overrides, thumbnails,
GCode, and cross-format conversion. Every format class (`ChituboxFile`, `GooFile`, `SL1File`, `AnycubicFile`, …) derives
from it and is registered in the static `FileFormat.AvailableFormats` array; extension lookup, file-type filters, and
installer file associations are all derived from that array via `FileExtension`. To add a format: create the class in
`UVtools.Core/FileFormats/`, implement at minimum `DecodeInternally`, `EncodeInternally`, `FileExtensions`, and the
format's header/layer structures, then add it to `AvailableFormats`.

`Scripts/010 Editor/*.bt` holds binary templates for most formats — useful when reverse-engineering or verifying a
header layout.

### `Layer` — `UVtools.Core/Layers/`

A single print layer. Pixel data is held compressed in a `CMat` and decompressed on demand; the codec is selectable via
`LayerCompressionCodec` (PNG, GZip, Deflate, Brotli, LZ4, Zstd) and implemented by `MatCompressor*` classes in
`UVtools.Core/Compressors/`. Issue types (islands, overhangs, resin traps, …) also live here and are driven by
`Managers/IssueManager`.

### `Operation` — `UVtools.Core/Operations/`

Abstract base for every layer mutation (resize, hollow, repair, morph, calibration tests, …). Operations are UI-agnostic
`ObservableObject`s with a `SlicerFile`, `Validate()`/`ValidateSpawn()` gates, and `Execute(progress)`, and they are the
shared unit of work between the GUI and the CLI. Profiles/undo/session state go through `Managers/OperationSessionManager`
and `ClipboardManager`.

`Suggestions/` is a parallel, smaller hierarchy (`Suggestion` + `SuggestionManager`) for auto-detected corrections rather
than user-invoked tools.

### Image processing

All pixel work goes through **EmguCV** (OpenCV wrapper). The native library (`cvextern.dll` / `libcvextern.so` /
`libcvextern.dylib`) comes from the `Emgu.CV.runtime.mini.*` packages and from `build/platforms/*`, copied into the
output by `UVtools.UI.csproj`. Do not replace EmguCV calls with pure-managed alternatives — performance is critical.
`MatCacheManager` and `KernelCacheManager` exist to avoid repeated allocations; prefer them over ad-hoc `Mat`/kernel
creation.

### UI layer — `UVtools.UI`

Avalonia **12** with `CommunityToolkit.Mvvm`, SukiUI theming, and compiled bindings on by default
(`AvaloniaUseCompiledBindingsByDefault`). `MainWindow` is intentionally large and is split into feature partial classes
(`MainWindow.Issues.cs`, `MainWindow.LayerPreview.cs`, `MainWindow.Layer3DPreview.cs`, `MainWindow.PixelEditor.cs`,
`MainWindow.GCode.cs`, …) — put new main-window behaviour in the matching partial rather than growing
`MainWindow.axaml.cs`.

Each `Operation` is surfaced by a matching `ToolControl` in `Controls/Tools/` (`OperationBlur` → `ToolBlurControl`),
hosted generically by `Windows/ToolWindow`; calibration operations pair with `Controls/Calibrators/`, suggestions with
`Controls/Suggestions/`. Adding a tool means adding both halves plus the wiring in `MainWindow`.

### CLI — `UVtools.Cmd`

One class per verb under `Symbols/` (`ConvertCommand`, `ExtractCommand`, `RunCommand`, `SetPropertiesCommand`, …), with
shared `GlobalArguments`/`GlobalOptions`. `RunCommand` executes Core `Operation`s and scripts, so CLI parity usually
comes for free when an operation is added to Core.

### Scripting

`UVtools.Core/Scripting/` hosts a Roslyn C# scripting runtime (`Scripter`, `ScriptParser`, typed `Script*Input`
controls) letting users write runtime operations; `OperationScripting` is the bridge into the normal operation pipeline.
`Scripts/UVtools.ScriptSample` is the reference project for script authors.

## Code Guidelines

- Follow existing naming conventions and code style — match the surrounding file.
- Every source file starts with the AGPL-3.0 header comment block; keep it on new files.
- Unsafe blocks are enabled in `UVtools.Core`, `UVtools.UI`, and `UVtools.AvaloniaControls`, and are used for
  performance-critical image processing.
- Nullable reference types are enabled solution-wide — respect nullability annotations.
- `ZLinq` (`AsValueEnumerable()`) is used in hot paths instead of LINQ; follow that pattern where the surrounding code
  does. See `AGENTS.md` for the DotNext buffer-writer and allocation rules.
- Do not leave large blocks of commented-out code.
- XML doc comments (`///`) are expected on public API members in `UVtools.Core` (documentation XML is generated to
  `documentation/UVtools.Core.xml`).

## Testing

`tests/UVtools.Tests` (xUnit) covers targeted regression areas — format round-trips, specific operations, Gerber/Excellon
parsing, mesh building. It is not comprehensive: most validation still happens manually through the UI/CLI or the
built-in calibration tests. Add tests there when changing the covered areas or fixing a reproducible bug.
