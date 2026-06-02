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
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemPriceText;

    [Header("Selection Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.88f, 0.45f, 1f);
    [SerializeField] private Color soldBackgroundColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color soldContentColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    private ShopItemDefinition currentItem;
    private Color iconNormalColor = Color.white;
    private Color itemNameNormalColor = Color.white;
    private Color itemPriceNormalColor = Color.white;

    public event Action<ShopItemSlotUI> Clicked;

    public ShopItemDefinition CurrentItem => currentItem;

    public bool HasItem => currentItem != null;

    public bool IsSelected { get; private set; }

    public bool IsSold { get; private set; }

    public int Price => currentItem == null ? 0 : currentItem.Price;

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

    public void Setup(ShopItemDefinition itemDefinition)
    {
        AutoBind();
        currentItem = itemDefinition;
        IsSelected = false;
        IsSold = false;

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
            itemPriceText.text = $"{currentItem.Price}¡È Ø";
        }

        ApplySelectionVisual();
        gameObject.SetActive(true);
    }

    public void ClearSlot()
    {
        currentItem = null;
        IsSelected = false;
        IsSold = false;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (itemNameText != null)
        {
            itemNameText.text = string.Empty;
        }

        if (itemPriceText != null)
        {
            itemPriceText.text = string.Empty;
        }

        ApplySelectionVisual();
        gameObject.SetActive(false);
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

        IsSold = true;
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

        if (itemNameText == null)
        {
            itemNameText = FindChildComponent<TMP_Text>("ItemName");
        }

        if (itemPriceText == null)
        {
            itemPriceText = FindChildComponent<TMP_Text>("ItemPrice");
        }
    }

    private void ApplySelectionVisual()
    {
        if (backgroundImage != null)
        {
            if (IsSold)
            {
                backgroundImage.color = soldBackgroundColor;
            }
            else
            {
                backgroundImage.color = IsSelected ? selectedColor : normalColor;
            }
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
    }
}
