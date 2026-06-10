using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShopController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ShopItemSlotUI itemSlotPrefab;
    [SerializeField] private ScrollRect shelfScrollView;
    [SerializeField] private Scrollbar verticalScrollbar;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Shop Setup")]
    [SerializeField] private string resourcesFolderName = "ShopItem";
    [SerializeField] [Min(1)] private int itemsToDisplay = 4;
    [SerializeField] private bool allowDuplicateStock = true;
    [SerializeField] [Min(0)] private int startingMoney = 1000;
    [SerializeField] private string exitSceneName = "ShopMainScene";

    [Header("Discount Settings")]
    [SerializeField] private ShopDiscountSettings discountSettings = new ShopDiscountSettings();
    [SerializeField] private ShopRarityWeightTable rarityWeights = new ShopRarityWeightTable();

    [Header("Feedback")]
    [SerializeField] private float feedbackDuration = 1.5f;
    [SerializeField] private Color successMessageColor = new Color(0.35f, 0.95f, 0.45f, 1f);
    [SerializeField] private Color failureMessageColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color neutralMessageColor = Color.white;

    private List<ShopItemDefinition> itemCatalog = new List<ShopItemDefinition>();
    private readonly List<ShopItemSlotUI> slotPool = new List<ShopItemSlotUI>();
    private readonly List<IShopDiscountModifier> discountModifiers = new List<IShopDiscountModifier>();
    private readonly List<IShopRarityWeightModifier> rarityWeightModifiers = new List<IShopRarityWeightModifier>();

    private Coroutine feedbackRoutine;

    public event System.Action LeaveRequested;

    public Button LeaveButton => leaveButton;

    private void Awake()
    {
        AutoBindSceneReferences();
        PrepareFeedbackText();
        ConfigureScrollView();
        LoadItemCatalog();
    }

    private void LoadItemCatalog()
    {
        ShopItemDefinition[] loadedItems = Resources.LoadAll<ShopItemDefinition>(resourcesFolderName);
        itemCatalog = loadedItems
            .Where(item => item != null)
            .OrderBy(item => item.ItemId)
            .ToList();
    }

    private void OnEnable()
    {
        ShopWallet.MoneyChanged += HandleMoneyChanged;

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(HandleBuyButtonClicked);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(HandleLeaveButtonClicked);
        }
    }

    private void Start()
    {
        ShopItemUnlockRegistry.RegisterDefaults(itemCatalog);
        ShopWallet.InitializeIfNeeded(startingMoney);
        RefreshMoneyText(ShopWallet.CurrentMoney);
        PopulateShelf();
    }

    private void OnDisable()
    {
        ShopWallet.MoneyChanged -= HandleMoneyChanged;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandleBuyButtonClicked);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(HandleLeaveButtonClicked);
        }
    }

    public void RegisterDiscountModifier(IShopDiscountModifier modifier)
    {
        if (modifier != null && !discountModifiers.Contains(modifier))
        {
            discountModifiers.Add(modifier);
        }
    }

    public void UnregisterDiscountModifier(IShopDiscountModifier modifier)
    {
        if (modifier != null)
        {
            discountModifiers.Remove(modifier);
        }
    }

    public void RegisterRarityWeightModifier(IShopRarityWeightModifier modifier)
    {
        if (modifier != null && !rarityWeightModifiers.Contains(modifier))
        {
            rarityWeightModifiers.Add(modifier);
        }
    }

    public void UnregisterRarityWeightModifier(IShopRarityWeightModifier modifier)
    {
        if (modifier != null)
        {
            rarityWeightModifiers.Remove(modifier);
        }
    }

    public void PopulateShelf()
    {
        List<ShopItemInstance> stockedItems = BuildStockInstances();
        RebuildSlotPool(stockedItems);

        for (int index = 0; index < slotPool.Count; index++)
        {
            ShopItemSlotUI slot = slotPool[index];
            slot.Clicked -= HandleSlotClicked;
            slot.Clicked += HandleSlotClicked;
            slot.Setup(stockedItems[index]);
        }

        ShopEvents.RaiseItemSelectionCleared();
        PublishPurchasePreview();
        RebuildShelfLayout();
    }

    private void HandleBuyButtonClicked()
    {
        List<ShopItemSlotUI> selectedSlots = GetSelectedPurchasableSlots();

        if (selectedSlots.Count == 0)
        {
            ShowFeedback("\u8bf7\u5148\u9009\u62e9\u8981\u8d2d\u4e70\u7684\u5546\u54c1\u3002", neutralMessageColor);
            return;
        }

        int totalPrice = selectedSlots.Sum(slot => slot.Price);
        if (!ShopWallet.TrySpend(totalPrice))
        {
            ShowFeedback("\u8d2d\u4e70\u5931\u8d25\uff1a\u7075\u77f3\u4e0d\u8db3\u3002", failureMessageColor);
            return;
        }

        for (int index = 0; index < selectedSlots.Count; index++)
        {
            ShopItemSlotUI slot = selectedSlots[index];
            ShopItemInstance purchasedItem = slot.CurrentItem;
            HandlePurchasedItem(purchasedItem);
            slot.MarkAsSold();
            ShopEvents.RaiseItemSold(purchasedItem);
        }

        ShopEvents.RaiseItemSelectionCleared();
        RefreshMoneyText(ShopWallet.CurrentMoney);
        PublishPurchasePreview();
        ShowFeedback($"\u8d2d\u4e70\u6210\u529f\uff0c\u82b1\u8d39 {totalPrice}\u7075\u77f3\u3002", successMessageColor);
    }

    private void HandleLeaveButtonClicked()
    {
        if (LeaveRequested != null)
        {
            LeaveRequested.Invoke();
            return;
        }

        if (string.IsNullOrWhiteSpace(exitSceneName))
        {
            ShowFeedback("\u79bb\u5f00\u573a\u666f\u5c1a\u672a\u914d\u7f6e\u3002", neutralMessageColor);
            return;
        }

        SceneManager.LoadScene(exitSceneName);
    }

    private void HandleSlotClicked(ShopItemSlotUI clickedSlot)
    {
        if (clickedSlot == null || !clickedSlot.HasItem)
        {
            return;
        }

        if (clickedSlot.IsSelected)
        {
            ShopEvents.RaiseItemSelected(clickedSlot.CurrentItem);
            RefreshMoneyText(ShopWallet.CurrentMoney);
            PublishPurchasePreview();
            return;
        }

        ShopItemSlotUI fallbackSlot = slotPool.LastOrDefault(
            slot => slot != clickedSlot && slot.HasItem && slot.IsSelected && !slot.IsSold);

        if (fallbackSlot != null)
        {
            ShopEvents.RaiseItemSelected(fallbackSlot.CurrentItem);
        }
        else
        {
            ShopEvents.RaiseItemSelectionCleared();
        }

        RefreshMoneyText(ShopWallet.CurrentMoney);
        PublishPurchasePreview();
    }

    private void HandlePurchasedItem(ShopItemInstance itemInstance)
    {
        if (itemInstance == null)
        {
            return;
        }

        ShopItemUnlockRegistry.Unlock(itemInstance.Definition);
        WarehouseInventory.Add(itemInstance.Definition);

        // Hook for future inventory integration.
    }

    private void HandleMoneyChanged(int currentMoney)
    {
        RefreshMoneyText(currentMoney);
    }

    private void RefreshMoneyText(int currentMoney)
    {
        if (moneyText == null)
        {
            return;
        }

        int selectedTotalPrice = GetSelectedPurchasableSlots().Sum(slot => slot.Price);
        if (selectedTotalPrice <= 0)
        {
            moneyText.text = $"{currentMoney}\u7075\u77f3";
            return;
        }

        int remainingMoney = Mathf.Max(0, currentMoney - selectedTotalPrice);
        moneyText.text = $"{currentMoney}\u7075\u77f3({remainingMoney}\u7075\u77f3)";
    }

    private List<ShopItemSlotUI> GetSelectedPurchasableSlots()
    {
        return slotPool
            .Where(slot => slot.gameObject.activeSelf && slot.HasItem && slot.IsSelected && !slot.IsSold)
            .ToList();
    }

    private void PublishPurchasePreview()
    {
        int selectedTotalPrice = GetSelectedPurchasableSlots().Sum(slot => slot.Price);
        ShopEvents.RaisePurchasePreviewChanged(selectedTotalPrice);
    }

    private List<ShopItemInstance> BuildStockInstances()
    {
        List<ShopItemDefinition> validItems = itemCatalog
            .Where(item => item != null && item.Rarity != ShopItemRarity.Immortal)
            .Distinct()
            .ToList();

        if (validItems.Count == 0)
        {
            ShowFeedback("\u672a\u914d\u7f6e\u53ef\u4e0a\u67b6\u7684\u5546\u54c1\u3002", failureMessageColor);
            return new List<ShopItemInstance>();
        }

        int targetCount = Mathf.Max(1, itemsToDisplay);
        ShopDiscountSettings effectiveDiscountSettings = BuildEffectiveDiscountSettings();
        List<ShopItemInstance> result = new List<ShopItemInstance>(targetCount);
        List<ShopItemDefinition> availableUniqueItems = new List<ShopItemDefinition>(validItems);

        for (int index = 0; index < targetCount; index++)
        {
            if (!allowDuplicateStock && availableUniqueItems.Count == 0)
            {
                break;
            }

            List<ShopItemDefinition> sourceCatalog = allowDuplicateStock ? validItems : availableUniqueItems;

            ShopItemDefinition definition = ShopGenerationUtility.DrawWeightedItem(
                sourceCatalog,
                rarityWeights,
                rarityWeightModifiers);

            if (definition == null)
            {
                continue;
            }

            if (!allowDuplicateStock)
            {
                availableUniqueItems.Remove(definition);
            }

            float discountRate = index == 0
                ? effectiveDiscountSettings.GuaranteedDiscount
                : ShopGenerationUtility.SampleTruncatedExponential(effectiveDiscountSettings);

            result.Add(new ShopItemInstance(definition, discountRate));
        }

        return result;
    }

    private ShopDiscountSettings BuildEffectiveDiscountSettings()
    {
        ShopDiscountSettings effectiveSettings = discountSettings == null
            ? new ShopDiscountSettings()
            : discountSettings.Clone();

        for (int index = 0; index < discountModifiers.Count; index++)
        {
            IShopDiscountModifier modifier = discountModifiers[index];
            if (modifier == null)
            {
                continue;
            }

            modifier.ModifyDiscountSettings(effectiveSettings);
        }

        effectiveSettings.Validate();
        return effectiveSettings;
    }

    private void AutoBindSceneReferences()
    {
        if (contentRoot == null)
        {
            Transform content = transform.Find("ShelfPanel/ShelfScrollView/Viewport/Content");
            if (content != null)
            {
                contentRoot = content;
            }
        }

        if (shelfScrollView == null)
        {
            Transform scrollViewTransform = transform.Find("ShelfPanel/ShelfScrollView");
            if (scrollViewTransform != null)
            {
                shelfScrollView = scrollViewTransform.GetComponent<ScrollRect>();
            }
        }

        if (verticalScrollbar == null && shelfScrollView != null)
        {
            verticalScrollbar = shelfScrollView.GetComponentInChildren<Scrollbar>(true);
        }

        if (moneyText == null)
        {
            Transform moneyTextTransform = transform.Find("MoneyPanel/MoneyText");
            if (moneyTextTransform != null)
            {
                moneyText = moneyTextTransform.GetComponent<TMP_Text>();
            }
        }

        if (buyButton == null)
        {
            Transform buyButtonTransform = transform.Find("ActionPanel/BuyButton");
            if (buyButtonTransform != null)
            {
                buyButton = buyButtonTransform.GetComponent<Button>();
            }
        }

        if (leaveButton == null)
        {
            Transform leaveButtonTransform = transform.Find("ActionPanel/LeaveButton");
            if (leaveButtonTransform != null)
            {
                leaveButton = leaveButtonTransform.GetComponent<Button>();
            }
        }

        if (feedbackText == null)
        {
            Transform feedbackTextTransform = transform.Find("FeedbackText");
            if (feedbackTextTransform != null)
            {
                feedbackText = feedbackTextTransform.GetComponent<TMP_Text>();
            }
        }
    }

    private void ConfigureScrollView()
    {
        if (shelfScrollView == null || contentRoot == null)
        {
            return;
        }

        RectTransform contentRect = contentRoot as RectTransform;
        RectTransform viewportRect = contentRoot.parent as RectTransform;

        shelfScrollView.content = contentRect;
        shelfScrollView.viewport = viewportRect;
        shelfScrollView.vertical = true;
        shelfScrollView.movementType = ScrollRect.MovementType.Clamped;
        shelfScrollView.verticalScrollbar = verticalScrollbar;
        shelfScrollView.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }

    private void RebuildSlotPool(IReadOnlyList<ShopItemInstance> stockedItems)
    {
        if (contentRoot == null || itemSlotPrefab == null)
        {
            return;
        }

        ClearExistingSlotObjects();
        slotPool.Clear();

        for (int index = 0; index < stockedItems.Count; index++)
        {
            ShopItemSlotUI slot = Instantiate(itemSlotPrefab, contentRoot);
            slot.name = $"ItemSlot_{index + 1:00}";
            slotPool.Add(slot);
        }
    }

    private void ClearExistingSlotObjects()
    {
        if (contentRoot == null)
        {
            return;
        }

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in contentRoot)
        {
            childrenToDestroy.Add(child.gameObject);
        }

        for (int index = 0; index < childrenToDestroy.Count; index++)
        {
            Destroy(childrenToDestroy[index]);
        }
    }

    private void PrepareFeedbackText()
    {
        if (feedbackText == null)
        {
            Debug.LogWarning(
                "ShopController is missing FeedbackText. Please create and bind a TMP text under ShopCanvas.",
                this);
            return;
        }

        feedbackText.text = string.Empty;
        feedbackText.gameObject.SetActive(false);
    }

    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText == null)
        {
            return;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message, color));
    }

    private IEnumerator ShowFeedbackRoutine(string message, Color color)
    {
        feedbackText.gameObject.SetActive(true);
        feedbackText.color = color;
        feedbackText.text = message;

        yield return new WaitForSeconds(feedbackDuration);

        feedbackText.text = string.Empty;
        feedbackText.gameObject.SetActive(false);
        feedbackRoutine = null;
    }

    private void RebuildShelfLayout()
    {
        if (contentRoot == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform contentRect = contentRoot as RectTransform;
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        if (shelfScrollView != null)
        {
            shelfScrollView.verticalNormalizedPosition = 1f;
        }
    }
}
