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

    private void Awake()
    {
        AutoBind();
        ShopWallet.InitializeIfNeeded(startingMoney);
        RefreshMoneyText(ShopWallet.CurrentMoney);
        RefreshRoundText(GetCurrentRoundValue());
    }

    private void OnEnable()
    {
        ShopWallet.MoneyChanged += HandleMoneyChanged;
        SubscribeScheduler();
        RefreshMoneyText(ShopWallet.CurrentMoney);
        RefreshRoundText(GetCurrentRoundValue());
    }

    private void OnDisable()
    {
        ShopWallet.MoneyChanged -= HandleMoneyChanged;
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

    private void HandleRoundChanged(int currentRound)
    {
        RefreshRoundText(currentRound);
    }

    private void RefreshMoneyText(int currentMoney)
    {
        if (moneyText == null)
        {
            return;
        }

        moneyText.text = $"{currentMoney}灵石";
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
