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

    private NPCDefinition currentNpc;
    private Color normalColor;

    public event Action<NPCItemUI> Clicked;

    public NPCDefinition CurrentNpc => currentNpc;

    public bool HasNpc => currentNpc != null;

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

        gameObject.SetActive(true);
    }

    public void ClearSlot()
    {
        currentNpc = null;

        if (npcAvatar != null)
        {
            npcAvatar.sprite = null;
            npcAvatar.enabled = false;
        }

        if (npcNameText != null)
        {
            npcNameText.text = string.Empty;
        }

        if (npcTittleText != null)
        {
            npcTittleText.text = string.Empty;
        }

        gameObject.SetActive(false);
    }

    public void SetHighlight(bool isHighlighted)
    {
        if (backgroundImage == null)
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
        if (!HasNpc)
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
