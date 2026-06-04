using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarehouseDetailPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text priceText;

    private const string CurrencyLabel = "灵石";

    private void Awake()
    {
        AutoBind();
        ShowEmpty();
    }

    public void Show(WarehouseItemStack stack)
    {
        AutoBind();

        if (stack.Definition == null)
        {
            ShowEmpty();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = stack.Icon;
            itemIcon.enabled = stack.Icon != null;
        }

        SetText(itemNameText, stack.DisplayName);
        SetText(attackText, $"攻击—{stack.Attack}");
        SetText(defenseText, $"防御—{stack.Defense}");
        SetText(speedText, $"遁速—{stack.MovementSpeed}");
        SetText(priceText, $"{stack.Price}{CurrencyLabel}");
    }

    public void ShowEmpty()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        SetText(itemNameText, string.Empty);
        SetText(attackText, string.Empty);
        SetText(defenseText, string.Empty);
        SetText(speedText, string.Empty);
        SetText(priceText, string.Empty);
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

        if (priceText == null)
        {
            priceText = FindChildComponent<TMP_Text>("DetailPrice");
        }
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<T>();
    }
}
