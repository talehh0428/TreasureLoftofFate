using System;
using UnityEngine;

public static class ShopWallet
{
    public static event Action<int> MoneyChanged;

    private static bool isInitialized;
    private static int currentMoney;

    public static bool IsInitialized => isInitialized;

    public static int CurrentMoney => currentMoney;

    public static void InitializeIfNeeded(int startingMoney)
    {
        if (isInitialized)
        {
            return;
        }

        SetMoney(startingMoney);
    }

    public static void SetMoney(int amount)
    {
        currentMoney = Mathf.Max(0, amount);
        isInitialized = true;
        MoneyChanged?.Invoke(currentMoney);
    }

    public static void Reset()
    {
        currentMoney = 0;
        isInitialized = false;
        MoneyChanged?.Invoke(currentMoney);
    }

    public static void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetMoney(currentMoney + amount);
    }

    public static bool CanAfford(int amount)
    {
        if (amount < 0)
        {
            return false;
        }

        return currentMoney >= amount;
    }

    public static bool TrySpend(int amount)
    {
        if (amount < 0)
        {
            return false;
        }

        if (!CanAfford(amount))
        {
            return false;
        }

        SetMoney(currentMoney - amount);
        return true;
    }
}
