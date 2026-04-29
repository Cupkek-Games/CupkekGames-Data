# CupkekGames Data

Data foundation for CupkekGames packages. **No Luna / UI dependency** — reusable in any Unity project.

## What's inside

- **`Data/`** (`CupkekGames.Data.asmdef`) — `IData`, `DataSO`, asset catalog. Foundation for inventory/save/stats data shape.
- **`Data.Primitives/`** — primitive `IData` types (int/float/bool/string wrappers).
- **`Data.DropTable/`** — drop-table data + roll utilities for loot/random distribution.

## Dependencies

- `com.cupkekgames.keyvaluedatabases` (catalogs/databases)
- `com.cupkekgames.editorui` (custom inspectors)
- `com.cupkekgames.assetfinder` (`[AssetFinder]` attribute on catalogs)
- `com.cupkekgames.services` (data service registration)

## Sibling packages built on Data

- `com.cupkekgames.gamesave` — generic `GameSaveManager<TData, TMeta>` (was previously a sub-asmdef of data; now its own package)
- `com.cupkekgames.newtonsoft` — Newtonsoft adapter for `IData` JSON persistence
- `com.cupkekgames.rpgstats`, `com.cupkekgames.inventory`, `com.cupkekgames.settings` — feature packages

## Distribution

Distributed via the CupkekGames UPM scoped registry (`https://www.docs.cupkek.games/upm`). See `com.cupkekgames.packagemanager`.
