using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TradeItemSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemPriceText;
    [SerializeField] private TMP_Text itemQuantityText;

    private WarehouseItemStack currentStack;
    private int sellPrice;
    private bool isSelected;
    private Color normalColor;

    /// <summary>选中状态变化时触发，供外部更新预览</summary>
    public event Action SelectionChanged;

    public WarehouseItemStack CurrentStack => currentStack;
    public bool HasItem => currentStack.Definition != null && currentStack.Quantity > 0;
    public int SellPrice => sellPrice;
    public bool IsSelected => isSelected;

    private void Awake()
    {
        AutoBind();
        CacheNormalColor();
    }

    private void Reset()
    {
        AutoBind();
    }

    private void OnValidate()
    {
        AutoBind();
    }

    public void Setup(WarehouseItemStack stack, int price)
    {
        AutoBind();
        currentStack = stack;
        sellPrice = price;

        if (!HasItem)
        {
            gameObject.SetActive(false);
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = stack.Icon;
            itemIcon.enabled = stack.Icon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = stack.DisplayName;
        }

        if (itemPriceText != null)
        {
            itemPriceText.text = $"售价: {price} 灵石";
        }

        if (itemQuantityText != null)
        {
            itemQuantityText.text = stack.Quantity.ToString();
        }

        SetSelected(false);
        gameObject.SetActive(true);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (backgroundImage != null)
        {
            backgroundImage.color = selected
                ? new Color(0.35f, 0.25f, 0.1f, 0.95f)
                : normalColor;
        }

        SelectionChanged?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasItem) return;

        SetSelected(!isSelected);
    }

    private void CacheNormalColor()
    {
        if (backgroundImage != null)
        {
            normalColor = backgroundImage.color;
        }
    }

    private void AutoBind()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (itemIcon == null)
        {
            itemIcon = FindChildComponent<Image>("ItemIcon");
        }

        if (itemNameText == null)
        {
            itemNameText = FindChildComponent<TMP_Text>("ItemName");
        }

        if (itemPriceText == null)
        {
            itemPriceText = FindChildComponent<TMP_Text>("ItemPrice");
        }

        if (itemQuantityText == null)
        {
            // 优先匹配直接子节点 ItemQuantity/QuantityText
            // 如果不存在，尝试在 QuantityBar 下查找（层级结构）
            itemQuantityText = FindChildComponent<TMP_Text>("ItemQuantity")
                            ?? FindChildComponent<TMP_Text>("QuantityText")
                            ?? FindNestedComponent<TMP_Text>("QuantityBar/QuantityText");
        }
    }

    /// <summary>查找直接子节点中的组件</summary>
    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<T>();
    }

    /// <summary>按路径查找嵌套子节点中的组件（如 "QuantityBar/QuantityText"）</summary>
    private T FindNestedComponent<T>(string childPath) where T : Component
    {
        Transform child = transform.Find(childPath);
        return child == null ? null : child.GetComponent<T>();
    }
}
