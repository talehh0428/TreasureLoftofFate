using UnityEngine;

public readonly struct WarehouseItemStack
{
    public WarehouseItemStack(ShopItemDefinition definition, int quantity)
    {
        Definition = definition;
        Quantity = Mathf.Max(0, quantity);
    }

    public ShopItemDefinition Definition { get; }
    public int Quantity { get; }
    public string ItemId => Definition == null ? string.Empty : Definition.ItemId;
    public string DisplayName => Definition == null ? string.Empty : Definition.DisplayName;
    public Sprite Icon => Definition == null ? null : Definition.Icon;
    public ShopItemRarity Rarity => Definition == null ? ShopItemRarity.Common : Definition.Rarity;
    public int Price => Definition == null ? 0 : Definition.Price;
    public int Attack => Definition == null ? 0 : Definition.Attack;
    public int Defense => Definition == null ? 0 : Definition.Defense;
    public int MovementSpeed => Definition == null ? 0 : Definition.MovementSpeed;
}
