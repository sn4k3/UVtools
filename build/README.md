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

- **compile.(ps1|sh):** Compiles project under default configurations
- **run.(ps1|sh):** Compiles and runs the project under default configurations
- **createRelease.(ps1|sh):** Compiles, publish and pack a release for a target runtime