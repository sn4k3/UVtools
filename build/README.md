# Build scripts and native runtimes

Build with the .NET 10 SDK. Release packaging additionally needs the tools listed in the
[StageKit.Fallout documentation](https://github.com/sn4k3/StageKit).

## Emgu CV 5 mini runtimes

Builds, tests and releases retain the NuGet mini packages. `EmguCvVersion` in
`Directory.Build.props` keeps the managed wrapper and all native runtimes at the same version.
The runtime dependencies are declared in `UVtools.Core/UVtools.Core.csproj` and flow to the UI,
command-line application and tests. Custom binaries in `build/platforms/<RID>/` override
the corresponding NuGet `cvextern` binary; missing custom files fall back to the packages.
Supporting native libraries from NuGet remain included.

`Directory.Build.targets` copies custom binaries after NuGet's build copies and replaces
matching publish entries. RID-less output uses `runtimes/<RID>/native/`; RID-specific output
puts the selected binary beside the executable. The usual filenames are `cvextern.dll`
on Windows, `libcvextern.so` on Linux, and `libcvextern.dylib` on macOS. This also supports
`build/platforms/win-arm64/cvextern.dll` when provided.

Use `-p:UseCustomEmguCvRuntime=false` to build or publish using only the NuGet binaries.

| UVtools RID            | NuGet runtime package                      | Native platform                                                           |
|------------------------|--------------------------------------------|---------------------------------------------------------------------------|
| `win-x64`, `win-arm64` | `Emgu.CV.runtime.mini.windows`             | Windows; NuGet also supplies the matching MSVC runtime                    |
| `osx-x64`, `osx-arm64` | `Emgu.CV.runtime.mini.macos`               | macOS Intel / Apple Silicon                                               |
| `linux-x64`            | `Emgu.CV.runtime.mini.ubuntu-24.04-x64`    | Ubuntu 24.04 and systems with its compatible native libraries             |
| `linux-arm64`          | `Emgu.CV.runtime.mini.debian-trixie-arm64` | Debian 13 (Trixie) ARM64 and systems with its compatible native libraries |

The Ubuntu package exposes `ubuntu-x64`, which NuGet cannot select for a `linux-x64` publish.
Core supplies a portable-path alias for RID-less builds; `Directory.Build.targets` puts that
same package binary beside the executable in a `linux-x64` publish. This preserves UVtools'
release names; it does **not** make the Ubuntu binary compatible with every Linux distribution.
An explicit `ubuntu-x64` publish uses NuGet's native asset selection directly. `UseRIDGraph`
remains enabled for that package's distribution-specific RID.

The mini packages still depend on system libraries. Install these runtime dependencies for
an Ubuntu 24.04 x64 installation:

```sh
sudo apt-get update
sudo apt-get install --yes libtiff6 libgeotiff5 libopenjp2-7 \
  libgstreamer1.0-0 libgstreamer-plugins-base1.0-0 \
  libavcodec60 libavformat60 libavutil58 libswscale7 \
  libgtk-3-0t64 liblapack3 libblas3
```

For Debian 13 ARM64, the mini binary requires a different FFmpeg ABI and codec libraries:

```sh
sudo apt-get update
sudo apt-get install --yes libgeotiff5 libavif16 libwebp7 libwebpmux3 libwebpdemux2 \
  libopenjp2-7 libopenexr-3-1-30 libavcodec61 libavformat61 libavutil59 libswscale8
```

These lists cover the direct non-base dependencies of the packaged `libcvextern.so`; APT
installs their transitive dependencies. Avalonia and .NET can have additional requirements.
The Debian ARM64 package is not an Ubuntu 24.04 ARM64 runtime: it requires newer libraries,
including glibc 2.38 or later and FFmpeg 7. An AppImage or self-contained .NET publish does
not eliminate OpenCV's system-library requirements. Debian/RPM/Arch package users also need
the matching native libraries; the portable RID is not a distribution compatibility promise.

On Windows N or Windows Server, OpenCV's MSMF support may require the Media Feature Pack.
See the [Emgu CV 5 release notes](https://www.emgu.com/wiki/index.php/Version_History#Emgu.CV-5.0.0).

Recompile the custom binaries for Emgu CV 5 before running builds with overrides enabled.
Replace the files in `build/platforms/<RID>/` with the matching OpenCV 5 builds. Publish to
a clean directory when changing native versions so stale binaries do not remain in output.

## Troubleshooting and custom builds

For a Linux native-load failure, run `ldd ./libcvextern.so` in the published application folder (or on
`runtimes/linux-x64/native/libcvextern.so` in a RID-less build) and inspect `not found`
entries. Install the matching distribution libraries before attempting a native rebuild.

- `createRelease.ps1` / `createRelease.sh`: publish and package releases using custom runtime
  overrides where available, with the NuGet mini packages providing the remaining files.
- `cvextern.ps1` / `libcvextern.sh`: optional native compilation for systems not covered by
  the NuGet packages. Use the tag matching `EmguCvVersion`; these scripts are not part of
  normal builds. A custom binary must match both the managed Emgu CV version and the target
  system's architecture and native dependencies.

Custom compilation remains separate from normal .NET builds. Update `build/platforms/*`
after compiling; subsequent builds and publishes pick up the replacement binaries.
