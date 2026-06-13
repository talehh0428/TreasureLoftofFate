using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShopMainSceneController : MonoBehaviour
{
    [Header("NPC Setup")]
    [SerializeField] private Transform npcContentRoot;
    [SerializeField] private NPCItemUI npcItemPrefab;
    [SerializeField] private List<NPCDefinition> npcCatalog = new List<NPCDefinition>();

    [Header("Actions")]
    [SerializeField] private Button talkButton;
    [SerializeField] private Button tradeButton;
    [SerializeField] private Button viewButton;
    [SerializeField] private Button leaveButton;

    private readonly List<NPCItemUI> npcPool = new List<NPCItemUI>();
    private readonly List<ShopVisitor> currentRoundVisitors = new List<ShopVisitor>();
    private readonly HashSet<string> talkedNpcIds = new HashSet<string>();
    private readonly HashSet<string> tradedNpcIds = new HashSet<string>();

    private NPCItemUI selectedNpcSlot;
    private ShopVisitor selectedVisitor;

    public event Action<ShopVisitor> VisitorSelected;

    public ShopVisitor SelectedVisitor => selectedVisitor;

    public NPCDefinition SelectedNpc => selectedVisitor?.Definition;

    public Button TalkButton => talkButton;

    public Button TradeButton => tradeButton;

    public Button ViewButton => viewButton;

    public Button LeaveButton => leaveButton;

    private void Awake()
    {
        AutoBindSceneReferences();
    }

    private void OnEnable()
    {
        // 确保进入场景时播放主场景背景音乐
        BgmManager.Instance.PlayMainSceneBGM();
    }

    private void Start()
    {
        PopulateNPCs();
    }

    public void SetNpcCatalog(IEnumerable<NPCDefinition> definitions)
    {
        IEnumerable<ShopVisitor> visitors = definitions == null
            ? Enumerable.Empty<ShopVisitor>()
            : definitions.Where(npc => npc != null).Distinct().Select(ShopVisitor.FromDefinition);

        SetVisitors(visitors);
    }

    public void SetVisitors(IEnumerable<ShopVisitor> visitors)
    {
        currentRoundVisitors.Clear();
        if (visitors != null)
        {
            currentRoundVisitors.AddRange(visitors.Where(visitor => visitor != null));
        }

        npcCatalog = currentRoundVisitors
            .Where(visitor => visitor != null && visitor.Definition != null)
            .Select(visitor => visitor.Definition)
            .Distinct()
            .ToList();

        talkedNpcIds.Clear();
        tradedNpcIds.Clear();
        selectedVisitor = null;
        selectedNpcSlot = null;

        RebuildVisibleVisitorPool();
        RefreshActionButtons();
    }

    public void PopulateNPCs()
    {
        List<ShopVisitor> visitors = npcCatalog
            .Where(npc => npc != null)
            .Distinct()
            .Select(ShopVisitor.FromDefinition)
            .ToList();

        currentRoundVisitors.Clear();
        currentRoundVisitors.AddRange(visitors);
        talkedNpcIds.Clear();
        tradedNpcIds.Clear();
        selectedVisitor = null;
        selectedNpcSlot = null;

        RebuildVisibleVisitorPool();
        RefreshActionButtons();
    }

    public void SetNpcTalked(NPCDefinition talkedNpc)
    {
        SetVisitorTalked(FindVisitorByDefinition(talkedNpc));
    }

    public void SetVisitorTalked(ShopVisitor talkedVisitor)
    {
        if (talkedVisitor == null)
        {
            return;
        }

        talkedNpcIds.Add(GetVisitorStateKey(talkedVisitor));
        RemoveVisitorIfRoundFlowCompleted(talkedVisitor);
        RefreshActionButtons();
    }

    public void SetNpcTraded(NPCDefinition tradedNpc)
    {
        SetVisitorTraded(FindVisitorByDefinition(tradedNpc));
    }

    public void SetVisitorTraded(ShopVisitor tradedVisitor)
    {
        if (tradedVisitor == null)
        {
            return;
        }

        tradedNpcIds.Add(GetVisitorStateKey(tradedVisitor));
        RemoveVisitorIfRoundFlowCompleted(tradedVisitor);
        RefreshActionButtons();
    }

    public bool HasTalked(NPCDefinition npc)
    {
        return HasTalked(FindVisitorByDefinition(npc));
    }

    public bool HasTalked(ShopVisitor visitor)
    {
        return visitor != null && talkedNpcIds.Contains(GetVisitorStateKey(visitor));
    }

    public bool HasTraded(NPCDefinition npc)
    {
        return HasTraded(FindVisitorByDefinition(npc));
    }

    public bool HasTraded(ShopVisitor visitor)
    {
        return visitor != null && tradedNpcIds.Contains(GetVisitorStateKey(visitor));
    }

    public void SetTalkAvailable(bool isAvailable)
    {
        SetButtonAvailable(talkButton, isAvailable);
    }

    public void SetTradeAvailable(bool isAvailable)
    {
        SetButtonAvailable(tradeButton, isAvailable);
    }

    public void ClearSelection()
    {
        selectedVisitor = null;
        selectedNpcSlot = null;
        RefreshNpcHighlights();
        RefreshActionButtons();
    }

    private void HandleNpcClicked(NPCItemUI clickedNpc)
    {
        if (clickedNpc == null || !clickedNpc.HasNpc)
        {
            return;
        }

        selectedNpcSlot = clickedNpc;
        selectedVisitor = clickedNpc.CurrentVisitor;

        RefreshNpcHighlights();
        RefreshActionButtons();

        Debug.Log($"NPC 已选中: {selectedVisitor.DisplayName}");
        VisitorSelected?.Invoke(selectedVisitor);
    }

    private void RebuildVisibleVisitorPool()
    {
        List<ShopVisitor> visibleVisitors = currentRoundVisitors
            .Where(visitor => visitor != null && !IsRoundFlowCompleted(visitor))
            .ToList();

        RebuildVisitorPool(visibleVisitors);

        for (int index = 0; index < npcPool.Count; index++)
        {
            NPCItemUI npcSlot = npcPool[index];
            npcSlot.Clicked -= HandleNpcClicked;
            npcSlot.Clicked += HandleNpcClicked;
            npcSlot.Setup(visibleVisitors[index]);
        }

        if (selectedVisitor != null && !visibleVisitors.Contains(selectedVisitor))
        {
            selectedVisitor = null;
            selectedNpcSlot = null;
        }

        RefreshNpcHighlights();
        RebuildNpcLayout();
    }

    private void RemoveVisitorIfRoundFlowCompleted(ShopVisitor visitor)
    {
        if (visitor == null || !IsRoundFlowCompleted(visitor))
        {
            return;
        }

        RebuildVisibleVisitorPool();
        RefreshActionButtons();
    }

    private void RebuildVisitorPool(IReadOnlyList<ShopVisitor> visitors)
    {
        if (npcContentRoot == null || npcItemPrefab == null)
        {
            return;
        }

        ClearExistingNpcObjects();
        npcPool.Clear();

        for (int index = 0; index < visitors.Count; index++)
        {
            NPCItemUI npcSlot = Instantiate(npcItemPrefab, npcContentRoot);
            npcSlot.name = $"NpcSlot_{index + 1:00}";
            npcSlot.Setup(visitors[index]);
            npcPool.Add(npcSlot);
        }
    }

    private void ClearExistingNpcObjects()
    {
        if (npcContentRoot == null)
        {
            return;
        }

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in npcContentRoot)
        {
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (GameObject childObject in childrenToDestroy)
        {
            Destroy(childObject);
        }
    }

    private void RebuildNpcLayout()
    {
        if (npcContentRoot == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform contentRect = npcContentRoot as RectTransform;
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    private void RefreshNpcHighlights()
    {
        for (int index = 0; index < npcPool.Count; index++)
        {
            NPCItemUI npcSlot = npcPool[index];
            npcSlot.SetHighlight(npcSlot == selectedNpcSlot);
        }
    }

    private void RefreshActionButtons()
    {
        SetButtonAvailable(talkButton, selectedVisitor != null && selectedVisitor.CanTalk && !HasTalked(selectedVisitor));
        SetButtonAvailable(tradeButton, selectedVisitor != null && selectedVisitor.CanTrade && !HasTraded(selectedVisitor));
    }

    private void SetButtonAvailable(Button button, bool isAvailable)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = isAvailable;

        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        button.colors = colors;
    }

    private bool IsRoundFlowCompleted(ShopVisitor visitor)
    {
        bool talkDone = !visitor.CanTalk || HasTalked(visitor);
        bool tradeDone = !visitor.CanTrade || HasTraded(visitor);
        return talkDone && tradeDone;
    }

    private string GetVisitorStateKey(ShopVisitor visitor)
    {
        if (visitor == null)
        {
            return string.Empty;
        }

        return visitor.StateKey;
    }

    private ShopVisitor FindVisitorByDefinition(NPCDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        return currentRoundVisitors.FirstOrDefault(visitor => visitor != null && visitor.Definition == definition);
    }

    private void AutoBindSceneReferences()
    {
        if (npcContentRoot == null)
        {
            Transform npcContent = transform.Find("NpcPanel/NpcScrollView/Viewport/Content");
            if (npcContent != null)
            {
                npcContentRoot = npcContent;
            }
        }

        if (talkButton == null)
        {
            talkButton = FindSceneButton("Btn_交谈");
        }

        if (tradeButton == null)
        {
            tradeButton = FindSceneButton("Btn_交易");
        }

        if (viewButton == null)
        {
            viewButton = FindSceneButton("Btn_查看");
        }

        if (leaveButton == null)
        {
            leaveButton = FindSceneButton("Btn_离开");
        }
    }

    private Button FindSceneButton(string objectName)
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int index = 0; index < buttons.Length; index++)
        {
            if (buttons[index].name == objectName)
            {
                return buttons[index];
            }
        }

        return null;
    }
}
