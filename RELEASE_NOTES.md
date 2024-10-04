- **File formats:**
  - (Change) Rename file and class from `PhotonSFile` to `AnycubicPhotonSFile`
  - (Change) Rename file and class from `PhotonWorkshopFile` to `AnycubicFile`
  - (Change) Rename file and class from `CXDLPFile` to `CrealityCXDLPFile`
  - (Change) Rename convert menu group from `CXDLP` to `Creality CXDLP`
  - (Fix) CTB (Version 5): `NullReferenceException` when trying to convert from a file with a `null` MaterialName (#857)
  - (Fix) Sanitize file version before convert the file to ensure capabilities (#934)
  - (Fix) Unable to set the format version when converting from files with a version that match it own default version (#857)

