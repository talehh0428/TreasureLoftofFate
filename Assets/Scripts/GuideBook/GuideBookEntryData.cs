using UnityEngine;

public readonly struct GuideBookEntryData
{
    public GuideBookEntryData(ShopItemDefinition definition, bool isUnlocked)
    {
        Definition = definition;
        IsUnlocked = isUnlocked;
    }

    public ShopItemDefinition Definition { get; }
    public bool IsUnlocked { get; }
    public string ItemId => Definition == null ? string.Empty : Definition.ItemId;
    public string DisplayName => Definition == null ? string.Empty : Definition.DisplayName;
    public string Description => Definition == null ? string.Empty : Definition.Description;
    public Sprite Icon => Definition == null ? null : Definition.Icon;
    public ShopItemRarity Rarity => Definition == null ? ShopItemRarity.Common : Definition.Rarity;
    public int Price => Definition == null ? 0 : Definition.Price;
    public int Attack => Definition == null ? 0 : Definition.Attack;
    public int Defense => Definition == null ? 0 : Definition.Defense;
    public int MovementSpeed => Definition == null ? 0 : Definition.MovementSpeed;
}
