using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShopMainSceneController : MonoBehaviour
{
    [Header("NPC Setup")]
    [SerializeField] private Transform npcContentRoot;
    [SerializeField] private NPCItemUI npcItemPrefab;
    [SerializeField] private List<NPCDefinition> npcCatalog = new List<NPCDefinition>();

    [Header("Interaction Panels")]
    [SerializeField] private NPCDialogueController interactionController;
    [SerializeField] private NPCDetailPanelController detailPanel;

    [Header("RightPanel Buttons")]
    [SerializeField] private Button rightPanelDialogueButton;
    [SerializeField] private Button rightPanelTradeButton;

    [Header("Leave Button")]
    [SerializeField] private Button leaveButton;

    [Header("Scene Transition")]
    [SerializeField] private string exitSceneName = "shichang";

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float feedbackDuration = 2f;

    private readonly List<NPCItemUI> npcPool = new List<NPCItemUI>();
    private NPCDefinition selectedNpc;
    private Coroutine feedbackRoutine;

    // --- 状态管理 ---
    private readonly HashSet<NPCDefinition> spokenNpcs = new HashSet<NPCDefinition>();
    private readonly HashSet<NPCDefinition> leftNpcs = new HashSet<NPCDefinition>();
    private int dailyEarnings;

    private void Awake()
    {
        AutoBindSceneReferences();
    }

    private void Start()
    {
        PopulateNPCs();
        WireInteractionFlow();
        ResetRightPanelButtons();
    }

    public void PopulateNPCs()
    {
        List<NPCDefinition> validNpcs = npcCatalog
            .Where(npc => npc != null)
            .Distinct()
            .ToList();

        if (validNpcs.Count == 0)
        {
            return;
        }

        RebuildNpcPool(validNpcs);

        for (int index = 0; index < npcPool.Count; index++)
        {
            NPCItemUI npcSlot = npcPool[index];
            npcSlot.Setup(validNpcs[index]);
        }

        // 重置状态
        spokenNpcs.Clear();
        leftNpcs.Clear();
        dailyEarnings = 0;

        RebuildNpcLayout();
    }

    private void WireInteractionFlow()
    {
        // 监听 NPC 选中事件
        NPCEvents.NPCSelected += HandleNPCSelected;
        NPCEvents.NPCSelectionCleared += HandleNPCSelectionCleared;

        if (interactionController != null)
        {
            interactionController.DialogueEnded += HandleDialogueEnded;
            interactionController.NextRequested += HandleTradeNext;
        }

        if (detailPanel != null)
        {
            detailPanel.DialogueRequested += OpenDialogue;
        }
    }

    private void OnDestroy()
    {
        NPCEvents.NPCSelected -= HandleNPCSelected;
        NPCEvents.NPCSelectionCleared -= HandleNPCSelectionCleared;

        if (interactionController != null)
        {
            interactionController.DialogueEnded -= HandleDialogueEnded;
            interactionController.NextRequested -= HandleTradeNext;
        }

        if (detailPanel != null)
        {
            detailPanel.DialogueRequested -= OpenDialogue;
        }

        if (rightPanelDialogueButton != null)
        {
            rightPanelDialogueButton.onClick.RemoveListener(HandleRightPanelDialogueClicked);
        }

        if (rightPanelTradeButton != null)
        {
            rightPanelTradeButton.onClick.RemoveListener(HandleRightPanelTradeClicked);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(HandleLeaveClicked);
        }
    }

    private void HandleNPCSelected(NPCDefinition npc)
    {
        selectedNpc = npc;
        UpdateRightPanelButtons(npc);
    }

    private void HandleNPCSelectionCleared()
    {
        selectedNpc = null;
        ResetRightPanelButtons();
    }

    /// <summary>RightPanel 交谈按钮点击</summary>
    private void HandleRightPanelDialogueClicked()
    {
        if (selectedNpc == null)
        {
            ShowFeedback("请先在左侧列表中选择一个 NPC。");
            return;
        }

        if (leftNpcs.Contains(selectedNpc))
        {
            ShowFeedback($"{selectedNpc.DisplayName} 已离开，无法交谈。");
            return;
        }

        if (spokenNpcs.Contains(selectedNpc))
        {
            ShowFeedback($"{selectedNpc.DisplayName} 已交谈过，无法再次交谈。");
            return;
        }

        OpenDialogue(selectedNpc);
    }

    /// <summary>RightPanel 交易按钮点击</summary>
    private void HandleRightPanelTradeClicked()
    {
        if (selectedNpc == null)
        {
            ShowFeedback("请先在左侧列表中选择一个 NPC。");
            return;
        }

        if (leftNpcs.Contains(selectedNpc))
        {
            ShowFeedback($"{selectedNpc.DisplayName} 已离开，无法交易。");
            return;
        }

        if (!spokenNpcs.Contains(selectedNpc))
        {
            ShowFeedback($"请先与 {selectedNpc.DisplayName} 交谈。");
            return;
        }

        OpenTrade(selectedNpc);
    }

    private void OpenDialogue(NPCDefinition npc)
    {
        if (interactionController == null)
        {
            ShowFeedback("交互面板未绑定，无法打开对话。");
            return;
        }

        if (spokenNpcs.Contains(npc))
        {
            ShowFeedback($"{npc.DisplayName} 已交谈过，无法再次交谈。");
            return;
        }

        interactionController.ShowDialogue(npc);
    }

    private void OpenTrade(NPCDefinition npc)
    {
        if (interactionController == null)
        {
            ShowFeedback("交互面板(NPCInteractionPanel)未绑定，无法交易。");
            return;
        }

        interactionController.ShowTrade(npc);
    }

    /// <summary>
    /// 交易结束回调。
    /// earnings 表示本次交易卖出的灵石总额（0 表示未出售）。
    /// </summary>
    private void HandleTradeNext(NPCDefinition npc, int earnings)
    {
        if (npc == null) return;

        dailyEarnings += earnings;
        MarkNpcAsLeft(npc);
    }

    private void HandleDialogueEnded(NPCDefinition npc)
    {
        if (npc == null) return;

        spokenNpcs.Add(npc);

        // 更新该 NPC 的视觉状态（显示交谈徽章）
        NPCItemUI slot = npcPool.FirstOrDefault(s => s.CurrentNpc == npc);
        if (slot != null)
        {
            slot.SetSpoken(true);
        }

        // 如果当前选中的就是这个 NPC，立即更新按钮（交易按钮应当可用）
        if (selectedNpc == npc)
        {
            UpdateRightPanelButtons(npc);
        }
    }

    /// <summary>将 NPC 标记为已离开，从列表中移除</summary>
    private void MarkNpcAsLeft(NPCDefinition npc)
    {
        leftNpcs.Add(npc);

        // 从列表中真正移除（销毁 GameObject）
        NPCItemUI slot = npcPool.FirstOrDefault(s => s.CurrentNpc == npc);
        if (slot != null)
        {
            npcPool.Remove(slot);
            Destroy(slot.gameObject);
        }

        // 清除选中状态
        if (selectedNpc == npc)
        {
            NPCEvents.RaiseNPCSelectionCleared();
        }

        RebuildNpcLayout();

        // 检查是否所有活跃 NPC 都已离开
        CheckAllNpcsLeft();
    }

    /// <summary>选中 NPC 时更新右侧按钮状态（每个 NPC 独立控制）</summary>
    private void UpdateRightPanelButtons(NPCDefinition npc)
    {
        if (npc == null)
        {
            ResetRightPanelButtons();
            return;
        }

        bool alreadySpoken = spokenNpcs.Contains(npc);
        bool hasLeft = leftNpcs.Contains(npc);
        bool dialogueAllowed = !alreadySpoken && !hasLeft;
        bool tradeAllowed = alreadySpoken && !hasLeft;

        // 交谈按钮：已交谈过或已离开则禁用
        if (rightPanelDialogueButton != null)
        {
            rightPanelDialogueButton.interactable = dialogueAllowed;
            rightPanelDialogueButton.gameObject.SetActive(true);
        }

        // 详情面板的对话按钮同步
        if (detailPanel != null)
        {
            detailPanel.SetDialogueButtonInteractable(dialogueAllowed);
        }

        // 交易按钮：该 NPC 已交谈且未离开 → 可交易
        if (rightPanelTradeButton != null)
        {
            rightPanelTradeButton.interactable = tradeAllowed;
            rightPanelTradeButton.gameObject.SetActive(true);
        }
    }

    /// <summary>清除选中时重置按钮状态</summary>
    private void ResetRightPanelButtons()
    {
        if (rightPanelDialogueButton != null)
        {
            rightPanelDialogueButton.interactable = false;
            rightPanelDialogueButton.gameObject.SetActive(true);
        }

        if (detailPanel != null)
        {
            detailPanel.SetDialogueButtonInteractable(false);
        }

        if (rightPanelTradeButton != null)
        {
            rightPanelTradeButton.interactable = false;
            rightPanelTradeButton.gameObject.SetActive(true);
        }
    }

    /// <summary>检查所有 NPC 是否都已离开，如果是则提示</summary>
    private void CheckAllNpcsLeft()
    {
        if (npcPool.Count == 0)
        {
            ShowFeedback("所有 NPC 都已离开，点击「离开」结算。");
        }
    }

    // ========== 离开按钮 ==========

    private void HandleLeaveClicked()
    {
        if (string.IsNullOrWhiteSpace(exitSceneName))
        {
            ShowFeedback("目标场景名未配置。");
            return;
        }

        SceneManager.LoadScene(exitSceneName);
    }

    // ========== 游戏内反馈 ==========

    private void ShowFeedback(string message)
    {
        Debug.Log($"ShopMainSceneController: {message}");

        if (feedbackText == null)
        {
            // 没有反馈文本组件时尝试自动查找
            GameObject shopMainPanelObj = GameObject.Find("ShopMainPanel");
            if (shopMainPanelObj != null)
            {
                Transform fb = shopMainPanelObj.transform.Find("FeedbackText");
                if (fb != null)
                {
                    feedbackText = fb.GetComponent<TMP_Text>();
                }
            }
        }

        if (feedbackText == null) return;

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(FeedbackRoutine(message));
    }

    private IEnumerator FeedbackRoutine(string message)
    {
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = message;

        yield return new WaitForSeconds(feedbackDuration);

        feedbackText.text = string.Empty;
        feedbackText.gameObject.SetActive(false);
        feedbackRoutine = null;
    }

    // ========== UI 重建 ==========

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

    private void AutoBindSceneReferences()
    {
        // ShopMainSceneController 在场景根层级，不在 ShopMainPanel 下
        // 通过 GameObject.Find 获取 ShopMainPanel
        GameObject shopMainPanelObj = GameObject.Find("ShopMainPanel");
        Transform shopMainPanel = shopMainPanelObj != null ? shopMainPanelObj.transform : transform;

        if (npcContentRoot == null)
        {
            Transform npcContent = shopMainPanel.Find("LeftPanel/NPCScrollView/Viewport/Content");
            if (npcContent != null)
            {
                npcContentRoot = npcContent;
            }
        }

        // 自动绑定 detailPanel（NPCDetailPanel 实例在 ShopMainPanel 下）
        if (detailPanel == null)
        {
            Transform detailTransform = shopMainPanel.Find("NPCDetailPanel");
            if (detailTransform != null)
            {
                detailPanel = detailTransform.GetComponent<NPCDetailPanelController>();
            }
        }

        // 自动绑定 interactionController（NPCInteractionPanel 实例在 ShopMainPanel 下）
        if (interactionController == null)
        {
            Transform interactionTransform = shopMainPanel.Find("NPCInteractionPanel");
            if (interactionTransform == null)
            {
                // 兼容旧名称查找
                interactionTransform = shopMainPanel.Find("NPCDialoguePanel");
            }
            if (interactionTransform != null)
            {
                interactionController = interactionTransform.GetComponent<NPCDialogueController>();
            }
        }

        // 自动绑定 RightPanel 交谈按钮（优先匹配英文名 Btn_Dialogue）
        if (rightPanelDialogueButton == null)
        {
            Transform btn = FindButtonInHierarchy(shopMainPanel, new[] {
                "RightPanel/Btn_Dialogue",
                "RightPanel/Btn_Talk",
                "RightPanel/Btn_交谈",
                "RightPanel/DialogueButton",
                "RightPanel/交谈",
                "Btn_Dialogue", "Btn_Talk", "Btn_交谈"
            });
            if (btn != null)
            {
                rightPanelDialogueButton = btn.GetComponent<Button>();
            }
        }

        // 自动绑定 RightPanel 交易按钮（优先匹配英文名 Btn_Trade）
        if (rightPanelTradeButton == null)
        {
            Transform btn = FindButtonInHierarchy(shopMainPanel, new[] {
                "RightPanel/Btn_Trade",
                "RightPanel/Btn_Deal",
                "RightPanel/Btn_交易",
                "RightPanel/TradeButton",
                "RightPanel/交易",
                "Btn_Trade", "Btn_Deal", "Btn_交易"
            });
            if (btn != null)
            {
                rightPanelTradeButton = btn.GetComponent<Button>();
            }
        }

        // 自动绑定反馈文本
        if (feedbackText == null)
        {
            Transform fb = shopMainPanel.Find("FeedbackText");
            if (fb == null)
            {
                GameObject fbObj = GameObject.Find("FeedbackText");
                if (fbObj != null) fb = fbObj.transform;
            }
            if (fb != null)
            {
                feedbackText = fb.GetComponent<TMP_Text>();
                if (feedbackText != null) feedbackText.gameObject.SetActive(false);
            }
        }

        // 自动绑定离开按钮（优先匹配英文名 Btn_Leave）
        if (leaveButton == null)
        {
            Transform btn = FindButtonInHierarchy(shopMainPanel, new[] {
                "RightPanel/Btn_Leave",
                "RightPanel/Btn_离开",
                "RightPanel/LeaveButton",
                "RightPanel/离开",
                "Btn_Leave", "Btn_离开"
            });
            if (btn == null)
            {
                // 在场景根层级查找
                GameObject leaveObj = GameObject.Find("Btn_Leave");
                if (leaveObj == null) leaveObj = GameObject.Find("Btn_离开");
                if (leaveObj != null) btn = leaveObj.transform;
            }
            if (btn != null)
            {
                leaveButton = btn.GetComponent<Button>();
            }
        }

        // 在 AutoBind 中直接添加监听（避免 WireInteractionFlow 时按钮为 null）
        if (rightPanelDialogueButton != null)
            rightPanelDialogueButton.onClick.AddListener(HandleRightPanelDialogueClicked);
        if (rightPanelTradeButton != null)
            rightPanelTradeButton.onClick.AddListener(HandleRightPanelTradeClicked);
        if (leaveButton != null)
            leaveButton.onClick.AddListener(HandleLeaveClicked);
    }

    /// <summary>在多个候选路径中查找按钮</summary>
    private Transform FindButtonInHierarchy(Transform root, string[] candidatePaths)
    {
        foreach (string path in candidatePaths)
        {
            Transform t = root.Find(path);
            if (t != null) return t;
        }
        return null;
    }
}
