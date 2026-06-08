using UnityEngine;

public readonly struct GuideBookEntryData
{
    public GuideBookEntryData(ShopItemDefinition definition, bool isUnlocked)
    {
        Definition = definition;
        NpcDefinition = null;
        IsUnlocked = isUnlocked;
    }

    public GuideBookEntryData(NPCDefinition definition)
    {
        Definition = null;
        NpcDefinition = definition;
        IsUnlocked = true;
    }

    public ShopItemDefinition Definition { get; }
    public NPCDefinition NpcDefinition { get; }
    public bool IsUnlocked { get; }
    public bool IsNpc => NpcDefinition != null;
    public string ItemId => Definition != null ? Definition.ItemId : NpcDefinition == null ? string.Empty : NpcDefinition.NpcId;
    public string DisplayName => Definition != null ? Definition.DisplayName : NpcDefinition == null ? string.Empty : NpcDefinition.DisplayName;
    public string Description => Definition != null ? Definition.Description : NpcDefinition == null ? string.Empty : NpcDefinition.Description;
    public Sprite Icon => Definition != null ? Definition.Icon : NpcDefinition == null ? null : NpcDefinition.Avatar;
    public ShopItemRarity Rarity => Definition == null ? ShopItemRarity.Common : Definition.Rarity;
    public int Price => Definition == null ? 0 : Definition.Price;
    public int Attack => Definition != null ? Definition.Attack : NpcDefinition == null ? 0 : NpcDefinition.Attack;
    public int Defense => Definition != null ? Definition.Defense : NpcDefinition == null ? 0 : NpcDefinition.Defense;
    public int MovementSpeed => Definition != null ? Definition.MovementSpeed : NpcDefinition == null ? 0 : NpcDefinition.MovementSpeed;
}
