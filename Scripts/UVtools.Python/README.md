# UVtools Python Scripting

<p align="center">
   <br>
  <img src="https://github.com/sn4k3/UVtools/raw/master/UVtools.CAD/UVtools_python.png">
</p>

## Requirements

- [Python 3.x](https://www.python.org/downloads)
- [Pythonnet 3.x](https://github.com/pythonnet/pythonnet): `pip install pythonnet`
- [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [UVtools](https://github.com/sn4k3/UVtools/releases/latest):
  - Windows: Must install MSI
  - Linux and macOS: Need to register `UVTOOLS_PATH` (Environment Variable)
- Near your .py script:
  - [UVtoolsBootstrap.py](https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/UVtools.Python/UVtoolsBootstrap.py)
  `wget https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/UVtools.Python/UVtoolsBootstrap.py`
<!--  - [UVtools.runtimeconfig.json](https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/UVtools.Python/UVtools.runtimeconfig.json)
  `wget https://raw.githubusercontent.com/sn4k3/UVtools/master/Scripts/UVtools.Python/UVtools.runtimeconfig.json`
!-->

## Bootstrap

```python
#!/usr/bin/env python
"""print_layers.py: Print all layers from a file."""
__author__      = "Someone"
__copyright__   = "Copyright 2022, Planet Earth"
from UVtoolsBootstrap import *

# Your code here
```


## Documentation

- Must explore [UVtools.Core](https://github.com/sn4k3/UVtools/tree/master/UVtools.Core) code, functions and arguments 
are the same as source.
- See [code samples](https://github.com/sn4k3/UVtools/tree/master/Scripts/UVtools.Python) 

## Support
https://github.com/sn4k3/UVtools/discussions/categories/scripts

## Contribute with your scripts
If you make a usefull script and want to contribute you can share and publish your script under 
[Github - Discussions - Scripts](https://github.com/sn4k3/UVtools/discussions/categories/scripts). 