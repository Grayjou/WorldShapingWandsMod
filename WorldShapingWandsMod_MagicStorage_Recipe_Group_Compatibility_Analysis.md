# WorldShapingWandsMod MagicStorage Recipe Group Compatibility Analysis

Date: 2026-03-11  
Scope: `WorldShapingWandsMod` and `MagicStorage` recipe group integration without creating duplicate groups.

## Problem Statement

WorldShapingWandsMod (WSW) contains many items that could benefit from MagicStorage's recipe groups (like `AnySilverBar`), but adding direct compatibility would create cluttered recipe group management. The challenge is integrating WSW items into MagicStorage groups without:

1. Creating duplicate/conflicting recipe groups
2. Breaking existing recipes
3. Adding maintenance overhead

## MagicStorage Recipe Group System Analysis

### How MagicStorage Handles Groups

MagicStorage uses a sophisticated recipe group system in `MagicRecipes.cs`:

```csharp
// Register primary group
RecipeGroup.RegisterGroup("MagicStorage:AnySilverBar", group);

// Create clone that extends existing groups
RegisterGroupClone(group, nameof(ItemID.SilverBar));
```

### RegisterGroupClone Method

```csharp
private static void RegisterGroupClone(RecipeGroup original, string groupName) {
    // If the group already exists, union the sets and overwrite the reference
    if (RecipeGroup.recipeGroupIDs.TryGetValue(groupName, out int groupID)) {
        RecipeGroup group = RecipeGroup.recipeGroups[groupID];
        original.ValidItems.UnionWith(group.ValidItems);
        group.ValidItems = original.ValidItems;
    } else {
        RecipeGroup group = new RecipeGroup(original.GetText, new int[1]);
        group.ValidItems = original.ValidItems;
        group.IconicItemId = original.IconicItemId;
        RecipeGroup.RegisterGroup(groupName, group);
    }
}
```

This method:
- **Extends existing groups** by adding items to them
- **Creates new groups** if they don't exist
- **Uses UnionWith** to merge item sets without duplicates

## WorldShapingWandsMod Compatibility Implementation

### Option 1: Direct Group Extension (Recommended)

WSW can implement a similar `RegisterGroupClone` method and extend MagicStorage groups:

```csharp
// In WorldShapingWandsMod.cs or a dedicated compatibility class
public override void PostSetupContent()
{
    if (ModLoader.TryGetMod("MagicStorage", out Mod magicStorage))
    {
        ExtendMagicStorageGroups(magicStorage);
    }
}

private void ExtendMagicStorageGroups(Mod magicStorage)
{
    // Example: Add WSW silver items to MagicStorage's AnySilverBar group
    var silverGroup = CreateRecipeGroupClone("MagicStorage:AnySilverBar");
    if (silverGroup != null)
    {
        // Add WSW silver wand items
        silverGroup.ValidItems.UnionWith(GetWSWSilverItems());
    }
}

private RecipeGroup CreateRecipeGroupClone(string groupName)
{
    if (RecipeGroup.recipeGroupIDs.TryGetValue(groupName, out int groupID))
    {
        return RecipeGroup.recipeGroups[groupID];
    }
    return null;
}

private HashSet<int> GetWSWSilverItems()
{
    var items = new HashSet<int>();
    // Add WSW items that use silver bars in recipes
    // Example: items.Add(ModContent.ItemType<WandOfBuildingSilver>());
    return items;
}
```

### Option 2: Conditional Recipe Registration

WSW can check for MagicStorage and register additional recipes that use MagicStorage groups:

```csharp
public override void AddRecipes()
{
    // Base recipes always registered
    CreateRecipe(ItemID.SilverWand)
        .AddIngredient(ItemID.SilverBar)
        .Register();

    // Additional recipes only if MagicStorage is present
    if (ModLoader.TryGetMod("MagicStorage", out _))
    {
        CreateRecipe(ItemID.SilverWand)
            .AddRecipeGroup("MagicStorage:AnySilverBar")
            .Register();
    }
}
```

### Option 3: Dynamic Group Detection and Extension

More advanced approach that detects and extends groups automatically:

```csharp
private void AutoExtendRecipeGroups()
{
    var groupMappings = new Dictionary<string, Func<HashSet<int>>>()
    {
        { "MagicStorage:AnySilverBar", () => GetSilverItems() },
        { "MagicStorage:AnyWorkBench", () => GetWorkbenchItems() },
        { "MagicStorage:AnyChest", () => GetChestItems() }
    };

    foreach (var mapping in groupMappings)
    {
        if (RecipeGroup.recipeGroupIDs.TryGetValue(mapping.Key, out int groupID))
        {
            RecipeGroup group = RecipeGroup.recipeGroups[groupID];
            group.ValidItems.UnionWith(mapping.Value());
        }
    }
}
```

## Implementation Strategy

### Phase 1: Core Compatibility Class

Create `Common/Utilities/MagicStorageCompatibility.cs`:

```csharp
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace WorldShapingWandsMod.Common.Utilities;

public static class MagicStorageCompatibility
{
    public static void Initialize(Mod magicStorage)
    {
        if (magicStorage == null) return;
        
        ExtendRecipeGroups();
    }

    private static void ExtendRecipeGroups()
    {
        // Extend AnySilverBar with WSW silver-using items
        ExtendGroup("MagicStorage:AnySilverBar", GetSilverWandItems());
        
        // Extend other relevant groups
        ExtendGroup("MagicStorage:AnyWorkBench", GetWorkbenchWandItems());
        ExtendGroup("MagicStorage:AnyChest", GetChestWandItems());
    }

    private static void ExtendGroup(string groupName, HashSet<int> itemsToAdd)
    {
        if (RecipeGroup.recipeGroupIDs.TryGetValue(groupName, out int groupID))
        {
            RecipeGroup.recipeGroups[groupID].ValidItems.UnionWith(itemsToAdd);
        }
    }

    private static HashSet<int> GetSilverWandItems()
    {
        return new HashSet<int>
        {
            // Add WSW items that use silver bars
            ModContent.ItemType<WandOfBuildingSilver>(),
            ModContent.ItemType<WandOfDismantlingSilver>()
        };
    }

    private static HashSet<int> GetWorkbenchWandItems()
    {
        return new HashSet<int>
        {
            // Add WSW items crafted at workbenches
        };
    }

    private static HashSet<int> GetChestWandItems()
    {
        return new HashSet<int>
        {
            // Add WSW items that require chests
        };
    }
}
```

### Phase 2: Integration in Main Mod Class

```csharp
// In WorldShapingWandsMod.cs
public override void PostSetupContent()
{
    if (ModLoader.TryGetMod("MagicStorage", out Mod magicStorage))
    {
        MagicStorageCompatibility.Initialize(magicStorage);
    }
}
```

### Phase 3: Recipe Updates

Update wand recipes to use MagicStorage groups when available:

```csharp
// In wand recipe classes
public override void AddRecipes()
{
    var recipe = CreateRecipe()
        .AddIngredient(ItemID.SilverBar, 10);

    // Use MagicStorage group if available
    if (ModLoader.TryGetMod("MagicStorage", out _))
    {
        recipe.AddRecipeGroup("MagicStorage:AnySilverBar", 10);
    }
    else
    {
        recipe.AddIngredient(ItemID.SilverBar, 10);
    }

    recipe.Register();
}
```

## Benefits of This Approach

1. **No Group Clutter**: Extends existing groups instead of creating new ones
2. **Backward Compatibility**: Works whether MagicStorage is present or not
3. **Automatic Updates**: New WSW items automatically integrate
4. **Performance**: Uses HashSet UnionWith for efficient merging
5. **Maintenance**: Centralized compatibility logic

## Testing Requirements

1. **With MagicStorage**: Verify WSW items appear in MagicStorage recipe groups
2. **Without MagicStorage**: Verify recipes still work with vanilla items
3. **Recipe Resolution**: Test that recipes correctly use alternative ingredients
4. **No Duplicates**: Ensure no duplicate items in extended groups

## Alternative: Mod.Call Integration

If MagicStorage exposes a `Mod.Call` API for group extension:

```csharp
public override void PostSetupContent()
{
    if (ModLoader.TryGetMod("MagicStorage", out Mod magicStorage))
    {
        // Hypothetical API
        magicStorage.Call("ExtendRecipeGroup", "AnySilverBar", GetSilverWandItems());
    }
}
```

This would be cleaner if MagicStorage provides such an API.

## Conclusion

WorldShapingWandsMod can integrate with MagicStorage recipe groups by extending existing groups using `RecipeGroup.recipeGroups[groupID].ValidItems.UnionWith()`. This approach avoids creating duplicate groups while providing seamless compatibility. The implementation should be done in a dedicated compatibility class with automatic detection and extension of relevant groups.</content>
<parameter name="filePath">c:\Users\RYZEN 9\Documents\Cloned\SummariesAndAnalysis\tModLoader\Analysis\WorldShapingWandsMod_MagicStorage_Recipe_Group_Compatibility_Analysis.md