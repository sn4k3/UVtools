- (Change) `Layer.IsBottomLayer` no longer calculate the value using the position of the layer, a new property `IsBottomLayerByHeight` is now used to get that result
- (Improvement) Tool - Double exposure: Increase the bottom layer count per cloned bottom layer
- (Improvement) Calibration - Exposure time finder: Set the absolute bottom layer count accordingly when also testing for bottom time
- (Improvement) Goo: Enforce Wait times or Light-off-delay flag based on property set
- (Fix) AnyCubic and Goo: `PerLayerSetting` flag was set inverted causing printer not to follow layer settings when it should and also the otherwise (#689)
- (Fix) Tool - Scripting: Prevent from reload UI multiple times when using profiles (#694)

