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
    [SerializeField] private TMP_Text npcDescriptionText;

    private NPCDefinition currentNpc;

    public event Action<NPCItemUI> Clicked;

    public NPCDefinition CurrentNpc => currentNpc;

    public bool HasNpc => currentNpc != null;

    private void Awake()
    {
        AutoBind();
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

        if (npcDescriptionText != null)
        {
            npcDescriptionText.text = currentNpc.Description;
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

        if (npcDescriptionText != null)
        {
            npcDescriptionText.text = string.Empty;
        }

        gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasNpc)
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

        if (npcDescriptionText == null)
        {
            npcDescriptionText = FindChildComponent<TMP_Text>("NpcDescription");
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
