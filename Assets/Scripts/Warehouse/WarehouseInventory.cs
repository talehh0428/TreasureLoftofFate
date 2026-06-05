using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WarehouseInventory
{
    public static event Action Changed;

    private static readonly Dictionary<string, WarehouseItemStack> ItemsById =
        new Dictionary<string, WarehouseItemStack>();

    public static int StackCount => ItemsById.Count;

    public static bool Add(ShopItemDefinition definition, int quantity = 1)
    {
        if (definition == null || quantity <= 0)
        {
            return false;
        }

        string itemId = definition.ItemId;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        int currentQuantity = ItemsById.TryGetValue(itemId, out WarehouseItemStack existingStack)
            ? existingStack.Quantity
            : 0;

        ItemsById[itemId] = new WarehouseItemStack(definition, currentQuantity + quantity);
        Changed?.Invoke();
        return true;
    }

    public static bool TryGet(string itemId, out WarehouseItemStack stack)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            stack = default;
            return false;
        }

        return ItemsById.TryGetValue(itemId, out stack);
    }

    public static bool Remove(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return false;
        }

        if (!ItemsById.TryGetValue(itemId, out WarehouseItemStack existingStack))
        {
            return false;
        }

        int newQuantity = existingStack.Quantity - quantity;
        if (newQuantity <= 0)
        {
            ItemsById.Remove(itemId);
        }
        else
        {
            ItemsById[itemId] = new WarehouseItemStack(existingStack.Definition, newQuantity);
        }

        Changed?.Invoke();
        return true;
    }

    public static int GetQuantity(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        return ItemsById.TryGetValue(itemId, out WarehouseItemStack stack) ? stack.Quantity : 0;
    }

    public static IReadOnlyList<WarehouseItemStack> GetStacks()
    {
        return ItemsById.Values
            .Where(stack => stack.Definition != null && stack.Quantity > 0)
            .OrderBy(stack => stack.ItemId)
            .ToList();
    }

    public static void ResetRuntimeState()
    {
        ItemsById.Clear();
        Changed?.Invoke();
    }
}
