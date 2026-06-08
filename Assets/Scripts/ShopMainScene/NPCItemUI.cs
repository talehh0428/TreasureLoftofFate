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
    [SerializeField] private GameObject spokenBadge;

    private ShopVisitor currentVisitor;
    private Color normalColor;
    private bool hasLeft;

    public event Action<NPCItemUI> Clicked;

    public NPCDefinition CurrentNpc => currentVisitor?.Definition;

    public ShopVisitor CurrentVisitor => currentVisitor;

    public bool HasNpc => currentVisitor != null;

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
        Setup(ShopVisitor.FromDefinition(definition));
    }

    public void Setup(ShopVisitor visitor)
    {
        AutoBind();
        currentVisitor = visitor;
        hasLeft = false;

        if (currentVisitor == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (npcAvatar != null)
        {
            npcAvatar.sprite = currentVisitor.Avatar;
            npcAvatar.enabled = currentVisitor.Avatar != null;
            npcAvatar.color = Color.white;
        }

        if (npcNameText != null)
        {
            npcNameText.text = currentVisitor.DisplayName;
            npcNameText.color = Color.white;
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

    }

    public void ClearSlot()
    {
        currentVisitor = null;
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
