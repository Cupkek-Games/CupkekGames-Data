# CupkekGames Data

Data foundation for CupkekGames packages. **No Luna / UI dependency** — reusable in any Unity project.

## What's inside

### Foundation (`CupkekGames.Data` namespace)
- **`Data/`** (CupkekGames.Data.asmdef) — `IData`, `DataSO`, asset catalog. Foundation for inventory/save/stats data shape.
- **`Data.Primitives/`** — primitive `IData` types (int/float/bool/string wrappers).
- **`Data.DropTable/`** — drop-table data + roll utilities for loot/random distribution.

### Service registration
- **`ServiceLocator/`** (CupkekGames.Systems.ServiceLocator.asmdef) — service registration + locator pattern; lets systems find each other without hard references.

### Save system
- **`GameSave/`** (CupkekGames.Systems.GameSave.asmdef) — generic `GameSaveManager<TData, TMeta>`, file I/O, save slots, autosave events. Subclass for your save shape.

### JSON serialization
- **`Newtonsoft/`** (CupkekGames.Newtonsoft.asmdef) — Newtonsoft.Json wrapper utilities.
- **`Data.Newtonsoft/`** (CupkekGames.Data.Newtonsoft.asmdef) — bridge that serializes IData via Newtonsoft.

## Dependency graph

```
com.cupkekgames.core               (utilities)
       ↑
com.cupkekgames.data               ← this package
   + com.unity.nuget.newtonsoft-json
       ↑
       ├── com.cupkekgames.rpgstats
       ├── com.cupkekgames.inventory   (via rpgstats)
       ├── com.cupkekgames.sequencer
       ├── com.cupkekgames.settings
       └── com.cupkekgames.luna's GameFull sample
```

## Installation

Embedded package — clone the repo into your project's `Packages/` folder. Requires `com.cupkekgames.core` and `com.unity.nuget.newtonsoft-json`.

## Distribution

Unity Asset Store. Install order: core → data → other CupkekGames packages.

## Related packages

- [`com.cupkekgames.core`](../com.cupkekgames.core/) — shared utilities
- `com.cupkekgames.luna` — UI library
- `com.cupkekgames.rpgstats`, `com.cupkekgames.inventory`, `com.cupkekgames.settings`, etc. — feature packages built on this foundation
