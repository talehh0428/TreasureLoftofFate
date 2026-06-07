using UnityEngine;

public readonly struct TradeOfferStack
{
    public TradeOfferStack(ShopItemDefinition definition, int warehouseQuantity, int offerQuantity)
    {
        Definition = definition;
        WarehouseQuantity = Mathf.Max(0, warehouseQuantity);
        OfferQuantity = Mathf.Max(0, offerQuantity);
    }

    public ShopItemDefinition Definition { get; }
    public int WarehouseQuantity { get; }
    public int OfferQuantity { get; }
    public string ItemId => Definition == null ? string.Empty : Definition.ItemId;
    public string DisplayName => Definition == null ? string.Empty : Definition.DisplayName;
    public Sprite Icon => Definition == null ? null : Definition.Icon;
    public ShopItemRarity Rarity => Definition == null ? ShopItemRarity.Common : Definition.Rarity;
    public int Price => Definition == null ? 0 : Definition.Price;
    public int Attack => Definition == null ? 0 : Definition.Attack;
    public int Defense => Definition == null ? 0 : Definition.Defense;
    public int MovementSpeed => Definition == null ? 0 : Definition.MovementSpeed;
}
