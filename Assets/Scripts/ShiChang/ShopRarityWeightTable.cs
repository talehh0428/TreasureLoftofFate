using System;
using UnityEngine;

[Serializable]
public class ShopRarityWeightTable
{
    [SerializeField] [Min(0f)] private float commonWeight = 50f;
    [SerializeField] [Min(0f)] private float fineWeight = 30f;
    [SerializeField] [Min(0f)] private float superiorWeight = 15f;
    [SerializeField] [Min(0f)] private float epicWeight = 5f;
    [SerializeField] [Min(0f)] private float immortalWeight;

    public float GetWeight(ShopItemRarity rarity)
    {
        switch (rarity)
        {
            case ShopItemRarity.Common:
                return commonWeight;
            case ShopItemRarity.Fine:
                return fineWeight;
            case ShopItemRarity.Superior:
                return superiorWeight;
            case ShopItemRarity.Epic:
                return epicWeight;
            case ShopItemRarity.Immortal:
                return immortalWeight;
            default:
                return 0f;
        }
    }
}