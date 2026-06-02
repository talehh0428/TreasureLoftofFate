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
    [SerializeField] private List<ShopItemDefinition> itemCatalog = new List<ShopItemDefinition>();
    [SerializeField] [Min(1)] private int itemsToDisplay = 4;
    [SerializeField] private bool allowDuplicateStock;
    [SerializeField] [Min(0)] private int startingMoney = 1000;
    [SerializeField] private string exitSceneName = string.Empty;

    [Header("Feedback")]
    [SerializeField] private float feedbackDuration = 1.5f;
    [SerializeField] private Color successMessageColor = new Color(0.35f, 0.95f, 0.45f, 1f);
    [SerializeField] private Color failureMessageColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color neutralMessageColor = Color.white;

    private readonly List<ShopItemSlotUI> slotPool = new List<ShopItemSlotUI>();
    private Coroutine feedbackRoutine;

    private void Awake()
    {
        AutoBindSceneReferences();
        PrepareFeedbackText();
        ConfigureScrollView();
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

    public void PopulateShelf()
    {
        List<ShopItemDefinition> stockedItems = BuildRandomStock();
        RebuildSlotPool(stockedItems);

        for (int index = 0; index < slotPool.Count; index++)
        {
            ShopItemSlotUI slot = slotPool[index];
            slot.Clicked -= HandleSlotClicked;
            slot.Clicked += HandleSlotClicked;
            slot.Setup(stockedItems[index]);
        }

        RebuildShelfLayout();
    }

    private void HandleBuyButtonClicked()
    {
        List<ShopItemSlotUI> selectedSlots = slotPool
            .Where(slot => slot.gameObject.activeSelf && slot.HasItem && slot.IsSelected && !slot.IsSold)
            .ToList();

        if (selectedSlots.Count == 0)
        {
            ShowFeedback("请先选择要购买的商品。", neutralMessageColor);
            return;
        }

        int totalPrice = selectedSlots.Sum(slot => slot.Price);
        if (!ShopWallet.TrySpend(totalPrice))
        {
            ShowFeedback("购买失败：灵石不足。", failureMessageColor);
            return;
        }

        foreach (ShopItemSlotUI slot in selectedSlots)
        {
            ShopItemDefinition purchasedItem = slot.CurrentItem;
            HandlePurchasedItem(purchasedItem);
            slot.MarkAsSold();
        }

        ShowFeedback($"购买成功，花费 {totalPrice} 灵石。", successMessageColor);
    }

    private void HandleLeaveButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(exitSceneName))
        {
            ShowFeedback("离开场景尚未配置。", neutralMessageColor);
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

        clickedSlot.SetSelected(clickedSlot.IsSelected);
    }

    private void HandlePurchasedItem(ShopItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
        {
            return;
        }

        // 这里预留给后续库存系统接入。
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

        moneyText.text = $"{currentMoney} 灵石";
    }

    private List<ShopItemDefinition> BuildRandomStock()
    {
        List<ShopItemDefinition> validItems = itemCatalog
            .Where(item => item != null)
            .Distinct()
            .ToList();

        if (validItems.Count == 0)
        {
            ShowFeedback("未配置可上架的商品。", failureMessageColor);
            return new List<ShopItemDefinition>();
        }

        List<ShopItemDefinition> result = new List<ShopItemDefinition>();
        int targetCount = Mathf.Max(0, itemsToDisplay);

        if (allowDuplicateStock)
        {
            for (int index = 0; index < targetCount; index++)
            {
                int randomIndex = Random.Range(0, validItems.Count);
                result.Add(validItems[randomIndex]);
            }

            return result;
        }

        List<ShopItemDefinition> shuffledItems = validItems
            .OrderBy(_ => Random.value)
            .ToList();

        int count = Mathf.Min(targetCount, shuffledItems.Count);
        result.AddRange(shuffledItems.Take(count));
        return result;
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

    private void RebuildSlotPool(IReadOnlyList<ShopItemDefinition> stockedItems)
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
            slot.Setup(stockedItems[index]);
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

        foreach (GameObject childObject in childrenToDestroy)
        {
            Destroy(childObject);
        }
    }

    private void PrepareFeedbackText()
    {
        if (feedbackText == null)
        {
            Debug.LogWarning("ShopController 缺少 FeedbackText 引用，请在 ShopCanvas 下创建并绑定一个 TMP 文本用于提示消息。", this);
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
