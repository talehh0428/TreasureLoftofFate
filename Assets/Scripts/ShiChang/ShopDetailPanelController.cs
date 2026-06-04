using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopDetailPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text speedText;

    private const string CurrencyLabel = "\u7075\u77f3";

    private void Awake()
    {
        AutoBind();
        ShowEmptyState();
    }

    private void OnEnable()
    {
        ShopEvents.ItemSelected += HandleItemSelected;
        ShopEvents.ItemSelectionCleared += HandleItemSelectionCleared;
    }

    private void OnDisable()
    {
        ShopEvents.ItemSelected -= HandleItemSelected;
        ShopEvents.ItemSelectionCleared -= HandleItemSelectionCleared;
    }

    private void HandleItemSelected(ShopItemInstance itemInstance)
    {
        if (itemInstance == null)
        {
            ShowEmptyState();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = itemInstance.Icon;
            itemIcon.enabled = itemInstance.Icon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = itemInstance.DisplayName;
        }

        if (rarityText != null)
        {
            rarityText.text = itemInstance.Rarity.ToDisplayName();
        }

        if (priceText != null)
        {
            priceText.text = $"{itemInstance.FinalPrice}{CurrencyLabel}";
        }

        if (attackText != null)
        {
            attackText.text = $"\u653b\u51fb\u2014{itemInstance.Attack}";
        }

        if (defenseText != null)
        {
            defenseText.text = $"\u9632\u5fa1\u2014{itemInstance.Defense}";
        }

        if (speedText != null)
        {
            speedText.text = $"\u9041\u901f\u2014{itemInstance.MovementSpeed}";
        }

    }

    private void HandleItemSelectionCleared()
    {
        ShowEmptyState();
    }

    private void ShowEmptyState()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (itemNameText != null)
        {
            itemNameText.text = string.Empty;
        }

        if (rarityText != null)
        {
            rarityText.text = string.Empty;
        }

        if (priceText != null)
        {
            priceText.text = string.Empty;
        }

        if (attackText != null)
        {
            attackText.text = string.Empty;
        }

        if (defenseText != null)
        {
            defenseText.text = string.Empty;
        }

        if (speedText != null)
        {
            speedText.text = string.Empty;
        }

    }

    private void AutoBind()
    {
        if (itemIcon == null)
        {
            itemIcon = FindChildComponent<Image>("DetailIcon");
        }

        if (itemNameText == null)
        {
            itemNameText = FindChildComponent<TMP_Text>("DetailName");
        }

        if (rarityText == null)
        {
            rarityText = FindChildComponent<TMP_Text>("DetailRarity");
        }

        if (priceText == null)
        {
            priceText = FindChildComponent<TMP_Text>("DetailPrice");
        }

        if (attackText == null)
        {
            attackText = FindChildComponent<TMP_Text>("DetailAttack");
        }

        if (defenseText == null)
        {
            defenseText = FindChildComponent<TMP_Text>("DetailDefense");
        }

        if (speedText == null)
        {
            speedText = FindChildComponent<TMP_Text>("DetailSpeed");
        }

    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<T>();
    }
}
