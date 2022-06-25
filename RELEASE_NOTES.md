- **PCB Exposure:**
   - (Add) Gerber file extensions: gko, gtl, gto, gts, gbl, gbo, gbs, gml
   - (Fix) Trailing zeros suppression mode and improve the parsing of the coordinate string (#492)
   - (Fix) Allow coordinates without number `XYD0*` to indicate `X=0` and `Y=0`
   - (Fix) Do not try to fetch apertures lower than index 10 when a `D02` (closed shutter) is found alone

