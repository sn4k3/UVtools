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
        This means you haven't the required dependencies to run the cvextern library, that may due system version and included libraries version, they must match the compiled version of libcvextern.  
        To know what is missing you can open a terminal on UVtools folder and run the following command: `ldd libcvextern.so |grep not`. That will return the missing dependencies from libcvextern, you can try install them by other means if you can, but most of the time you will need compile the EmguCV to compile the dependencies and correct link them, this process is very slow but only need to run once.
    - How to apply to UVtools:
       - Copy the output file 'libcvextern.so|dylib' from 'emgucv/libs/*' created by this compilation to the UVtools executable folder and replace the original. 
       - Keep a copy of file somewhere safe, you will need to replace it everytime you update UVtools.
       - Additionally you can share your libcvextern.so|dylib on UVtools GitHub with your system information (Name & Version) to help others with same problem, 
         anyone with the same system version can make use of it without the need of the compilation process.
   - **Note:** You need to repeat this process everytime UVtools upgrades OpenCV version, keep a eye on changelog.