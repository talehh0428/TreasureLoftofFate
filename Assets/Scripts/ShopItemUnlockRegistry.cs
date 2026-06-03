using System;
using System.Collections.Generic;

public static class ShopItemUnlockRegistry
{
    private const string AlwaysUnlockedItemId = "0001";

    public static event Action<string> ItemUnlocked;

    private static readonly HashSet<string> UnlockedItemIds = new HashSet<string>();
    private static readonly HashSet<string> RegisteredDefaultItemIds = new HashSet<string>();

    public static bool IsUnlocked(ShopItemDefinition definition)
    {
        return definition != null && IsUnlocked(definition.ItemId, definition.UnlockedByDefault);
    }

    public static bool IsUnlocked(string itemId, bool unlockedByDefault = false)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (UnlockedItemIds.Contains(itemId))
        {
            return true;
        }

        return unlockedByDefault || itemId == AlwaysUnlockedItemId || RegisteredDefaultItemIds.Contains(itemId);
    }

    public static void RegisterDefaults(IEnumerable<ShopItemDefinition> definitions)
    {
        if (definitions == null)
        {
            return;
        }

        foreach (ShopItemDefinition definition in definitions)
        {
            if (definition == null)
            {
                continue;
            }

            if (definition.UnlockedByDefault || definition.ItemId == AlwaysUnlockedItemId)
            {
                RegisteredDefaultItemIds.Add(definition.ItemId);
            }
        }
    }

    public static bool Unlock(ShopItemDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        RegisterDefaults(new[] { definition });
        return Unlock(definition.ItemId);
    }

    public static bool Unlock(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (!UnlockedItemIds.Add(itemId))
        {
            return false;
        }

        ItemUnlocked?.Invoke(itemId);
        return true;
    }

    public static void ResetRuntimeState()
    {
        UnlockedItemIds.Clear();
        RegisteredDefaultItemIds.Clear();
    }
}
