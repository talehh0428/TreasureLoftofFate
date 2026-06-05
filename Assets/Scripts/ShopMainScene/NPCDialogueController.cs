using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC 交互面板控制器 — 统一管理"对话"和"交易"两种模式。
/// 内部通过切换 DialogueContent / TradeContent 子容器来实现。
/// 交易模式采用左-右布局（物品列表），底部仅含"出售"和"下一步"按钮。
/// </summary>
public class NPCDialogueController : MonoBehaviour
{
    public enum InteractionMode
    {
        Dialogue,
        Trade
    }

    [Header("UI References - Root")]
    [SerializeField] private GameObject interactionPanel;

    [Header("Dialogue Mode")]
    [SerializeField] private GameObject dialogueContent;
    [SerializeField] private Image npcAvatar;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button tradeButton;

    [Header("Trade Mode")]
    [SerializeField] private GameObject tradeContent;

    // --- 顶部 NPC 信息 ---
    [SerializeField] private TMP_Text tradeNpcNameText;
    [SerializeField] private Image tradeNpcAvatar;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text speedText;

    // --- 左区：物品列表 ---
    [SerializeField] private Transform itemContentRoot;
    [SerializeField] private TradeItemSlotUI tradeItemSlotPrefab;

    // --- 底部操作栏 ---
    [SerializeField] private TMP_Text totalPreviewText;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text emptyText;

    [Header("Dialogue Settings")]
    [SerializeField] private float textSpeed = 0.05f;

    [Header("Trade Settings")]
    [SerializeField] private float sellPriceMultiplier = 1.2f;
    [SerializeField] private float feedbackDuration = 1.5f;
    [SerializeField] private Color successColor = new Color(0.35f, 0.95f, 0.45f, 1f);
    [SerializeField] private Color failureColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color neutralColor = Color.white;

    /// <summary>玩家点击"下一步"时触发，携带本次交易卖出的总金额（0 表示未出售物品）</summary>
    public event Action<NPCDefinition, int> NextRequested;
    public event Action<NPCDefinition> DialogueEnded;
    public event Action<NPCDefinition> TradeRequested;

    private NPCDefinition currentNpc;
    private Coroutine typewriterRoutine;
    private Coroutine feedbackRoutine;
    private bool isTypewriterPlaying;
    private bool isVisible;
    private InteractionMode currentMode = InteractionMode.Dialogue;
    private int lastTradeEarnings;

    private readonly List<TradeItemSlotUI> slotPool = new List<TradeItemSlotUI>();

    public bool IsVisible => isVisible;
    public NPCDefinition CurrentNpc => currentNpc;
    public InteractionMode CurrentMode => currentMode;

    private void Awake()
    {
        AutoBind();
        Hide();
    }

    private void OnEnable()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(HandleContinueClicked);
        if (tradeButton != null)
            tradeButton.onClick.AddListener(HandleTradeClicked);
        if (sellButton != null)
            sellButton.onClick.AddListener(HandleSellClicked);
        if (nextButton != null)
            nextButton.onClick.AddListener(HandleNextClicked);
    }

    private void OnDisable()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(HandleContinueClicked);
        if (tradeButton != null)
            tradeButton.onClick.RemoveListener(HandleTradeClicked);
        if (sellButton != null)
            sellButton.onClick.RemoveListener(HandleSellClicked);
        if (nextButton != null)
            nextButton.onClick.RemoveListener(HandleNextClicked);

        StopTypewriter();
        StopFeedback();
    }

    // ========== 公开方法 ==========

    /// <summary>打开对话模式</summary>
    public void ShowDialogue(NPCDefinition npc)
    {
        if (npc == null) return;

        currentNpc = npc;
        currentMode = InteractionMode.Dialogue;
        isVisible = true;

        // 切换 Content
        SetContentActive(InteractionMode.Dialogue);

        // 填充 NPC 信息
        if (npcAvatar != null)
        {
            npcAvatar.sprite = npc.Avatar;
            npcAvatar.enabled = npc.Avatar != null;
        }
        if (npcNameText != null)
            npcNameText.text = npc.DisplayName;

        // 开始打字
        StartTypewriter(npc.Description);

        if (continueButton != null)
            continueButton.interactable = false;
        if (tradeButton != null)
            tradeButton.gameObject.SetActive(false);
    }

    /// <summary>打开交易模式</summary>
    public void ShowTrade(NPCDefinition npc)
    {
        if (npc == null) return;

        currentNpc = npc;
        currentMode = InteractionMode.Trade;
        isVisible = true;
        lastTradeEarnings = 0; // 重置本次交易收益

        // 切换 Content
        SetContentActive(InteractionMode.Trade);

        // 填充 NPC 信息（交易区）
        if (tradeNpcAvatar != null)
        {
            tradeNpcAvatar.sprite = npc.Avatar;
            tradeNpcAvatar.enabled = npc.Avatar != null;
        }
        if (tradeNpcNameText != null)
            tradeNpcNameText.text = npc.DisplayName;

        UpdateNpcDisplay();
        PopulateItemList();
        UpdateTotalPreview();
    }

    /// <summary>隐藏面板</summary>
    public void Hide()
    {
        isVisible = false;
        StopTypewriter();
        StopFeedback();
        currentNpc = null;

        // 清空对话数据
        if (npcAvatar != null) { npcAvatar.sprite = null; npcAvatar.enabled = false; }
        if (npcNameText != null) npcNameText.text = string.Empty;
        if (dialogueText != null) dialogueText.text = string.Empty;
        if (dialogueContent != null) dialogueContent.SetActive(false);

        // 清空交易数据
        if (tradeNpcAvatar != null) { tradeNpcAvatar.sprite = null; tradeNpcAvatar.enabled = false; }
        if (tradeNpcNameText != null) tradeNpcNameText.text = string.Empty;
        if (tradeContent != null) tradeContent.SetActive(false);
        if (totalPreviewText != null) totalPreviewText.text = string.Empty;
    }

    /// <summary>刷新当前模式（主要用于交易模式数据变更后刷新）</summary>
    public void Refresh()
    {
        if (!isVisible || currentNpc == null) return;

        switch (currentMode)
        {
            case InteractionMode.Trade:
                UpdateNpcDisplay();
                PopulateItemList();
                UpdateTotalPreview();
                break;
            case InteractionMode.Dialogue:
                // 对话模式不需要刷新，除非重新开始打字
                break;
        }
    }

    // ========== 内部逻辑 ==========

    private void SetContentActive(InteractionMode mode)
    {
        if (dialogueContent != null)
            dialogueContent.SetActive(mode == InteractionMode.Dialogue);
        if (tradeContent != null)
            tradeContent.SetActive(mode == InteractionMode.Trade);

        // 确保整个面板激活
        if (interactionPanel != null)
            interactionPanel.SetActive(true);
        gameObject.SetActive(true);
    }

    // ---------- 对话模式 ----------

    private void HandleContinueClicked()
    {
        if (isTypewriterPlaying)
        {
            // 打字中 → 跳过
            StopTypewriter();
            if (dialogueText != null && currentNpc != null)
                dialogueText.text = currentNpc.Description;
            if (continueButton != null)
                continueButton.interactable = true;
            if (tradeButton != null)
                tradeButton.gameObject.SetActive(true);
            return;
        }

        // 打字完成 → 关闭
        DialogueEnded?.Invoke(currentNpc);
        Hide();
    }

    private void HandleTradeClicked()
    {
        if (currentNpc == null) return;

        TradeRequested?.Invoke(currentNpc);
        ShowTrade(currentNpc);
    }

    private void StartTypewriter(string text)
    {
        StopTypewriter();
        if (string.IsNullOrEmpty(text))
        {
            if (dialogueText != null) dialogueText.text = string.Empty;
            if (continueButton != null) continueButton.interactable = true;
            return;
        }
        typewriterRoutine = StartCoroutine(TypewriterRoutine(text));
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        isTypewriterPlaying = true;
        if (dialogueText != null) dialogueText.text = string.Empty;

        for (int i = 0; i < text.Length; i++)
        {
            if (dialogueText != null)
                dialogueText.text += text[i];
            yield return new WaitForSeconds(textSpeed);
        }

        isTypewriterPlaying = false;
        if (continueButton != null) continueButton.interactable = true;
        if (tradeButton != null) tradeButton.gameObject.SetActive(true);
    }

    private void StopTypewriter()
    {
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }
        isTypewriterPlaying = false;
    }

    // ---------- 交易模式 ----------

    private void UpdateNpcDisplay()
    {
        if (currentNpc == null) return;

        if (attackText != null)
        {
            int effectiveAtk = NPCInteractionData.GetEffectiveAttack(currentNpc);
            attackText.text = $"攻击：{effectiveAtk}";
        }
        if (defenseText != null)
        {
            int effectiveDef = NPCInteractionData.GetEffectiveDefense(currentNpc);
            defenseText.text = $"防御：{effectiveDef}";
        }
        if (speedText != null)
        {
            int effectiveSpd = NPCInteractionData.GetEffectiveSpeed(currentNpc);
            speedText.text = $"遁速：{effectiveSpd}";
        }
    }

    private void PopulateItemList()
    {
        IReadOnlyList<WarehouseItemStack> stacks = WarehouseInventory.GetStacks();
        RebuildSlotPool(stacks);

        for (int i = 0; i < slotPool.Count; i++)
        {
            TradeItemSlotUI slot = slotPool[i];
            slot.SelectionChanged -= UpdateTotalPreview;
            slot.SelectionChanged += UpdateTotalPreview;
            slot.Setup(stacks[i], GetSellPrice(stacks[i]));
        }

        if (emptyText != null)
            emptyText.gameObject.SetActive(stacks.Count == 0);

        // 有物品时默认选中第一个
        if (slotPool.Count > 0 && slotPool[0].HasItem)
        {
            slotPool[0].SetSelected(true);
        }

        UpdateTotalPreview();
        RebuildLayout();
    }

    private void UpdateTotalPreview()
    {
        if (totalPreviewText == null) return;

        int selectedCount = 0;
        int totalEarnings = 0;

        foreach (TradeItemSlotUI slot in slotPool)
        {
            if (slot.IsSelected && slot.HasItem)
            {
                selectedCount++;
                totalEarnings += slot.SellPrice;
            }
        }

        if (selectedCount > 0)
        {
            totalPreviewText.text = $"已选 {selectedCount} 件  共 {totalEarnings} 灵石";
            totalPreviewText.color = successColor;
        }
        else
        {
            totalPreviewText.text = "请选择要出售的物品";
            totalPreviewText.color = neutralColor;
        }
    }

    private void RebuildSlotPool(IReadOnlyList<WarehouseItemStack> stacks)
    {
        if (itemContentRoot == null) return;
        ClearExistingSlots();
        slotPool.Clear();
        if (tradeItemSlotPrefab == null) return;

        for (int i = 0; i < stacks.Count; i++)
        {
            TradeItemSlotUI slot = Instantiate(tradeItemSlotPrefab, itemContentRoot);
            slot.name = $"TradeSlot_{i + 1:00}";
            slotPool.Add(slot);
        }
    }

    private void ClearExistingSlots()
    {
        if (itemContentRoot == null) return;
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in itemContentRoot)
            children.Add(child.gameObject);
        foreach (GameObject child in children)
            Destroy(child);
    }

    private int GetSellPrice(WarehouseItemStack stack)
    {
        return Mathf.RoundToInt(stack.Price * sellPriceMultiplier);
    }

    private void HandleSellClicked()
    {
        List<TradeItemSlotUI> selectedSlots = slotPool
            .Where(s => s.IsSelected && s.HasItem)
            .ToList();

        if (selectedSlots.Count == 0)
        {
            ShowFeedback("请先选择要卖出的商品。", neutralColor);
            return;
        }

        int totalEarnings = 0;
        List<ShopItemDefinition> soldItems = new List<ShopItemDefinition>();

        foreach (TradeItemSlotUI slot in selectedSlots)
        {
            ShopItemDefinition def = slot.CurrentStack.Definition;
            if (WarehouseInventory.Remove(def.ItemId, 1))
            {
                totalEarnings += slot.SellPrice;
                soldItems.Add(def);
                NPCInteractionData.ApplyItemBoost(currentNpc, def);
            }
        }

        if (soldItems.Count > 0)
        {
            ShopWallet.AddMoney(totalEarnings);
            lastTradeEarnings += totalEarnings; // 累计本次交易金额
            UpdateNpcDisplay();
            PopulateItemList();
            UpdateTotalPreview();
            ShowFeedback($"卖出成功！获得 {totalEarnings} 灵石。", successColor);
        }
        else
        {
            ShowFeedback("卖出失败。", failureColor);
        }
    }

    /// <summary>
    /// 玩家点击"下一步" — 直接继续，不返回对话模式。
    /// 携带本次交易实际卖出的总金额（0 表示未出售物品）。
    /// </summary>
    private void HandleNextClicked()
    {
        if (currentNpc == null)
        {
            Hide();
            return;
        }

        NPCDefinition npc = currentNpc;
        int earnings = lastTradeEarnings;
        lastTradeEarnings = 0; // 重置，为下一个 NPC 做准备
        NextRequested?.Invoke(npc, earnings);
        Hide();
    }

    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText == null) return;
        StopFeedback();
        feedbackRoutine = StartCoroutine(FeedbackRoutine(message, color));
    }

    private IEnumerator FeedbackRoutine(string message, Color color)
    {
        feedbackText.gameObject.SetActive(true);
        feedbackText.color = color;
        feedbackText.text = message;
        yield return new WaitForSeconds(feedbackDuration);
        feedbackText.text = string.Empty;
        feedbackText.gameObject.SetActive(false);
        feedbackRoutine = null;
    }

    private void StopFeedback()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }
        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
            feedbackText.gameObject.SetActive(false);
        }
    }

    private void RebuildLayout()
    {
        if (itemContentRoot == null) return;
        Canvas.ForceUpdateCanvases();
        RectTransform contentRect = itemContentRoot as RectTransform;
        if (contentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    // ========== AutoBind ==========

    private void AutoBind()
    {
        if (interactionPanel == null)
            interactionPanel = gameObject;

        // --- DialogueContent ---
        if (dialogueContent == null)
        {
            Transform dc = transform.Find("DialogueContent");
            if (dc != null) dialogueContent = dc.gameObject;
        }

        if (dialogueContent != null)
        {
            Transform root = dialogueContent.transform;
            npcAvatar ??= FindChildComponent<Image>(root, "NPCInfo/NPCAvatar");
            npcNameText ??= FindChildComponent<TMP_Text>(root, "NPCInfo/NPCNameText");
            dialogueText ??= FindChildComponent<TMP_Text>(root, "DialogueText");
            continueButton ??= FindChildComponent<Button>(root, "ContinueButton");
            tradeButton ??= FindChildComponent<Button>(root, "TradeButton");
        }
        else
        {
            Debug.LogWarning("NPCDialogueController: DialogueContent 未找到，无法自动绑定对话 UI 元素。");
        }

        // --- TradeContent ---
        if (tradeContent == null)
        {
            Transform tc = transform.Find("TradeContent");
            if (tc != null) tradeContent = tc.gameObject;
        }

        if (tradeContent != null)
        {
            Transform root = tradeContent.transform;

            // 顶部 NPC 信息
            tradeNpcAvatar ??= FindChildComponent<Image>(root, "NPCInfo/NPCAvatar");
            tradeNpcNameText ??= FindChildComponent<TMP_Text>(root, "NPCInfo/NPCNameText");
            attackText ??= FindChildComponent<TMP_Text>(root, "NPCInfo/AttackText");
            defenseText ??= FindChildComponent<TMP_Text>(root, "NPCInfo/DefenseText");
            speedText ??= FindChildComponent<TMP_Text>(root, "NPCInfo/SpeedText");

            // 左区：物品列表
            itemContentRoot ??= root.Find("MainArea/ItemScrollView/Viewport/Content");

            // 底部操作栏
            totalPreviewText ??= FindChildComponent<TMP_Text>(root, "BottomBar/TotalPreviewText");
            sellButton ??= FindChildComponent<Button>(root, "BottomBar/SellButton");
            nextButton ??= FindChildComponent<Button>(root, "BottomBar/NextButton");
            feedbackText ??= FindChildComponent<TMP_Text>(root, "FeedbackText");
            emptyText ??= FindChildComponent<TMP_Text>(root, "MainArea/ItemScrollView/Viewport/Content/EmptyText");
        }
        else
        {
            Debug.LogWarning("NPCDialogueController: TradeContent 未找到，无法自动绑定交易 UI 元素。");
        }
    }

    private T FindChildComponent<T>(Transform parent, string childPath) where T : Component
    {
        Transform child = parent.Find(childPath);
        return child == null ? null : child.GetComponent<T>();
    }
}
