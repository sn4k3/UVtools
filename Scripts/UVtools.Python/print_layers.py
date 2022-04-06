#!/usr/bin/env python
"""print_layers.py: Print all layers from a file."""
__author__      = "Someone"
__copyright__   = "Copyright 2022, Planet Earth"
from UVtoolsBootstrap import *

file = input('Input the file path: ')

slicerFile = None
try:
    slicerFile = FileFormat.Open(file)
except Exception as e:
    print(e)
    exit(-1)

if slicerFile is None:
    print(f'Unable to find {file} or it\'s invalid file')
    exit(-1)

for layer in slicerFile:
    print(layer)
