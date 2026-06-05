using UnityEngine;
using TMPro;

public class SpiritStonesDisplay : MonoBehaviour
{
    [SerializeField]
    private int defaultStartingMoney = 1000;

    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (!ShopWallet.IsInitialized)
        {
            ShopWallet.InitializeIfNeeded(defaultStartingMoney);
        }
        UpdateText(ShopWallet.CurrentMoney);
    }

    private void OnEnable()
    {
        ShopWallet.MoneyChanged += OnMoneyChanged;
    }

    private void OnDisable()
    {
        ShopWallet.MoneyChanged -= OnMoneyChanged;
    }

    private void OnMoneyChanged(int amount)
    {
        UpdateText(amount);
    }

    private void UpdateText(int amount)
    {
        if (textComponent != null)
        {
            textComponent.text = $"灵石: {amount}";
        }
    }
}
