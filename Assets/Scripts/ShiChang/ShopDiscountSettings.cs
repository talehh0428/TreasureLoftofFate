/*
 折扣分布参数，可调上下限和衰减系数
 */


using System;
using UnityEngine;

[Serializable]
public class ShopDiscountSettings
{
    [SerializeField] [Range(0f, 0.99f)] private float minimumDiscount = 0.05f;
    [SerializeField] [Range(0f, 0.99f)] private float maximumDiscount = 0.80f;
    [SerializeField] [Min(0.01f)] private float lambda = 4f;
    [SerializeField] [Range(0f, 0.99f)] private float guaranteedDiscount = 0.99f;

    public float MinimumDiscount => minimumDiscount;
    public float MaximumDiscount => maximumDiscount;
    public float Lambda => lambda;
    public float GuaranteedDiscount => guaranteedDiscount;

    public ShopDiscountSettings Clone()
    {
        ShopDiscountSettings clone = new ShopDiscountSettings();
        clone.minimumDiscount = minimumDiscount;
        clone.maximumDiscount = maximumDiscount;
        clone.lambda = lambda;
        clone.guaranteedDiscount = guaranteedDiscount;
        return clone;
    }

    public void SetMinimumDiscount(float value)
    {
        minimumDiscount = Mathf.Clamp(value, 0f, 0.99f);
    }

    public void SetMaximumDiscount(float value)
    {
        maximumDiscount = Mathf.Clamp(value, 0f, 0.99f);
    }

    public void SetLambda(float value)
    {
        lambda = Mathf.Max(0.01f, value);
    }

    public void SetGuaranteedDiscount(float value)
    {
        guaranteedDiscount = Mathf.Clamp(value, 0f, 0.99f);
    }

    public void Validate()
    {
        minimumDiscount = Mathf.Clamp(minimumDiscount, 0f, 0.99f);
        maximumDiscount = Mathf.Clamp(maximumDiscount, minimumDiscount, 0.99f);
        lambda = Mathf.Max(0.01f, lambda);
        guaranteedDiscount = Mathf.Clamp(guaranteedDiscount, 0f, 0.99f);
    }
}