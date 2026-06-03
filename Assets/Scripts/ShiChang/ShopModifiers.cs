public interface IShopDiscountModifier
{
    void ModifyDiscountSettings(ShopDiscountSettings settings);
}

public interface IShopRarityWeightModifier
{
    float ModifyWeight(ShopItemDefinition itemDefinition, float currentWeight);
}