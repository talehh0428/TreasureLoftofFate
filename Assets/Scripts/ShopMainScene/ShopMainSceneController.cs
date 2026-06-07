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
    private readonly List<NPCDefinition> currentRoundNpcs = new List<NPCDefinition>();
    private readonly HashSet<string> talkedNpcIds = new HashSet<string>();
    private readonly HashSet<string> tradedNpcIds = new HashSet<string>();

    private NPCItemUI selectedNpcSlot;
    private NPCDefinition selectedNpc;

    public event Action<NPCDefinition> NpcSelected;

    public NPCDefinition SelectedNpc => selectedNpc;

    public Button TalkButton => talkButton;

    public Button TradeButton => tradeButton;

    public Button ViewButton => viewButton;

    public Button LeaveButton => leaveButton;

    private void Awake()
    {
        AutoBindSceneReferences();
    }

    private void Start()
    {
        PopulateNPCs();
    }

    public void SetNpcCatalog(IEnumerable<NPCDefinition> definitions)
    {
        npcCatalog = definitions == null
            ? new List<NPCDefinition>()
            : definitions.Where(npc => npc != null).Distinct().ToList();

        PopulateNPCs();
    }

    public void PopulateNPCs()
    {
        List<NPCDefinition> validNpcs = npcCatalog
            .Where(npc => npc != null)
            .Distinct()
            .ToList();

        currentRoundNpcs.Clear();
        currentRoundNpcs.AddRange(validNpcs);
        talkedNpcIds.Clear();
        tradedNpcIds.Clear();
        selectedNpc = null;
        selectedNpcSlot = null;

        RebuildVisibleNpcPool();
        RefreshActionButtons();
    }

    public void SetNpcTalked(NPCDefinition talkedNpc)
    {
        if (talkedNpc == null)
        {
            return;
        }

        talkedNpcIds.Add(GetNpcStateKey(talkedNpc));
        RemoveNpcIfRoundFlowCompleted(talkedNpc);
        RefreshActionButtons();
    }

    public void SetNpcTraded(NPCDefinition tradedNpc)
    {
        if (tradedNpc == null)
        {
            return;
        }

        tradedNpcIds.Add(GetNpcStateKey(tradedNpc));
        RemoveNpcIfRoundFlowCompleted(tradedNpc);
        RefreshActionButtons();
    }

    public bool HasTalked(NPCDefinition npc)
    {
        return npc != null && talkedNpcIds.Contains(GetNpcStateKey(npc));
    }

    public bool HasTraded(NPCDefinition npc)
    {
        return npc != null && tradedNpcIds.Contains(GetNpcStateKey(npc));
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
        selectedNpc = null;
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
        selectedNpc = clickedNpc.CurrentNpc;

        RefreshNpcHighlights();
        RefreshActionButtons();

        Debug.Log($"NPC 已选中: {selectedNpc.DisplayName}");
        NpcSelected?.Invoke(selectedNpc);
    }

    private void RebuildVisibleNpcPool()
    {
        List<NPCDefinition> visibleNpcs = currentRoundNpcs
            .Where(npc => npc != null && !IsRoundFlowCompleted(npc))
            .ToList();

        RebuildNpcPool(visibleNpcs);

        for (int index = 0; index < npcPool.Count; index++)
        {
            NPCItemUI npcSlot = npcPool[index];
            npcSlot.Clicked -= HandleNpcClicked;
            npcSlot.Clicked += HandleNpcClicked;
            npcSlot.Setup(visibleNpcs[index]);
        }

        if (selectedNpc != null && !visibleNpcs.Contains(selectedNpc))
        {
            selectedNpc = null;
            selectedNpcSlot = null;
        }

        RefreshNpcHighlights();
        RebuildNpcLayout();
    }

    private void RemoveNpcIfRoundFlowCompleted(NPCDefinition npc)
    {
        if (npc == null || !IsRoundFlowCompleted(npc))
        {
            return;
        }

        RebuildVisibleNpcPool();
        RefreshActionButtons();
    }

    private void RebuildNpcPool(IReadOnlyList<NPCDefinition> npcDefinitions)
    {
        if (npcContentRoot == null || npcItemPrefab == null)
        {
            return;
        }

        ClearExistingNpcObjects();
        npcPool.Clear();

        for (int index = 0; index < npcDefinitions.Count; index++)
        {
            NPCItemUI npcSlot = Instantiate(npcItemPrefab, npcContentRoot);
            npcSlot.name = $"NpcSlot_{index + 1:00}";
            npcSlot.Setup(npcDefinitions[index]);
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
        SetButtonAvailable(talkButton, selectedNpc != null && !HasTalked(selectedNpc));
        SetButtonAvailable(tradeButton, selectedNpc != null && !HasTraded(selectedNpc));
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

    private bool IsRoundFlowCompleted(NPCDefinition npc)
    {
        return HasTalked(npc) && HasTraded(npc);
    }

    private string GetNpcStateKey(NPCDefinition npc)
    {
        if (npc == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(npc.NpcId)
            ? npc.GetInstanceID().ToString()
            : npc.NpcId;
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
