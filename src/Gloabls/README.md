# ConquestFrontierWarsRay Globals

This project is the direct-use managed rewrite of the portable `Globals` and
`MGlobals` gameplay core.

It intentionally keeps the parts that are still useful to the raylib runtime:

- shared gameplay constants
- legacy packet and event identifiers
- player/global state
- alliance and visibility masks
- part-id generation helpers
- resource totals and caps
- mission objective state
- object-class classification helpers
- JSON-backed resource metadata such as shared color tables

It intentionally drops the old native shell:

- `Globals.dll`
- Win32 handles and resource-module loading
- DACOM globals and interface pointers
- editor viewers and document plumbing
- menu/dialog factory exports

## Related code

- [ConquestGlobalsService.cs](D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Gloabls/ConquestGlobalsService.cs)
- [ConquestGlobalState.cs](D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Gloabls/ConquestGlobalState.cs)
- [ObjectClassClassifier.cs](D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Gloabls/ObjectClassClassifier.cs)
- [MGlobals-port-ledger.md](D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Gloabls/MGlobals-port-ledger.md)
- [Resources/globals-resource-manifest.json](D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Gloabls/Resources/globals-resource-manifest.json)
- [tools/generate_globals_assets.py](D:/git2/Conquest-Frontier-Wars-Source2/ConquestFrontierWarsRay/src/Gloabls/tools/generate_globals_assets.py)
