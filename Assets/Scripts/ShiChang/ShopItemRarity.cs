/*
 稀有度枚举和中文显示名
 */

public enum ShopItemRarity
{
    Common = 0,
    Fine = 1,
    Superior = 2,
    Epic = 3,
    Immortal = 4,
}

public static class ShopItemRarityExtensions
{
    public static string ToDisplayName(this ShopItemRarity rarity)
    {
        switch (rarity)
        {
            case ShopItemRarity.Common:
                return "\u51e1\u54c1";
            case ShopItemRarity.Fine:
                return "\u826f\u54c1";
            case ShopItemRarity.Superior:
                return "\u4e0a\u54c1";
            case ShopItemRarity.Epic:
                return "\u6781\u54c1";
            case ShopItemRarity.Immortal:
                return "\u4ed9\u54c1";
            default:
                return rarity.ToString();
        }
    }
}