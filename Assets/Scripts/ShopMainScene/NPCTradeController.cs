using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 追踪 NPC 运行时属性变化（通过交易提升的属性值）
/// </summary>
public static class NPCInteractionData
{
    private static readonly Dictionary<string, int> AttackBoosts = new Dictionary<string, int>();
    private static readonly Dictionary<string, int> DefenseBoosts = new Dictionary<string, int>();
    private static readonly Dictionary<string, int> SpeedBoosts = new Dictionary<string, int>();

    public static int GetEffectiveAttack(NPCDefinition npc)
    {
        if (npc == null) return 0;
        return npc.Attack + GetBoost(npc.NpcId, AttackBoosts);
    }

    public static int GetEffectiveDefense(NPCDefinition npc)
    {
        if (npc == null) return 0;
        return npc.Defense + GetBoost(npc.NpcId, DefenseBoosts);
    }

    public static int GetEffectiveSpeed(NPCDefinition npc)
    {
        if (npc == null) return 0;
        return npc.MovementSpeed + GetBoost(npc.NpcId, SpeedBoosts);
    }

    public static void ApplyItemBoost(NPCDefinition npc, ShopItemDefinition item)
    {
        if (npc == null || item == null) return;
        string id = npc.NpcId;

        if (item.Attack > 0) AttackBoosts[id] = GetBoost(id, AttackBoosts) + item.Attack;
        if (item.Defense > 0) DefenseBoosts[id] = GetBoost(id, DefenseBoosts) + item.Defense;
        if (item.MovementSpeed > 0) SpeedBoosts[id] = GetBoost(id, SpeedBoosts) + item.MovementSpeed;
    }

    public static void ResetAll()
    {
        AttackBoosts.Clear();
        DefenseBoosts.Clear();
        SpeedBoosts.Clear();
    }

    private static int GetBoost(string npcId, Dictionary<string, int> boosts)
    {
        boosts.TryGetValue(npcId, out int value);
        return value;
    }
}

public class NPCTradeController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tradePanel;
    [SerializeField] private Image npcAvatar;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Transform itemContentRoot;
    [SerializeField] private TradeItemSlotUI tradeItemSlotPrefab;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text emptyText;

    [Header("Trade Settings")]
    [SerializeField] private float sellPriceMultiplier = 1.2f;
    [SerializeField] private float feedbackDuration = 1.5f;
    [SerializeField] private Color successColor = new Color(0.35f, 0.95f, 0.45f, 1f);
    [SerializeField] private Color failureColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color neutralColor = Color.white;

    public event Action BackRequested;

    private NPCDefinition currentNpc;
    private readonly List<TradeItemSlotUI> slotPool = new List<TradeItemSlotUI>();
    private Coroutine feedbackRoutine;

    private void Awake()
    {
        AutoBind();
        Hide();
    }

    private void OnEnable()
    {
        if (sellButton != null)
        {
            sellButton.onClick.AddListener(HandleSellClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(HandleBackClicked);
        }
    }

    private void OnDisable()
    {
        if (sellButton != null)
        {
            sellButton.onClick.RemoveListener(HandleSellClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackClicked);
        }

        StopFeedback();
    }

    public void Show(NPCDefinition npc)
    {
        if (npc == null) return;

        currentNpc = npc;

        if (tradePanel != null)
        {
            tradePanel.SetActive(true);
        }

        UpdateNpcDisplay();
        PopulateItemList();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        StopFeedback();
        currentNpc = null;

        if (tradePanel != null)
        {
            tradePanel.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (currentNpc != null)
        {
            UpdateNpcDisplay();
            PopulateItemList();
        }
    }

    private void UpdateNpcDisplay()
    {
        if (currentNpc == null) return;

        if (npcAvatar != null)
        {
            npcAvatar.sprite = currentNpc.Avatar;
            npcAvatar.enabled = currentNpc.Avatar != null;
        }

        if (npcNameText != null)
        {
            npcNameText.text = currentNpc.DisplayName;
        }

        if (attackText != null)
        {
            int effectiveAtk = NPCInteractionData.GetEffectiveAttack(currentNpc);
            attackText.text = effectiveAtk > currentNpc.Attack
                ? $"攻击：{effectiveAtk} (+{effectiveAtk - currentNpc.Attack})"
                : $"攻击：{effectiveAtk}";
        }

        if (defenseText != null)
        {
            int effectiveDef = NPCInteractionData.GetEffectiveDefense(currentNpc);
            defenseText.text = effectiveDef > currentNpc.Defense
                ? $"防御：{effectiveDef} (+{effectiveDef - currentNpc.Defense})"
                : $"防御：{effectiveDef}";
        }

        if (speedText != null)
        {
            int effectiveSpd = NPCInteractionData.GetEffectiveSpeed(currentNpc);
            speedText.text = effectiveSpd > currentNpc.MovementSpeed
                ? $"遁速：{effectiveSpd} (+{effectiveSpd - currentNpc.MovementSpeed})"
                : $"遁速：{effectiveSpd}";
        }
    }

    private void PopulateItemList()
    {
        IReadOnlyList<WarehouseItemStack> stacks = WarehouseInventory.GetStacks();
        RebuildSlotPool(stacks);

        for (int i = 0; i < slotPool.Count; i++)
        {
            TradeItemSlotUI slot = slotPool[i];
            slot.Setup(stacks[i], GetSellPrice(stacks[i]));
        }

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(stacks.Count == 0);
        }

        RebuildLayout();
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
        {
            children.Add(child.gameObject);
        }

        foreach (GameObject child in children)
        {
            Destroy(child);
        }
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
            int sellPrice = GetSellPrice(slot.CurrentStack);

            if (WarehouseInventory.Remove(def.ItemId, 1))
            {
                totalEarnings += sellPrice;
                soldItems.Add(def);
                NPCInteractionData.ApplyItemBoost(currentNpc, def);
            }
        }

        if (soldItems.Count > 0)
        {
            ShopWallet.AddMoney(totalEarnings);
            UpdateNpcDisplay();
            PopulateItemList();
            ShowFeedback($"卖出成功！获得 {totalEarnings} 灵石。", successColor);
        }
        else
        {
            ShowFeedback("卖出失败。", failureColor);
        }
    }

    private void HandleBackClicked()
    {
        BackRequested?.Invoke();
        Hide();
    }

    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText == null) return;

        StopFeedback();
        feedbackRoutine = StartCoroutine(FeedbackRoutine(message, color));
    }

    private IEnumerator<WaitForSeconds> FeedbackRoutine(string message, Color color)
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
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    private void AutoBind()
    {
        if (tradePanel == null)
        {
            tradePanel = gameObject;
        }

        if (npcAvatar == null)
        {
            npcAvatar = FindChildComponent<Image>("NPCInfo/Avatar");
        }

        if (npcNameText == null)
        {
            npcNameText = FindChildComponent<TMP_Text>("NPCInfo/NameText");
        }

        if (attackText == null)
        {
            attackText = FindChildComponent<TMP_Text>("NPCInfo/AttackText");
        }

        if (defenseText == null)
        {
            defenseText = FindChildComponent<TMP_Text>("NPCInfo/DefenseText");
        }

        if (speedText == null)
        {
            speedText = FindChildComponent<TMP_Text>("NPCInfo/SpeedText");
        }

        if (itemContentRoot == null)
        {
            itemContentRoot = transform.Find("ItemScrollView/Viewport/Content");
        }

        if (sellButton == null)
        {
            sellButton = FindChildComponent<Button>("SellButton");
        }

        if (backButton == null)
        {
            backButton = FindChildComponent<Button>("BackButton");
        }

        if (feedbackText == null)
        {
            feedbackText = FindChildComponent<TMP_Text>("FeedbackText");
        }

        if (emptyText == null)
        {
            emptyText = FindChildComponent<TMP_Text>("EmptyText");
        }
    }

    private T FindChildComponent<T>(string childPath) where T : Component
    {
        Transform child = transform.Find(childPath);
        return child == null ? null : child.GetComponent<T>();
    }
}
