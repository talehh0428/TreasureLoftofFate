using System.Collections.Generic;
using UnityEngine;

public static class ShopGenerationUtility
{
    public static float SampleTruncatedExponential(ShopDiscountSettings settings)
    {
        if (settings == null)
        {
            return 0f;
        }

        settings.Validate();

        float min = settings.MinimumDiscount;
        float max = settings.MaximumDiscount;
        float lambda = settings.Lambda;

        if (Mathf.Approximately(max, min))
        {
            return RoundDiscount(min);
        }

        if (lambda <= 0.01f)
        {
            return RoundDiscount(Random.Range(min, max));
        }

        float u = Random.Range(0f, 1f);
        float expTerm = Mathf.Exp(-lambda * (max - min));
        float sampled = min - Mathf.Log(1f - u * (1f - expTerm)) / lambda;

        return RoundDiscount(Mathf.Clamp(sampled, min, max));
    }

    public static ShopItemDefinition DrawWeightedItem(
        IReadOnlyList<ShopItemDefinition> catalog,
        ShopRarityWeightTable weightTable,
        IReadOnlyList<IShopRarityWeightModifier> weightModifiers)
    {
        if (catalog == null || catalog.Count == 0 || weightTable == null)
        {
            return null;
        }

        float totalWeight = 0f;
        float[] weights = new float[catalog.Count];

        for (int index = 0; index < catalog.Count; index++)
        {
            ShopItemDefinition item = catalog[index];
            float weight = item == null ? 0f : weightTable.GetWeight(item.Rarity);

            if (weightModifiers != null)
            {
                for (int modifierIndex = 0; modifierIndex < weightModifiers.Count; modifierIndex++)
                {
                    IShopRarityWeightModifier modifier = weightModifiers[modifierIndex];
                    if (modifier == null)
                    {
                        continue;
                    }

                    weight = modifier.ModifyWeight(item, weight);
                }
            }

            weight = Mathf.Max(0f, weight);
            weights[index] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        for (int index = 0; index < catalog.Count; index++)
        {
            cumulativeWeight += weights[index];
            if (randomValue <= cumulativeWeight)
            {
                return catalog[index];
            }
        }

        return catalog[catalog.Count - 1];
    }

    private static float RoundDiscount(float discount)
    {
        return Mathf.Round(discount * 100f) / 100f;
    }
}