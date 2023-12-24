- (Add) Setting: Automations - Restrict the file name to valid ASCII characters (Default: Off)
- (Add) New check when opening a file that verify if the file name have invalid characters and prompt for rename based on the above introduced setting
- (Add) Rename file: Add a option to allow only ASCII characters on the file name
- (Improvement) LZ4 layer compression by reusing pools of memory buffers, this relief the LOH allocations and improves the overall performance
- (Fix) Phased Exposures: Disallow AnyCubic file formats from run the tool

