/*
 单次上架实例，保存折扣和折后价
 */

using UnityEngine;

public class ShopItemInstance
{
    public ShopItemInstance(ShopItemDefinition definition, float discountRate)
    {
        Definition = definition;
        DiscountRate = Mathf.Clamp01(discountRate);
        FinalPrice = CalculateFinalPrice(definition == null ? 0 : definition.Price, DiscountRate);
    }

    public ShopItemDefinition Definition { get; }
    public float DiscountRate { get; }
    public int FinalPrice { get; }
    public bool IsSold { get; private set; }
    public string DisplayName => Definition == null ? string.Empty : Definition.DisplayName;
    public string Description => Definition == null ? string.Empty : Definition.Description;
    public Sprite Icon => Definition == null ? null : Definition.Icon;
    public ShopItemRarity Rarity => Definition == null ? ShopItemRarity.Common : Definition.Rarity;
    public int OriginalPrice => Definition == null ? 0 : Definition.Price;
    public int Attack => Definition == null ? 0 : Definition.Attack;
    public int Defense => Definition == null ? 0 : Definition.Defense;
    public int MovementSpeed => Definition == null ? 0 : Definition.MovementSpeed;
    public string DiscountLabel => $"-{Mathf.RoundToInt(DiscountRate * 100f)}%";

    public void MarkAsSold()
    {
        IsSold = true;
    }

    private static int CalculateFinalPrice(int originalPrice, float discountRate)
    {
        if (originalPrice <= 0)
        {
            return 0;
        }

        float discountedValue = originalPrice * (1f - discountRate);
        return Mathf.Max(1, Mathf.CeilToInt(discountedValue));
    }
}