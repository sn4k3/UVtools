# Build scripts & tools

## Requirements

### Windows

- [dotnet sdk](https://dotnet.microsoft.com/en-us/download)
- [Windows Subsystem for Linux (WSL)](https://docs.microsoft.com/en-us/windows/wsl/about)
  - dotnet-sdk
  - wget
  - zip
  - unzip

### Linux and macOS

- dotnet-sdk
- wget
- zip
- unzip 


## Scripts

- **compile.(bat|sh):** Compiles project under default configurations
- **run.(bat|sh):** Compiles and runs the project under default configurations
- **createRelease.(ps1|sh):** Compiles, publish and pack a release for a target runtime
- **libcvextern.sh:** Compiles and produces `libcvextern.so|dylib`
   - This script is independent and can run outside UVtools project, it's an standard alone script
   - Required when:
      - Your system is not included in UVtools releases
      - When lunch UVtools and got the error: `System.DllNotFoundException: unable to load shared library 'cvextern' or one of its dependencies`