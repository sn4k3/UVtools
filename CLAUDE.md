# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

UVtools is a cross-platform (Windows, Linux, macOS) MSLA/DLP resin printer file analysis, calibration, repair, conversion, and manipulation application. It supports 35+ proprietary file formats (CTB, SL1, GOO, PWS, etc.) and provides both a desktop GUI and CLI.

## Build & Run

```bash
# Build entire solution
dotnet build

# Build release
dotnet build -c Release

# Run the desktop UI
dotnet run --project UVtools.UI

# Run the CLI
dotnet run --project UVtools.Cmd -- <args>
```

Build scripts in `build/` offer platform-specific shortcuts (`compile.bat`, `compile.sh`), but plain `dotnet build` works for development.

**Note:** Assemblies are signed with `build/UVtools.snk`. This file must be present to build successfully.

## Project Structure

| Project | Purpose |
|---------|---------|
| `UVtools.Core` | Core library: file formats, layer processing, image ops |
| `UVtools.UI` | Avalonia desktop GUI |
| `UVtools.Cmd` | CLI (uses `System.CommandLine`) |
| `UVtools.AvaloniaControls` | Reusable Avalonia controls (multi-targeted net8/9/10) |
| `UVtools.Installer` | WiX MSI installer for Windows |
| `UVtools.ScriptSample` | Example project for the built-in C# scripting engine |

Global build properties (version, target framework, artifact paths, signing) are centralized in `Directory.Build.props`. The version is the single `<UVtoolsVersion>` property there.

Build output goes to `artifacts/` at the repo root.

## Architecture

### Core Abstractions

**`FileFormat`** (`UVtools.Core/FileFormats/FileFormat.cs`) — Abstract base for every supported printer file format. Implements `IList<Layer>`. All 35+ format classes (e.g., `ChituboxFile`, `GooFile`, `SL1File`) inherit from it. The class handles decode/encode lifecycle, GCode, thumbnails, print settings, and per-layer overrides.

**`Layer`** (`UVtools.Core/Layers/`) — Represents a single print layer. Stores compressed image data; decompresses on demand via codec classes in the same folder.

**`Operation`** (`UVtools.Core/Operations/`) — Abstract base for all layer-level mutations (resize, hollow, repair, calibration tests, etc.). Operations are designed to be UI-agnostic and are executed by both the GUI and the CLI.

### Image Processing

All pixel/image work goes through **EmguCV** (OpenCV C# wrapper). The native library (`cvextern.dll` / `libcvextern.so` / `libcvextern.dylib`) is bundled via NuGet (`Emgu.CV.runtime.mini.*`). Do not replace EmguCV calls with pure-managed alternatives—performance is critical here.

### UI Layer

`UVtools.UI` uses **Avalonia 11** with `CommunityToolkit.Mvvm`. The main window (`MainWindow.axaml/.cs`) is intentionally large and monolithic. Custom reusable controls live in `UVtools.AvaloniaControls`.

### Scripting Engine

`UVtools.Core/Scripting/` hosts a C# scripting runtime (`Microsoft.CodeAnalysis.CSharp.Scripting`) that lets users write and execute custom operations at runtime. `UVtools.ScriptSample` is the reference project for script authors.

## Testing

There is no formal test project or test framework in this solution. Validation is done manually via the UI/CLI or through the built-in calibration/test operations.

## Code Guidelines

- Follow existing naming conventions and code style — match the surrounding file.
- Unsafe blocks are allowed (enabled in `UVtools.Core.csproj`) and are used for performance-critical image processing.
- Nullable reference types are enabled solution-wide — respect nullability annotations.
- Do not leave large blocks of commented-out code.
- XML doc comments (`///`) are expected on public API members in `UVtools.Core` (a documentation XML is generated to `documentation/UVtools.Core.xml`).

## Adding a New File Format

1. Create a class in `UVtools.Core/FileFormats/` inheriting `FileFormat`.
2. Implement at minimum: `Decode`, `Encode`, `FileExtensions`, and the format-specific header/layer structures.
3. Register the format in `FileFormat.AvailableFormats` (static list in `FileFormat.cs`).
