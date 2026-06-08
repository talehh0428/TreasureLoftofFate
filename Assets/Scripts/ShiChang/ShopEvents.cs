using System;

public static class ShopEvents
{
    public static event Action<ShopItemInstance> ItemSelected;
    public static event Action ItemSelectionCleared;
    public static event Action<ShopItemInstance> ItemSold;
    public static event Action<int> PurchasePreviewChanged;
    public static event Action<int> WalletPreviewChanged;

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

    public static void RaisePurchasePreviewChanged(int selectedTotalPrice)
    {
        int price = Math.Max(0, selectedTotalPrice);
        PurchasePreviewChanged?.Invoke(price);
        RaiseWalletPreviewChanged(-price);
    }

    public static void RaiseWalletPreviewChanged(int previewDelta)
    {
        WalletPreviewChanged?.Invoke(previewDelta);
    }
}
