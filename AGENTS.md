# CupkekGames Data — AI Agent Instructions

## Package Overview

**CupkekGames Data** (`com.cupkekgames.data`) is the persistence foundation for CupkekGames packages. Pure data layer — no UI, no game-specific logic. Provides IData types, asset catalog (`AssetCatalog<T>`), drop-table utilities, and primitive IData wrappers.

(ServiceLocator, GameSave runtime, and Newtonsoft serialization were once sub-asmdefs of this package; they're now their own sibling packages — `com.cupkekgames.services`, `com.cupkekgames.gamesave`, `com.cupkekgames.newtonsoft`.)

Direct deps: `keyvaluedatabase`, `editorui`, `assetfinder`, `servicelocator`. Reusable in any Unity project (no Luna UI dep).

## Critical: Do not hand-edit Unity serialized assets or `.meta` files

- Don't edit `.meta`, `.asset`, `.prefab`, `.unity` directly. Use Unity Editor.
- `.meta` GUIDs preserved across moves — use `git mv` (or plain `mv` when working untracked).

## Package Structure

```
com.cupkekgames.data/
  package.json
  README.md
  AGENTS.md
  Data/                          ← CupkekGames.Data.asmdef
    Runtime/                       (IData, DataSO, asset catalog)
    Editor/
  Data.Primitives/               ← CupkekGames.Data.Primitives.asmdef
    Runtime/                       (primitive IData wrappers)
    Editor/
  Data.DropTable/                ← CupkekGames.Data.DropTable.asmdef
    Runtime/                       (drop tables, roll utilities)
  ServiceLocator/                ← CupkekGames.Systems.ServiceLocator.asmdef
    Runtime/                       (service registration + lookup)
    Editor/
  GameSave/                      ← CupkekGames.Systems.GameSave.asmdef
    Runtime/                       (GameSaveManager<T,M>, save slots, file I/O)
  Newtonsoft/                    ← CupkekGames.Newtonsoft.asmdef
    Runtime/                       (Newtonsoft.Json wrapper utilities)
  Data.Newtonsoft/               ← CupkekGames.Data.Newtonsoft.asmdef
    Runtime/                       (IData ↔ Newtonsoft serialization bridge)
```

## Coding Conventions

- **Namespaces**: match asmdef name (e.g. `CupkekGames.Data`, `CupkekGames.Systems.ServiceLocator`, `CupkekGames.Systems.GameSave`)
- **Asmdefs**: GUID references, not name references
- **String keys over SO references**: data classes use string keys for cross-references; resolved via databases at runtime
- **Strict typing**: all C# is strictly typed
- **No Luna deps**: this package's identity is "no UI". If you find yourself wanting a UI type, the code belongs in a different package (luna or a Luna-aware feature package like settings/inventory).

## What NOT to add here

- UI code → `com.cupkekgames.luna`
- Inventory / RPGStats / specific gameplay → their own packages
- Settings UI → `com.cupkekgames.settings`
- Scene management → `com.cupkekgames.sequencer`

## Related packages

- `com.cupkekgames.keyvaluedatabases` — serializable Dictionary used by `AssetCatalog`
- `com.cupkekgames.assetfinder` — `[AssetFinder]` attribute on catalogs
- `com.cupkekgames.editorui` — custom inspector widgets
- `com.cupkekgames.services` — service registration
- `com.cupkekgames.luna` — UI library (consumers, not a dep here)
- Multi-package architecture: see https://docs.cupkek.games/luna/architecture (Luna's in-package ARCHITECTURE.md was removed 2026-06-11; the site is the docs home)
