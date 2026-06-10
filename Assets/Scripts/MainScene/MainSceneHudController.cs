using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class MainSceneHudController : MonoBehaviour
{
    [Header("State")]
    [SerializeField] [Min(0)] private int startingMoney = 1000;
    [SerializeField] private NPCEventScheduler roundScheduler;

    [Header("UI References")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text moneyText;

    private bool isSubscribedToScheduler;
    private int previewWalletDelta;
    private static bool hasInitializedNewGameState;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetNewGameStateInitializationFlag()
    {
        hasInitializedNewGameState = false;
    }

    private void Awake()
    {
        AutoBind();
        InitializeNewGameStateIfNeeded();
        RefreshMoneyText(ShopWallet.CurrentMoney);
        RefreshRoundText(GetCurrentRoundValue());
    }

    private void OnEnable()
    {
        ShopWallet.MoneyChanged += HandleMoneyChanged;
        ShopEvents.WalletPreviewChanged += HandleWalletPreviewChanged;
        SubscribeScheduler();
        RefreshMoneyText(ShopWallet.CurrentMoney);
        RefreshRoundText(GetCurrentRoundValue());
    }

    private void OnDisable()
    {
        ShopWallet.MoneyChanged -= HandleMoneyChanged;
        ShopEvents.WalletPreviewChanged -= HandleWalletPreviewChanged;
        UnsubscribeScheduler();
    }

    private void OnValidate()
    {
        AutoBind();
    }

    private void HandleMoneyChanged(int currentMoney)
    {
        RefreshMoneyText(currentMoney);
    }

    private void HandleWalletPreviewChanged(int previewDelta)
    {
        previewWalletDelta = previewDelta;
        RefreshMoneyText(ShopWallet.CurrentMoney);
    }

    private void HandleRoundChanged(int currentRound)
    {
        RefreshRoundText(currentRound);
    }

    private void InitializeNewGameStateIfNeeded()
    {
        if (hasInitializedNewGameState)
        {
            ShopWallet.InitializeIfNeeded(startingMoney);
            return;
        }

        WarehouseInventory.ResetRuntimeState();
        ShopItemUnlockRegistry.ResetRuntimeState();
        ShopWallet.SetMoney(startingMoney);
        hasInitializedNewGameState = true;
    }

    private void RefreshMoneyText(int currentMoney)
    {
        if (moneyText == null)
        {
            return;
        }

        if (previewWalletDelta == 0)
        {
            moneyText.text = $"{currentMoney}";
            return;
        }

        int previewMoney = Mathf.Max(0, currentMoney + previewWalletDelta);
        moneyText.text = $"{currentMoney}\uff08{previewMoney}\uff09";
    }

    private void RefreshRoundText(int currentRound)
    {
        if (roundText == null)
        {
            return;
        }

        roundText.text = $"第{Mathf.Max(1, currentRound)}回合";
    }

    private int GetCurrentRoundValue()
    {
        return roundScheduler == null ? 1 : roundScheduler.CurrentRound;
    }

    private void SubscribeScheduler()
    {
        if (roundScheduler == null || isSubscribedToScheduler)
        {
            return;
        }

        roundScheduler.RoundChanged += HandleRoundChanged;
        isSubscribedToScheduler = true;
    }

    private void UnsubscribeScheduler()
    {
        if (roundScheduler == null || !isSubscribedToScheduler)
        {
            return;
        }

        roundScheduler.RoundChanged -= HandleRoundChanged;
        isSubscribedToScheduler = false;
    }

    private void AutoBind()
    {
        if (roundScheduler == null)
        {
            roundScheduler = FindObjectOfType<NPCEventScheduler>(true);
        }
    }
}
