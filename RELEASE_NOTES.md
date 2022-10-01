- **Layer:**
   - (Add) Property: `LayerMatBoundingRectangle`
   - (Add) Property: `LayerMatModelBoundingRectangle`
   - (Add) Method: `GetLayerMat(roi)`
- **Issues:**
   - **Islands:**
      - (Improvement) Islands detection performance
      - (Improvement) Required area to consider an island is now real area instead of bounding box area
      - (Fix) Logic bug when combining with overhangs
   - **Overhangs:**
      - (Improvement) Overhangs detection performance
      - (Improvement) Overhangs are now split and identified as separately in the layer
      - (Improvement) Overhangs now shows the correct issue area and able to locate the problem region more precisely
      - (Improvement) Compress overhangs into contours instead of using whole pixels, resulting in better render performance and less memory to hold the issue
      - (Fix) Bug in overhang logic causing to detect the problem twice when combined with supports
   - (Improvement) Touching bounds check logic to spare cycles
- **Tool - Raise platform on print finish:**
   - (Add) Preset "Minimum": Sets to the minimum position
   - (Add) Preset "Medium": Sets to half-way between minimum and maximum position
   - (Add) Preset "Maximum": Sets to the maximum position
   - (Add) Wait time: Sets the ensured wait time to stay still on the desired position.
           This is useful if the printer firmware always move to top and you want to stay still on the set position for at least the desired time.
           Note: The print time calculation will take this wait into consideration and display a longer print time.
- (Add) FileFormat: AnyCubic custom machine (.pwc)
- (Downgrade) OpenCV from 4.5.5 to 4.5.4 due a possible crash while detecting islands (Windows)

