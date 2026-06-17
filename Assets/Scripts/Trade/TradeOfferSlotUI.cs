using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TradeOfferSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image quantityBarImage;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Button decreaseButton;

    [Header("Display")]
    [SerializeField] private Color quantityBarColor = new Color(0.92f, 0.9f, 0.84f, 1f);

    [Header("Rarity Backgrounds")]
    [SerializeField] private Color commonColor = new Color(216f / 255f, 210f / 255f, 196f / 255f, 1f);
    [SerializeField] private Color fineColor = new Color(126f / 255f, 159f / 255f, 110f / 255f, 1f);
    [SerializeField] private Color superiorColor = new Color(122f / 255f, 92f / 255f, 142f / 255f, 1f);
    [SerializeField] private Color epicColor = new Color(176f / 255f, 138f / 255f, 74f / 255f, 1f);
    [SerializeField] private Color immortalColor = new Color(155f / 255f, 78f / 255f, 63f / 255f, 1f);

    private TradeOfferStack currentStack;

    public event Action<TradeOfferSlotUI> Clicked;
    public event Action<TradeOfferSlotUI> DecreaseClicked;

    public TradeOfferStack CurrentStack => currentStack;
    public bool HasItem => currentStack.Definition != null && currentStack.OfferQuantity > 0;

    private void Awake()
    {
        AutoBind();
    }

    private void OnEnable()
    {
        if (decreaseButton != null)
        {
            decreaseButton.onClick.AddListener(HandleDecreaseClicked);
        }
    }

    private void OnDisable()
    {
        if (decreaseButton != null)
        {
            decreaseButton.onClick.RemoveListener(HandleDecreaseClicked);
        }
    }

    private void Reset()
    {
        AutoBind();
    }

    private void OnValidate()
    {
        AutoBind();
    }

    public void Setup(TradeOfferStack stack)
    {
        AutoBind();
        currentStack = stack;

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
            quantityText.text = currentStack.OfferQuantity.ToString();
        }

        ApplyVisual();
        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasItem)
        {
            return;
        }

        Clicked?.Invoke(this);
    }

    private void HandleDecreaseClicked()
    {
        if (!HasItem)
        {
            return;
        }

        DecreaseClicked?.Invoke(this);
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

        if (quantityBarImage == null)
        {
            quantityBarImage = FindChildComponent<Image>("QuantityBar");
        }

        if (quantityText == null)
        {
            quantityText = FindChildComponent<TMP_Text>("QuantityText");
        }

        if (decreaseButton == null)
        {
            decreaseButton = FindChildComponent<Button>("DecreaseButton");
        }
    }

    private void ApplyVisual()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = GetRarityColor();
        }

        if (quantityBarImage != null)
        {
            quantityBarImage.color = quantityBarColor;
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
