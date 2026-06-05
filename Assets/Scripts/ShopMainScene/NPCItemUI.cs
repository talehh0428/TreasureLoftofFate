using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class NPCItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image npcAvatar;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text npcTittleText;
    [SerializeField] private GameObject spokenBadge;

    private NPCDefinition currentNpc;
    private Color normalColor;
    private bool hasLeft;

    public event Action<NPCItemUI> Clicked;

    public NPCDefinition CurrentNpc => currentNpc;

    public bool HasNpc => currentNpc != null;

    public bool HasLeft => hasLeft;

    private void Awake()
    {
        AutoBind();
        CacheNormalColor();
    }

    private void Reset()
    {
        AutoBind();
    }

    private void OnValidate()
    {
        AutoBind();
    }

    public void Setup(NPCDefinition definition)
    {
        AutoBind();
        currentNpc = definition;
        hasLeft = false;

        if (currentNpc == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (npcAvatar != null)
        {
            npcAvatar.sprite = currentNpc.Avatar;
            npcAvatar.enabled = currentNpc.Avatar != null;
        }

        if (npcNameText != null)
        {
            npcNameText.text = currentNpc.DisplayName;
        }

        if (npcTittleText != null)
        {
            npcTittleText.text = currentNpc.Description;
        }

        if (spokenBadge != null)
        {
            spokenBadge.SetActive(false);
        }

        // 恢复正常的颜色
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }

        gameObject.SetActive(true);
    }

    /// <summary>标记该 NPC 已经交谈过</summary>
    public void SetSpoken(bool spoken)
    {
        if (spokenBadge != null)
        {
            spokenBadge.SetActive(spoken);
        }
    }

    /// <summary>标记该 NPC 已交易离开（禁用点击，变为灰色）</summary>
    public void MarkAsLeft()
    {
        hasLeft = true;

        if (backgroundImage != null)
        {
            Color grayColor = normalColor;
            grayColor.a *= 0.4f;
            backgroundImage.color = grayColor;
        }

        if (npcAvatar != null)
        {
            npcAvatar.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }

        if (npcNameText != null)
        {
            npcNameText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }

        if (npcTittleText != null)
        {
            npcTittleText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    public void ClearSlot()
    {
        currentNpc = null;
        hasLeft = false;

        if (npcAvatar != null)
        {
            npcAvatar.sprite = null;
            npcAvatar.enabled = false;
            npcAvatar.color = Color.white;
        }

        if (npcNameText != null)
        {
            npcNameText.text = string.Empty;
            npcNameText.color = Color.white;
        }

        if (npcTittleText != null)
        {
            npcTittleText.text = string.Empty;
            npcTittleText.color = Color.white;
        }

        if (spokenBadge != null)
        {
            spokenBadge.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    public void SetHighlight(bool isHighlighted)
    {
        if (backgroundImage == null || hasLeft)
        {
            return;
        }

        backgroundImage.color = isHighlighted
            ? new Color(0.35f, 0.25f, 0.1f, 0.95f)
            : normalColor;
    }

    private void CacheNormalColor()
    {
        if (backgroundImage != null)
        {
            normalColor = backgroundImage.color;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasNpc || hasLeft)
        {
            return;
        }

        Clicked?.Invoke(this);
        NPCEvents.RaiseNPCSelected(currentNpc);
    }

    private void AutoBind()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (npcAvatar == null)
        {
            npcAvatar = FindChildComponent<Image>("NpcAvatar");
        }

        if (npcNameText == null)
        {
            npcNameText = FindChildComponent<TMP_Text>("NpcName");
        }

        if (npcTittleText == null)
        {
            npcTittleText = FindChildComponent<TMP_Text>("NpcDescription");
        }

        if (spokenBadge == null)
        {
            Transform badgeTrans = transform.Find("SpokenBadge");
            if (badgeTrans != null)
            {
                spokenBadge = badgeTrans.gameObject;
            }
        }
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<T>();
    }
}
