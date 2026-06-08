using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TradeSceneController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform warehouseContentRoot;
    [SerializeField] private Transform offerContentRoot;
    [SerializeField] private TradeInventorySlotUI warehouseSlotPrefab;
    [SerializeField] private TradeOfferSlotUI offerSlotPrefab;
    [SerializeField] private ScrollRect warehouseScrollView;
    [SerializeField] private ScrollRect offerScrollView;
    [SerializeField] private TradeDetailPanelController detailPanel;
    [SerializeField] private Button tradeButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text emptyWarehouseText;
    [SerializeField] private TMP_Text emptyOfferText;

    [Header("Trade Settings")]
    [SerializeField] [Min(1)] private int maxTradeItemCount = 8;
    [SerializeField] [Min(0f)] private float sellPriceMultiplier = 1f;

    private readonly Dictionary<string, int> offerQuantities = new Dictionary<string, int>();
    private readonly List<TradeInventorySlotUI> warehouseSlots = new List<TradeInventorySlotUI>();
    private readonly List<TradeOfferSlotUI> offerSlots = new List<TradeOfferSlotUI>();

    private ShopVisitor currentVisitor;
    private NPCDefinition currentNpc;
    private TradeInventorySlotUI selectedWarehouseSlot;
    private string selectedItemId;
    private bool isUnloading;

    private void Awake()
    {
        AutoBind();
        ConfigureScrollViews();
        currentVisitor = TradeSceneContext.CurrentVisitor;
        currentNpc = TradeSceneContext.CurrentNpc;
        RebuildAll();
    }

    private void OnEnable()
    {
        WarehouseInventory.Changed += RebuildAll;

        if (tradeButton != null)
        {
            tradeButton.onClick.AddListener(HandleTradeClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(UnloadTradeScene);
        }
    }

    private void OnDisable()
    {
        WarehouseInventory.Changed -= RebuildAll;
        ShopEvents.RaiseWalletPreviewChanged(0);

        if (tradeButton != null)
        {
            tradeButton.onClick.RemoveListener(HandleTradeClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(UnloadTradeScene);
        }
    }

    public void RebuildAll()
    {
        AutoBind();
        ClampOffersToWarehouse();
        RebuildWarehouseSlots();
        RebuildOfferSlots();
        RefreshSelectedDetail();
        RefreshSummary();
    }

    public void UnloadTradeScene()
    {
        if (isUnloading)
        {
            return;
        }

        isUnloading = true;
        TradeSceneContext.Clear();

        Scene currentScene = gameObject.scene;
        if (currentScene.IsValid() && currentScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(currentScene);
        }
    }

    private void HandleWarehouseSlotClicked(TradeInventorySlotUI clickedSlot)
    {
        if (clickedSlot == null || !clickedSlot.HasItem)
        {
            return;
        }

        selectedItemId = clickedSlot.CurrentStack.ItemId;
        AddOffer(clickedSlot.CurrentStack.ItemId);
        SelectWarehouseSlot(clickedSlot);
        RefreshSelectedDetail();
    }

    private void HandleOfferSlotClicked(TradeOfferSlotUI clickedSlot)
    {
        if (clickedSlot == null || !clickedSlot.HasItem)
        {
            return;
        }

        selectedItemId = clickedSlot.CurrentStack.ItemId;
        RefreshSelectedDetail();
    }

    private void HandleOfferDecreaseClicked(TradeOfferSlotUI clickedSlot)
    {
        if (clickedSlot == null || !clickedSlot.HasItem)
        {
            return;
        }

        RemoveOffer(clickedSlot.CurrentStack.ItemId);
    }

    private void AddOffer(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return;
        }

        int offeredCount = GetTotalOfferCount();
        if (offeredCount >= maxTradeItemCount)
        {
            ShowFeedback($"最多交易 {maxTradeItemCount} 件物品。");
            return;
        }

        int warehouseQuantity = WarehouseInventory.GetQuantity(itemId);
        int currentOfferQuantity = GetOfferQuantity(itemId);
        if (warehouseQuantity <= 0 || currentOfferQuantity >= warehouseQuantity)
        {
            return;
        }

        offerQuantities[itemId] = currentOfferQuantity + 1;
        RebuildAll();
    }

    private void RemoveOffer(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || !offerQuantities.TryGetValue(itemId, out int quantity))
        {
            return;
        }

        quantity--;
        if (quantity <= 0)
        {
            offerQuantities.Remove(itemId);
        }
        else
        {
            offerQuantities[itemId] = quantity;
        }

        RebuildAll();
    }

    private void HandleTradeClicked()
    {
        if (currentVisitor == null && currentNpc == null)
        {
            ShowFeedback("未找到交易对象。");
            return;
        }

        if (offerQuantities.Count == 0)
        {
            ShowFeedback("请先选择要交易的物品。");
            return;
        }

        List<TradeOfferStack> offers = BuildOfferStacks();
        int totalEarnings = offers.Sum(stack => GetSellPrice(stack) * stack.OfferQuantity);
        int totalAttack = offers.Sum(stack => stack.Attack * stack.OfferQuantity);
        int totalDefense = offers.Sum(stack => stack.Defense * stack.OfferQuantity);
        int totalSpeed = offers.Sum(stack => stack.MovementSpeed * stack.OfferQuantity);

        for (int index = 0; index < offers.Count; index++)
        {
            TradeOfferStack offer = offers[index];
            for (int count = 0; count < offer.OfferQuantity; count++)
            {
                WarehouseInventory.Remove(offer.ItemId);
            }
        }

        ShopEvents.RaiseWalletPreviewChanged(0);
        ShopWallet.AddMoney(totalEarnings);
        if (currentNpc != null)
        {
            currentNpc.ApplyStatBonus(totalAttack, totalDefense, totalSpeed);
        }

        MainSceneShopController sourceController = TradeSceneContext.SourceController;
        if (sourceController != null)
        {
            sourceController.CompleteTradeForVisitor(currentVisitor);
        }

        offerQuantities.Clear();
        ShowFeedback($"交易完成，获得 {totalEarnings} 灵石。");
        UnloadTradeScene();
    }

    private void RebuildWarehouseSlots()
    {
        IReadOnlyList<TradeOfferStack> stacks = BuildWarehouseStacks();
        RebuildWarehouseSlotPool(stacks);

        for (int index = 0; index < warehouseSlots.Count; index++)
        {
            TradeInventorySlotUI slot = warehouseSlots[index];
            slot.Clicked -= HandleWarehouseSlotClicked;
            slot.Clicked += HandleWarehouseSlotClicked;
            slot.Setup(stacks[index]);
            slot.SetSelected(slot.CurrentStack.ItemId == selectedItemId);
        }

        if (emptyWarehouseText != null)
        {
            emptyWarehouseText.gameObject.SetActive(stacks.Count == 0);
        }

        RebuildLayout(warehouseContentRoot, warehouseScrollView);
    }

    private void RebuildOfferSlots()
    {
        IReadOnlyList<TradeOfferStack> offers = BuildOfferStacks();
        RebuildOfferSlotPool(offers);

        for (int index = 0; index < offerSlots.Count; index++)
        {
            TradeOfferSlotUI slot = offerSlots[index];
            slot.Clicked -= HandleOfferSlotClicked;
            slot.DecreaseClicked -= HandleOfferDecreaseClicked;
            slot.Clicked += HandleOfferSlotClicked;
            slot.DecreaseClicked += HandleOfferDecreaseClicked;
            slot.Setup(offers[index]);
        }

        if (emptyOfferText != null)
        {
            emptyOfferText.gameObject.SetActive(offers.Count == 0);
        }

        RebuildLayout(offerContentRoot, offerScrollView);
    }

    private IReadOnlyList<TradeOfferStack> BuildWarehouseStacks()
    {
        return WarehouseInventory.GetStacks()
            .Select(stack =>
            {
                int offerQuantity = GetOfferQuantity(stack.ItemId);
                return new TradeOfferStack(
                    stack.Definition,
                    Mathf.Max(0, stack.Quantity - offerQuantity),
                    offerQuantity);
            })
            .OrderBy(stack => stack.ItemId)
            .ToList();
    }

    private List<TradeOfferStack> BuildOfferStacks()
    {
        List<TradeOfferStack> result = new List<TradeOfferStack>();
        IReadOnlyList<WarehouseItemStack> warehouseStacks = WarehouseInventory.GetStacks();

        foreach (KeyValuePair<string, int> pair in offerQuantities)
        {
            WarehouseItemStack warehouseStack = warehouseStacks.FirstOrDefault(stack => stack.ItemId == pair.Key);
            if (warehouseStack.Definition == null || pair.Value <= 0)
            {
                continue;
            }

            result.Add(new TradeOfferStack(
                warehouseStack.Definition,
                Mathf.Max(0, warehouseStack.Quantity - pair.Value),
                pair.Value));
        }

        return result.OrderBy(stack => stack.ItemId).ToList();
    }

    private void ClampOffersToWarehouse()
    {
        List<string> keys = offerQuantities.Keys.ToList();
        for (int index = 0; index < keys.Count; index++)
        {
            string itemId = keys[index];
            int warehouseQuantity = WarehouseInventory.GetQuantity(itemId);
            int quantity = Mathf.Clamp(offerQuantities[itemId], 0, warehouseQuantity);

            if (quantity <= 0)
            {
                offerQuantities.Remove(itemId);
            }
            else
            {
                offerQuantities[itemId] = quantity;
            }
        }
    }

    private void RefreshSelectedDetail()
    {
        TradeOfferStack selectedStack = BuildWarehouseStacks()
            .Concat(BuildOfferStacks())
            .FirstOrDefault(stack => stack.ItemId == selectedItemId);

        if (detailPanel != null)
        {
            if (selectedStack.Definition == null)
            {
                detailPanel.ShowEmpty();
            }
            else
            {
                detailPanel.Show(selectedStack);
            }
        }
    }

    private void RefreshSummary()
    {
        int itemCount = GetTotalOfferCount();
        int earnings = BuildOfferStacks().Sum(stack => GetSellPrice(stack) * stack.OfferQuantity);

        if (summaryText != null)
        {
            summaryText.text = $"预交易 {itemCount}/{maxTradeItemCount} 件  可得 {earnings} 灵石";
        }

        ShopEvents.RaiseWalletPreviewChanged(earnings);

        if (tradeButton != null)
        {
            tradeButton.interactable = (currentVisitor != null || currentNpc != null) && itemCount > 0;
        }
    }

    private int GetSellPrice(TradeOfferStack stack)
    {
        return Mathf.RoundToInt(stack.Price * sellPriceMultiplier);
    }

    private int GetOfferQuantity(string itemId)
    {
        return offerQuantities.TryGetValue(itemId, out int quantity) ? quantity : 0;
    }

    private int GetTotalOfferCount()
    {
        return offerQuantities.Values.Sum();
    }

    private void SelectWarehouseSlot(TradeInventorySlotUI slotToSelect)
    {
        selectedWarehouseSlot = slotToSelect;

        for (int index = 0; index < warehouseSlots.Count; index++)
        {
            TradeInventorySlotUI slot = warehouseSlots[index];
            slot.SetSelected(slot == selectedWarehouseSlot);
        }
    }

    private void RebuildWarehouseSlotPool(IReadOnlyList<TradeOfferStack> stacks)
    {
        ClearChildren(warehouseContentRoot);
        warehouseSlots.Clear();

        if (warehouseContentRoot == null || warehouseSlotPrefab == null)
        {
            return;
        }

        for (int index = 0; index < stacks.Count; index++)
        {
            TradeInventorySlotUI slot = Instantiate(warehouseSlotPrefab, warehouseContentRoot);
            slot.name = $"TradeWarehouseSlot_{index + 1:00}";
            warehouseSlots.Add(slot);
        }
    }

    private void RebuildOfferSlotPool(IReadOnlyList<TradeOfferStack> stacks)
    {
        ClearChildren(offerContentRoot);
        offerSlots.Clear();

        if (offerContentRoot == null || offerSlotPrefab == null)
        {
            return;
        }

        for (int index = 0; index < stacks.Count; index++)
        {
            TradeOfferSlotUI slot = Instantiate(offerSlotPrefab, offerContentRoot);
            slot.name = $"TradeOfferSlot_{index + 1:00}";
            offerSlots.Add(slot);
        }
    }

    private void ClearChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in root)
        {
            children.Add(child.gameObject);
        }

        for (int index = 0; index < children.Count; index++)
        {
            Destroy(children[index]);
        }
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    private void AutoBind()
    {
        if (warehouseContentRoot == null)
        {
            warehouseContentRoot = transform.Find("TradePanel/WarehouseScrollView/Viewport/Content");
        }

        if (offerContentRoot == null)
        {
            offerContentRoot = transform.Find("TradePanel/DetailPanel/OfferScrollView/Viewport/Content");
        }

        if (warehouseScrollView == null)
        {
            Transform scrollView = transform.Find("TradePanel/WarehouseScrollView");
            if (scrollView != null)
            {
                warehouseScrollView = scrollView.GetComponent<ScrollRect>();
            }
        }

        if (offerScrollView == null)
        {
            Transform scrollView = transform.Find("TradePanel/DetailPanel/OfferScrollView");
            if (scrollView != null)
            {
                offerScrollView = scrollView.GetComponent<ScrollRect>();
            }
        }

        if (detailPanel == null)
        {
            Transform detailRoot = transform.Find("TradePanel/DetailPanel");
            if (detailRoot != null)
            {
                detailPanel = detailRoot.GetComponent<TradeDetailPanelController>();
            }
        }

        if (tradeButton == null)
        {
            tradeButton = FindChildComponent<Button>("TradePanel/DetailPanel/TradeButton");
        }

        if (exitButton == null)
        {
            exitButton = FindChildComponent<Button>("TradePanel/ExitButton");
        }

        if (summaryText == null)
        {
            summaryText = FindChildComponent<TMP_Text>("TradePanel/DetailPanel/SummaryText");
        }

        if (feedbackText == null)
        {
            feedbackText = FindChildComponent<TMP_Text>("TradePanel/DetailPanel/FeedbackText");
        }

        if (emptyWarehouseText == null)
        {
            emptyWarehouseText = FindChildComponent<TMP_Text>("TradePanel/EmptyWarehouseText");
        }

        if (emptyOfferText == null)
        {
            emptyOfferText = FindChildComponent<TMP_Text>("TradePanel/DetailPanel/EmptyOfferText");
        }
    }

    private void ConfigureScrollViews()
    {
        ConfigureScrollView(warehouseScrollView, warehouseContentRoot);
        ConfigureScrollView(offerScrollView, offerContentRoot);
    }

    private void ConfigureScrollView(ScrollRect scrollView, Transform contentRoot)
    {
        if (scrollView == null || contentRoot == null)
        {
            return;
        }

        scrollView.content = contentRoot as RectTransform;
        scrollView.viewport = contentRoot.parent as RectTransform;
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.movementType = ScrollRect.MovementType.Clamped;
    }

    private void RebuildLayout(Transform contentRoot, ScrollRect scrollView)
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

        if (scrollView != null)
        {
            scrollView.verticalNormalizedPosition = 1f;
        }
    }

    private T FindChildComponent<T>(string relativePath) where T : Component
    {
        Transform child = transform.Find(relativePath);
        return child == null ? null : child.GetComponent<T>();
    }
}
