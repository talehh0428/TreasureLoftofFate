using System;

public static class ShopEvents
{
    public static event Action<ShopItemInstance> ItemSelected;
    public static event Action ItemSelectionCleared;
    public static event Action<ShopItemInstance> ItemSold;

    public static void RaiseItemSelected(ShopItemInstance itemInstance)
    {
        ItemSelected?.Invoke(itemInstance);
    }

    public static void RaiseItemSelectionCleared()
    {
        ItemSelectionCleared?.Invoke();
    }

    public static void RaiseItemSold(ShopItemInstance itemInstance)
    {
        ItemSold?.Invoke(itemInstance);
    }
}