- **File formats:**
   - (Add) `TransitionLayerCount` modifier to: Chitubox Zip, CWS, JXS, OSLA, PW*, UVJ, ZCodex, ZCode
   - (Add) Utility methods for transition layers calculation/parse
   - (Improvement) Calculate and set `TransitionLayerCount` property in file decode based on layer exposure time configuration
- **GCode:**
   - (Improvement) GCode: Able to parse layer image file with appended numbers on the filename (Afecting CWS) (#577)
   - (Fix) Bad parsing of the file when it comes from Lychee or NovaMaker slicer (Afecting CWS)
   - (Fix) Incorrect parse of "Wait time before cure" from layers when printer require wait sync moves (Afecting CWS)
- **Tools:**
   - (Add) External tests: The Complete Resin 3D Printing Settings Guide for Beginners
   - (Add) External tests: 9 settings for faster printing
   - (Improvement) Fade exposure time: Set `TransitionLayerCount` property with the affected layer count
- **Suggestions:**
   - (Add) Transition layers: If you are printing flat on the build plate your model will print better when using a smooth transition exposure time instead of a harsh variation, resulting in reduced layer line effect and avoid possible problems due the large exposure difference.
                              This is not so important when your model print raised under a raft/supports unaffected by the bottom exposure, in that case, it's fine to ignore this.
   - (Add) Model position: Printing on a corner will reduce the FEP stretch forces when detaching from the model during a lift sequence, benefits are: Reduced lift height and faster printing, less stretch, less FEP marks, better FEP lifespan, easier to peel, less prone to failure and use the screen pixels more evenly.
                           If the model is too large to fit within the margin(s) on the screen, it will attempt to center it on that same axis to avoid touching on screen edge(s) and to give a sane margin from it.
- **Status bar:**
   - (Add) Transition layers: 0/-0.00s
   - (Improvement) Change "Layer Height: 0.000mm" to "Layers: count @ 0.000mm"
   - (Improvement) Change "Bottom layers: 0" to "Bottom layers: 0/0.000mm"
- (Change) Show user informative message about CTB Encrypted file format once per ten file loads
- (Upgrade) .NET from 6.0.9 to 6.0.10
- (Fix) Windows MSI installation not upgrading well when downgrade libraries

