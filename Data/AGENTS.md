# CupkekGames.Data Package — AI Agent Instructions

## Package Overview

**CupkekGames.Data** is a flexible, composition-based data system built for Unity. It provides:
- **Type-safe serialization** via `IData` interface and `DataSO<T>` base class
- **String-key lookups** via `IKeyProvider` for decoupling references (no direct SO references)
- **Flexible asset references** via `SerializableReference<T>` (asset or inline)
- **Composition support** via `IFeature` for building complex data types without inheritance
- **Editor tooling** via custom drawers for data keys, serializable references, and database lookups

The Data package is **composable**: systems like InventorySystem layer on top of it, implementing features, validators, and serializers specific to their domain.

## Core Principles

### 1. IData Interface
All serializable data types implement `IData`:
```csharp
public interface IData
{
    bool Validate();
    void OnAfterDeserialize();
    IData CloneData();
}
```
- **Validate()** → Returns true if the data is valid (e.g., name not empty)
- **OnAfterDeserialize()** → Hook for post-deserialization logic (e.g., resolve guids, cache values)
- **CloneData()** → Should return `new MyData(this)` where **`MyData(MyData other)`** is a **copy constructor** that deep-copies fields (no `ServiceLocator`). Prefer **`IFeature.CloneFeature()`** (typically `new ConcreteFeature(this)`) for `[SerializeReference]` feature lists.

**IFeature** includes **`CloneFeature()`** so polymorphic features copy correctly; every concrete feature type should use a copy ctor and `CloneFeature() => new MyFeature(this)`.

**Example (InventoryItemDefinition):**
```csharp
public class InventoryItemDefinition : IData
{
    public string Name = "";
    public string Description = "";
    [DatabaseKey("ItemIcon")]
    public string IconKey = "";
    public int MaxStackAmount = 1;
    [SerializeReference]
    public List<IFeature> Features = new();
    
    public bool Validate() => !string.IsNullOrEmpty(Name);
    public void OnAfterDeserialize() { }
    public InventoryItemDefinition(InventoryItemDefinition other) { /* copy fields + Features */ }
    public IData CloneData() => new InventoryItemDefinition(this);
}
```

### 2. DataSO / DataSO<T> Base Classes
Non-generic **`DataSO`** exists so bootstrap code can enumerate all `DataSO` instances (e.g. `Resources.FindObjectsOfTypeAll` in editor). Play-session id bumps on **`BeforeSceneLoad`** (safe when Domain Reload is off). **`Initialize()`** copies default → actual via **`IData.CloneData()`** (no serializer) when the play session id changed. JSON still uses **`IDataSerializer`** for `LoadFromJson` / `ToJson`.

**Play mode (editor):** `DataSOPlayModeEditorBootstrap` on **`ExitingEditMode`** applies default → actual + `SetDirty` so Inspector and runtime agree before `Awake`. **`DataSOPlayModeRuntime`** runs **`AfterSceneLoad`** only in the **editor** as a safety net to call **`EnsurePlaySessionInitialized()`** on loaded assets. **Player builds** do not run that scan; **`OnEnable`** (with `Application.isPlaying`) calls **`EnsurePlaySessionInitialized()`** when each asset loads.

```csharp
public abstract class DataSO : ScriptableObject { /* EnsurePlaySessionInitialized, OnEnable */ }

public abstract class DataSO<T> : DataSO, IDataSO where T : IData, new()
{
    [SerializeField] private T _defaultData = new();
    [SerializeField] private bool _resetOnStart = true;
    [SerializeField] private T _actualData;
    public T Data { get; set; }
    public void Initialize();
    public void LoadFromJson(string json, bool toDefault = false);
    public string ToJson(bool useDefault = false);
    public void ResetToDefault();
}
```
- **Two-state persistence** → `_defaultData` (template) + `_actualData` (runtime)
- **JSON** → `IDataSerializer` / ServiceLocator
- **`_resetOnStart`** → When true, each play session resets `_actualData` from `_defaultData` using **`CloneData()`**

**Example (InventoryItemDefinitionSO):**
```csharp
public class InventoryItemDefinitionSO : DataSO<InventoryItemDefinition>
{
    [MenuItem("Assets/Create/CupkekGames/Inventory/Item Definition")]
    public static void Create() { /* ... */ }
}
```

### 3. String Keys (IKeyProvider Pattern)
Replaces direct SO references with **string keys** resolved at runtime:
```csharp
public interface IKeyProvider
{
    IEnumerable<string> GetKeys();
}

public interface IGroupedKeyProvider : IKeyProvider
{
    IEnumerable<(string key, string group)> GetGroupedKeys();
}
```

**Attribute-driven lookups:**
```csharp
public class InventoryItemDefinition : IData
{
    [DatabaseKey("ItemIcon")]  // Lookup "ItemIcon" database for key
    public string IconKey = "";
}
```

**Why?** Loose coupling, easier refactoring, serialization-friendly.

### 4. SerializableReference<T>
Flexible reference that can be **either an asset or inline data**:
```csharp
[SerializeField] private ReferenceMode _mode;
[SerializeField] private UnityEngine.Object _assetReference;
[SerializeReference] private T _inlineValue;

public T Value { get; set; }
public bool HasValue { get; }
public ReferenceMode Mode { get; }
```

**Use cases:**
- Inline: Small, one-off data (no need for SO)
- Asset: Shared, reusable SO instances

### 5. Composition via IFeature
Data types can extend capabilities without inheritance:
```csharp
public interface IFeature { }
```

**Example (InventorySystem + RPGStats bridge):**
```csharp
// See InventorySystem.RPGStats — EquipableFeature uses CupkekGames.RPGStats.AttributeEffect
public class EquipableFeature : IItemFeature
{
    public CatalogKey EquipmentType;
    public AttributeEffect Effects;
}

public class PotionFeature : IFeature
{
    public int HealAmount;
}

// InventoryItemDefinition composes both:
var itemDef = new InventoryItemDefinition();
itemDef.Features.Add(new EquipableFeature { /* ... */ });
itemDef.Features.Add(new PotionFeature { /* ... */ });
```

**Rules:**
- Features are `[SerializeReference]` — polymorphic serialization works seamlessly
- Query with `GetFeature<T>()`, `HasFeature<T>()`, `GetFeatures<T>()`
- Game code defines domain-specific features; framework stays generic

## Package Structure

```
CupkekGames.Data/
  Runtime/
    Core/
      IData.cs                    ← Every serializable type implements this
      DataSO.cs                   ← Base class for IData in ScriptableObjects
      IDataSO.cs                  ← Interface for DataSO
      IDataSerializer.cs          ← ServiceLocator interface: Clone, Serialize, Deserialize
      IAssetResolver.cs           ← Resolve guids → assets (optional feature)
    Keys/
      IKeyProvider.cs             ← Get list of keys (e.g., "Sword", "Potion")
      INamedKeyProvider.cs        ← Keyed provider for a specific domain
      INamedDatabaseProvider.cs   ← Multi-provider registry
      DatabaseKey*.cs             ← Scoped key databases (e.g., sprites by key)
      NamedKeyProvider*.cs        ← Implementations & SOs
    References/
      ReferenceMode.cs            ← enum: Asset vs. Inline
      SerializableReference.cs    ← Flexible ref<T>
    Features/
      IFeature.cs                 ← Marker for composition
      FeatureGroupAttribute.cs    ← Inspector grouping for [SerializeReference] lists
  Editor/
    Drawers/
      DatabaseKeyDrawer.cs        ← Inspector for [DatabaseKey] fields
      SerializableReferenceDrawer.cs ← Inline/Asset toggle UI
      FeatureListDrawer.cs        ← Group [SerializeReference] lists by feature type
    DataSOEditor.cs               ← Base editor with Save/Load JSON buttons
```

## Usage Patterns

### Pattern 1: Define Game Data as IData

```csharp
namespace MyGame.Items
{
    [Serializable]
    public class ItemData : IData
    {
        public string Name = "Item";
        public string Description = "";
        public int Rarity = 1;
        
        public bool Validate() => !string.IsNullOrEmpty(Name) && Rarity > 0;
        public void OnAfterDeserialize()
        {
            // Resolve any cached lookups, guids, etc.
        }
    }
    
    public class ItemDataSO : DataSO<ItemData>
    {
        [MenuItem("Assets/Create/MyGame/ItemData")]
        public static void Create() => CreateInstance<ItemDataSO>();
    }
}
```

### Pattern 2: Use String Keys Instead of Direct References

**Before (breaks easily):**
```csharp
public IconSO iconAsset;  // Direct reference → upgrade hell
```

**After (maintainable):**
```csharp
[DatabaseKey("ItemIcons")]
public string iconKey = "";  // Inspector shows dropdown of valid keys
```

At runtime, resolve via key provider:
```csharp
var iconProvider = ServiceLocator.Get<INamedKeyProvider>("ItemIcons");
var texture = iconProvider.GetValue(iconKey) as Texture2D;
```

### Pattern 3: Compose Data with Features

**Define features in your domain:**
```csharp
public class CombatFeature : IFeature
{
    public int Damage;
    public DamageType Type;
}

public class RarityFeature : IFeature
{
    public Rarity RarityLevel;
    public Color DisplayColor;
}
```

**Use in data:**
```csharp
var itemDef = new InventoryItemDefinition();
itemDef.Features.Add(new CombatFeature { Damage = 10 });
itemDef.Features.Add(new RarityFeature { RarityLevel = Rarity.Epic });

// Query at runtime:
if (itemDef.HasFeature<CombatFeature>())
    Debug.Log($"Damage: {itemDef.GetFeature<CombatFeature>().Damage}");
```

### Pattern 4: Use SerializableReference for Flexible Data

```csharp
[Serializable]
public class BuffData : IData
{
    public string Name = "";
    
    // Player can choose: hard asset reference or inline config
    public SerializableReference<StatModifier> Effect = new();
    
    public bool Validate() => !string.IsNullOrEmpty(Name) && Effect.HasValue;
    public void OnAfterDeserialize() { }
}
```

In Inspector:
- **Asset mode** → Pick shared StatModifier SO
- **Inline mode** → Edit StatModifier fields directly

## Integration Points

### ServiceLocator Requirement

`DataSO<T>` expects `IDataSerializer` in the ServiceLocator:
```csharp
public static IDataSerializer Serializer => ServiceLocator.Get<IDataSerializer>();
```

**Setup in your ServiceRegistry:**
```csharp
var registry = ScriptableObject.CreateInstance<ServiceRegistry>();
registry.Register<IDataSerializer>(new JsonDataSerializer()); // Your impl
// Persist or inject into ServiceLocator.Initialize()
```

### For Game Projects Using InventorySystem

The Items/Inventory setup demonstrates the full pattern:
1. **Data** (this package) provides core infrastructure
2. **InventorySystem** builds game concepts (items, slots, inventory)
3. **Luna's `Samples~/GameFull` sample** provides concrete examples (Potions, Equipment) under `ScriptableObjects/Items/`
4. **Your game** extends with custom features (Recipes, Buffs, Enchantments)

## Coding Conventions

- **Namespaces:** `CupkekGames.Data.*` for core; game code uses domain namespaces
- **IData implementers:** Always provide `Validate()` and `OnAfterDeserialize()`
- **DatabaseKey attributes:** Specify provider name (e.g., "ItemIcons", "ItemTypes")
- **SerializeReference lists:** Use `FeatureGroupAttribute` for UI grouping if many feature types
- **No `object` or `dynamic`:** All references strongly typed via generics
- **String keys preferred:** Over asset references for data lookups (loose coupling)

## Common Tasks

### Task: Create a New Data Type

1. Define `struct` or `class` implementing `IData`
2. Implement `Validate()` and `OnAfterDeserialize()`
3. Create `class YourDataSO : DataSO<YourData>` with `[CreateAssetMenu]`
4. Editor automatically provides Save/Load JSON buttons

### Task: Add a New IFeature Type

1. Define `public class YourFeature : IFeature { /* fields */ }`
2. Add to existing `List<IFeature> Features` in your data class
3. Query in runtime code: `data.GetFeature<YourFeature>()`
4. No base class changes needed

### Task: Set Up Key Database

1. Create `INamedKeyProvider` SO in Editor (e.g., ItemIconProvider)
2. Populate with key → value pairs
3. Register in ServiceLocator
4. Use `[DatabaseKey("ItemIcons")]` in data fields

### Task: Validate Data on Load

Use `OnAfterDeserialize()` to resolve guids, rebuild caches, or fix deprecated formats:
```csharp
public void OnAfterDeserialize()
{
    if (string.IsNullOrEmpty(CachedId))
        CachedId = System.Guid.NewGuid().ToString();
}
```

## Companion package: `CupkekGames.Data.DropTable`

Small runtime layered on top of this package. Namespace `CupkekGames.Data.DropTable`; assembly `CupkekGames.Data.DropTable` (separate asmdef).

Types:
- **`DropEntry`** — single weighted row: `CatalogKey`, `Chance`, `MinAmount`, `MaxAmount`.
- **`DropTable : IData`** — list of entries plus `Evaluate(...)` roller.
- **`DropTableSO : DataSO<DropTable>`** — asset wrapper.
- **`DropResult`** — output struct: `CatalogKey`, `Amount`.

`DropTable.Evaluate` supports `minimumDrops` (forces highest-chance pending entries in when too few rolled through), `chanceMultiplier` (global scaling), `limit` (cap; `minimumDrops` wins over `limit` when both set), and an optional per-entry `modifier` delegate that can fully override the effective chance and add an amount bonus. Minimum-fill uses effective chance as the tiebreaker, and amounts are clamped so `min ≥ 1` and `max ≥ min`.

Depends on `CupkekGames.Data` (this package) and `CupkekGames.Systems.ServiceLocator`. `CupkekGames.InventorySystem` references this assembly so inventory samples can use drop tables without duplicating types.

## Notes for AI Assistants

- **Composition over inheritance** → Feature-based data is more flexible than base class hierarchies
- **ServiceLocator pattern** → Data package itself doesn't enforce initialization; consuming systems set it up
- **String keys aren't opaque** → Provide Inspector dropdown via custom drawer (see `DatabaseKeyDrawer`)
- **IDataSerializer is pluggable** → Default is JSON; game can use MessagePack, Protocol Buffers, etc.
- **Editor support varies** → Custom drawers exist; extend as needed for domain-specific fields
