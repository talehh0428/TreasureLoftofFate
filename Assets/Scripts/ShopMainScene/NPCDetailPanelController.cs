using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCDetailPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fullBodyImage;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text npcSubtitleText;
    [SerializeField] private TMP_Text npcDescriptionText;

    [Header("Detail Content (for show/hide)")]
    [SerializeField] private GameObject detailContent;

    [Header("Attribute Panel")]
    [SerializeField] private GameObject attributePanel;

    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;
    [SerializeField] private TMP_Text movementSpeedText;

    [Header("Actions")]
    [SerializeField] private Button dialogueButton;

    [Header("Empty State")]
    [SerializeField] [TextArea] private string emptyDescription = "请选择 NPC 查看详情";

    private NPCDefinition currentNpc;

    public event System.Action<NPCDefinition> DialogueRequested;

    /// <summary>由外部控制对话按钮是否可点击</summary>
    public void SetDialogueButtonInteractable(bool interactable)
    {
        if (dialogueButton != null)
        {
            dialogueButton.interactable = interactable;
        }
    }

    private void Awake()
    {
        AutoBind();
        HideDetail();
    }

    private void OnEnable()
    {
        NPCEvents.NPCSelected += HandleNPCSelected;
        NPCEvents.NPCSelectionCleared += HandleNPCSelectionCleared;

        if (dialogueButton != null)
        {
            dialogueButton.onClick.AddListener(HandleDialogueButtonClicked);
        }
    }

    private void OnDisable()
    {
        NPCEvents.NPCSelected -= HandleNPCSelected;
        NPCEvents.NPCSelectionCleared -= HandleNPCSelectionCleared;

        if (dialogueButton != null)
        {
            dialogueButton.onClick.RemoveListener(HandleDialogueButtonClicked);
        }
    }

    private void HandleDialogueButtonClicked()
    {
        if (currentNpc != null)
        {
            DialogueRequested?.Invoke(currentNpc);
        }
    }

    private void HandleNPCSelected(NPCDefinition npc)
    {
        currentNpc = npc;
        ShowDetail();

        if (npc == null)
        {
            ShowEmptyState();
            return;
        }

        if (fullBodyImage != null)
        {
            fullBodyImage.sprite = npc.Avatar;
            fullBodyImage.enabled = npc.Avatar != null;
        }

        if (npcNameText != null)
        {
            npcNameText.text = npc.DisplayName;
        }

        if (npcSubtitleText != null)
        {
            npcSubtitleText.text = npc.Description;
        }

        if (npcDescriptionText != null)
        {
            npcDescriptionText.text = npc.Prompt;
        }

        UpdateAttributePanel(npc);

        if (dialogueButton != null)
        {
            dialogueButton.gameObject.SetActive(true);
        }
    }

    private void UpdateAttributePanel(NPCDefinition npc)
    {
        if (attributePanel != null)
        {
            attributePanel.SetActive(true);
        }

        int displayAttack = NPCInteractionData.GetEffectiveAttack(npc);
        int displayDefense = NPCInteractionData.GetEffectiveDefense(npc);
        int displaySpeed = NPCInteractionData.GetEffectiveSpeed(npc);

        if (attackText != null)
            attackText.text = $"攻击：{displayAttack}";

        if (defenseText != null)
            defenseText.text = $"防御：{displayDefense}";

        if (movementSpeedText != null)
            movementSpeedText.text = $"遁速：{displaySpeed}";
    }

    private void HandleNPCSelectionCleared()
    {
        currentNpc = null;
        HideDetail();
    }

    private void HideDetail()
    {
        // 必须先清空文本再隐藏，否则 inactive 的 GameObject 上的文本修改可能不生效
        ShowEmptyState();
        if (detailContent != null)
        {
            detailContent.SetActive(false);
        }
    }

    private void ShowDetail()
    {
        if (detailContent != null)
        {
            detailContent.SetActive(true);
        }
    }

    private void ShowEmptyState()
    {
        currentNpc = null;

        if (fullBodyImage != null)
        {
            fullBodyImage.sprite = null;
            fullBodyImage.enabled = false;
        }

        if (npcNameText != null)
        {
            npcNameText.text = string.Empty;
        }

        if (npcSubtitleText != null)
        {
            npcSubtitleText.text = string.Empty;
        }

        if (npcDescriptionText != null)
        {
            npcDescriptionText.text = emptyDescription;
        }

        if (attributePanel != null)
        {
            attributePanel.SetActive(false);
        }

        if (dialogueButton != null)
        {
            dialogueButton.gameObject.SetActive(false);
        }
    }

    private void AutoBind()
    {
        // 确保 detailContent 存在
        if (detailContent == null)
        {
            Transform dc = transform.Find("DetailContent");
            if (dc != null) detailContent = dc.gameObject;
        }

        if (detailContent == null)
        {
            Debug.LogWarning("NPCDetailPanelController: detailContent 未找到，无法自动绑定 UI 元素。");
            return;
        }

        Transform root = detailContent.transform;

        if (fullBodyImage == null)
        {
            fullBodyImage = FindChildComponent<Image>(root, "PortraitContainer/FullBodyImage");
        }

        if (npcNameText == null)
        {
            npcNameText = FindChildComponent<TMP_Text>(root, "NPCName");
        }

        if (npcSubtitleText == null)
        {
            npcSubtitleText = FindChildComponent<TMP_Text>(root, "NPCDescription");
        }

        if (npcDescriptionText == null)
        {
            // 尝试查找第二个 NPCDescription（兄弟节点中第一个 NPCDescription 之后的同名节点）
            Transform firstDesc = root.Find("NPCDescription");
            if (firstDesc != null)
            {
                int descIndex = firstDesc.GetSiblingIndex();
                for (int i = descIndex + 1; i < root.childCount; i++)
                {
                    Transform child = root.GetChild(i);
                    if (child.name == "NPCDescription")
                    {
                        npcDescriptionText = child.GetComponent<TMP_Text>();
                        break;
                    }
                }
            }
        }

        if (attributePanel == null)
        {
            Transform attrTrans = root.Find("AttributePanel");
            if (attrTrans != null)
            {
                attributePanel = attrTrans.gameObject;
            }
        }

        // 自动绑定属性文本
        if (attributePanel != null)
        {
            attackText ??= FindChildComponent<TMP_Text>(attributePanel.transform, "AttackText");
            defenseText ??= FindChildComponent<TMP_Text>(attributePanel.transform, "DefenseText");
            movementSpeedText ??= FindChildComponent<TMP_Text>(attributePanel.transform, "MovementSpeedText");
        }

        // 自动绑定对话按钮
        if (dialogueButton == null)
        {
            dialogueButton = FindChildComponent<Button>(root, "DialogueButton");
        }
    }

    private T FindChildComponent<T>(Transform parent, string childPath) where T : Component
    {
        Transform child = parent.Find(childPath);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<T>();
    }
}
