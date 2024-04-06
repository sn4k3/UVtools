- **File formats:**
  - (Add) nanoDLP file format
  - (Add) SL1 printer note keyword: `LAYERIMAGEFORMAT_xxx` sets the layer image format required for the converted file if the format have multiple options (For Archives with PNG's)
  - (Fix) Anycubic file format: Model Min/Max(X/Y) was not properly calculated
  - (Fix) Photon Mono M5s Pro incorrect display height and width (fixes #854)
- **PrusaSlicer:**
  - (Add) Concepts3D Athena 8K & 12K
  - (Change) Wanhao D7: Add `LAYERIMAGEFORMAT_Png32` to printer notes
  - (Change) Nova3D Bene4 Mono, Bene5, Elfin2 Mono SE, Whale, Whale2: Add `LAYERIMAGEFORMAT_Png24BgrAA` to printer notes
- **UI:**
  - (Add) Settings - Automations - Events: After file load, before file save and after file save. Events are fired upon an action and execute a defined script.
        If the script is written with the UVtools scripting structure, it will run under an operation and within the Core context.
        Otherwise, if plain C# code is used, it will run under the Terminal and in the UI context.
  - (Add) Show a message of congratulations on the software birthday (Trigger only once per year)
  - (Add) Menu - Help: Add "Community forums" submenu, move Facebook group into it and add GitHub, Reddit, Twitter and Youtube
  - (Change) Window title: Move version near software name and add the system arch to it
  - (Change) About window: Move version near software name and add "Age" label
  - (Change) Benchmark tool: Add a thin border to the result panels
  - (Improvement) On the status bar, hide the " @ mm/min" from lift speed label if file is not able to use lift speed parameters
  - (Improvement) Re-style the new version button
  - (Fix) Show print times correctly when larger than a day (#854)
- (Upgrade) .NET from 6.0.27 to 6.0.28
- (Upgrade) Wix from 4.0.4 to 5.0.0

