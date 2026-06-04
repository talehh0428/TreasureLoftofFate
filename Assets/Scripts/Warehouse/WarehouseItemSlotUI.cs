using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class WarehouseItemSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text quantityText;

    [Header("Selection")]
    [SerializeField] private Color selectedOverlayColor = new Color(1f, 0.9f, 0.45f, 1f);

    [Header("Rarity Backgrounds")]
    [SerializeField] private Color commonColor = new Color(0.55f, 0.58f, 0.62f, 1f);
    [SerializeField] private Color fineColor = new Color(0.32f, 0.62f, 0.48f, 1f);
    [SerializeField] private Color superiorColor = new Color(0.28f, 0.52f, 0.72f, 1f);
    [SerializeField] private Color epicColor = new Color(0.62f, 0.48f, 0.78f, 1f);
    [SerializeField] private Color immortalColor = new Color(0.84f, 0.58f, 0.24f, 1f);

    private WarehouseItemStack currentStack;

    public event Action<WarehouseItemSlotUI> Clicked;

    public WarehouseItemStack CurrentStack => currentStack;
    public bool HasItem => currentStack.Definition != null && currentStack.Quantity > 0;
    public bool IsSelected { get; private set; }

    private void Awake()
    {
        AutoBind();
        ApplyVisual();
    }

    private void Reset()
    {
        AutoBind();
        ApplyVisual();
    }

    private void OnValidate()
    {
        AutoBind();
        ApplyVisual();
    }

    public void Setup(WarehouseItemStack stack)
    {
        AutoBind();
        currentStack = stack;
        IsSelected = false;

        if (!HasItem)
        {
            gameObject.SetActive(false);
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = currentStack.Icon;
            itemIcon.enabled = currentStack.Icon != null;
        }

        if (quantityText != null)
        {
            quantityText.text = currentStack.Quantity.ToString();
        }

        ApplyVisual();
        gameObject.SetActive(true);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected && HasItem;
        ApplyVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasItem)
        {
            return;
        }

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

        if (quantityText == null)
        {
            quantityText = FindChildComponent<TMP_Text>("QuantityText");
        }
    }

    private void ApplyVisual()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = IsSelected ? selectedOverlayColor : GetRarityColor();
        }
    }

    private Color GetRarityColor()
    {
        switch (currentStack.Rarity)
        {
            case ShopItemRarity.Fine:
                return fineColor;
            case ShopItemRarity.Superior:
                return superiorColor;
            case ShopItemRarity.Epic:
                return epicColor;
            case ShopItemRarity.Immortal:
                return immortalColor;
            default:
                return commonColor;
        }
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<T>();
    }
}
