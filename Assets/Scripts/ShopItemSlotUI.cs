using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ShopItemSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image rarityBorder;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemPriceText;
    [SerializeField] private TMP_Text discountText;

    [Header("Selection Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.88f, 0.45f, 1f);
    [SerializeField] private Color soldBackgroundColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color soldContentColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Rarity Colors")]
    [SerializeField] private Color commonRarityColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color fineRarityColor = new Color(0.3f, 0.85f, 0.35f, 1f);
    [SerializeField] private Color superiorRarityColor = new Color(0.62f, 0.35f, 0.9f, 1f);
    [SerializeField] private Color epicRarityColor = new Color(1f, 0.82f, 0.18f, 1f);
    [SerializeField] private Color immortalRarityColor = new Color(0.88f, 0.18f, 0.18f, 1f);

    private ShopItemInstance currentItem;
    private Color iconNormalColor = Color.white;
    private Color itemNameNormalColor = Color.white;
    private Color itemPriceNormalColor = Color.white;
    private Color discountNormalColor = Color.white;

    public event Action<ShopItemSlotUI> Clicked;

    public ShopItemInstance CurrentItem => currentItem;
    public bool HasItem => currentItem != null;
    public bool IsSelected { get; private set; }
    public bool IsSold => currentItem != null && currentItem.IsSold;
    public int Price => currentItem == null ? 0 : currentItem.FinalPrice;

    private void Awake()
    {
        AutoBind();
        CacheContentColors();
        ApplySelectionVisual();
    }

    private void Reset()
    {
        AutoBind();
        CacheContentColors();
        ApplySelectionVisual();
    }

    private void OnValidate()
    {
        AutoBind();
        CacheContentColors();
        ApplySelectionVisual();
    }

    public void Setup(ShopItemInstance itemInstance)
    {
        AutoBind();
        currentItem = itemInstance;
        IsSelected = false;

        if (currentItem == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = currentItem.Icon;
            itemIcon.enabled = currentItem.Icon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = currentItem.DisplayName;
        }

        if (itemPriceText != null)
        {
            itemPriceText.text = $"{currentItem.FinalPrice}\u7075\u77f3";
        }

        if (discountText != null)
        {
            discountText.text = currentItem.DiscountLabel;
        }

        ApplySelectionVisual();
        gameObject.SetActive(true);
    }

    public void SetSelected(bool selected)
    {
        if (!HasItem)
        {
            IsSelected = false;
            ApplySelectionVisual();
            return;
        }

        if (IsSold)
        {
            IsSelected = false;
            ApplySelectionVisual();
            return;
        }

        IsSelected = selected;
        ApplySelectionVisual();
    }

    public void MarkAsSold()
    {
        if (!HasItem)
        {
            return;
        }

        currentItem.MarkAsSold();
        IsSelected = false;
        ApplySelectionVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasItem || IsSold)
        {
            return;
        }

        SetSelected(!IsSelected);
        Clicked?.Invoke(this);
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

        if (rarityBorder == null)
        {
            rarityBorder = FindChildComponent<Image>("RarityBorder");
        }

        if (itemNameText == null)
        {
            itemNameText = FindChildComponent<TMP_Text>("ItemName");
        }

        if (itemPriceText == null)
        {
            itemPriceText = FindChildComponent<TMP_Text>("ItemPrice");
        }

        if (discountText == null)
        {
            discountText = FindChildComponent<TMP_Text>("DiscountText");
        }
    }

    private void ApplySelectionVisual()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = IsSold
                ? soldBackgroundColor
                : (IsSelected ? selectedColor : normalColor);
        }

        if (itemIcon != null)
        {
            itemIcon.color = IsSold ? soldContentColor : iconNormalColor;
        }

        if (itemNameText != null)
        {
            itemNameText.color = IsSold ? soldContentColor : itemNameNormalColor;
        }

        if (itemPriceText != null)
        {
            itemPriceText.color = IsSold ? soldContentColor : itemPriceNormalColor;
        }

        if (discountText != null)
        {
            discountText.color = IsSold ? soldContentColor : discountNormalColor;
        }

        if (rarityBorder != null)
        {
            rarityBorder.color = IsSold ? soldContentColor : GetRarityColor();
        }
    }

    private Color GetRarityColor()
    {
        if (currentItem == null)
        {
            return commonRarityColor;
        }

        switch (currentItem.Rarity)
        {
            case ShopItemRarity.Common:
                return commonRarityColor;
            case ShopItemRarity.Fine:
                return fineRarityColor;
            case ShopItemRarity.Superior:
                return superiorRarityColor;
            case ShopItemRarity.Epic:
                return epicRarityColor;
            case ShopItemRarity.Immortal:
                return immortalRarityColor;
            default:
                return commonRarityColor;
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

    private void CacheContentColors()
    {
        if (itemIcon != null)
        {
            iconNormalColor = itemIcon.color;
        }

        if (itemNameText != null)
        {
            itemNameNormalColor = itemNameText.color;
        }

        if (itemPriceText != null)
        {
            itemPriceNormalColor = itemPriceText.color;
        }

        if (discountText != null)
        {
            discountNormalColor = discountText.color;
        }
    }
}