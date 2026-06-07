using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC interaction panel controller for dialogue. Trade is now handled by TradeScene.
/// </summary>
public class NPCDialogueController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private GameObject dialogueContent;
    [SerializeField] private Image npcAvatar;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button tradeButton;

    [Header("Dialogue Settings")]
    [SerializeField] private float textSpeed = 0.05f;

    public event Action<NPCDefinition> DialogueEnded;
    public event Action<NPCDefinition> TradeRequested;

    private NPCDefinition currentNpc;
    private Coroutine typewriterRoutine;
    private bool isTypewriterPlaying;
    private bool isVisible;

    public bool IsVisible => isVisible;
    public NPCDefinition CurrentNpc => currentNpc;

    private void Awake()
    {
        AutoBind();
        Hide();
    }

    private void OnEnable()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(HandleContinueClicked);
        }

        if (tradeButton != null)
        {
            tradeButton.onClick.AddListener(HandleTradeClicked);
        }
    }

    private void OnDisable()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(HandleContinueClicked);
        }

        if (tradeButton != null)
        {
            tradeButton.onClick.RemoveListener(HandleTradeClicked);
        }

        StopTypewriter();
    }

    public void ShowDialogue(NPCDefinition npc)
    {
        if (npc == null)
        {
            return;
        }

        currentNpc = npc;
        isVisible = true;

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);
        }

        if (dialogueContent != null)
        {
            dialogueContent.SetActive(true);
        }

        gameObject.SetActive(true);

        if (npcAvatar != null)
        {
            npcAvatar.sprite = npc.Avatar;
            npcAvatar.enabled = npc.Avatar != null;
        }

        if (npcNameText != null)
        {
            npcNameText.text = npc.DisplayName;
        }

        if (continueButton != null)
        {
            continueButton.interactable = false;
        }

        if (tradeButton != null)
        {
            tradeButton.gameObject.SetActive(false);
        }

        StartTypewriter(npc.Description);
    }

    public void ShowTrade(NPCDefinition npc)
    {
        if (npc == null)
        {
            return;
        }

        currentNpc = npc;
        TradeRequested?.Invoke(currentNpc);
    }

    public void Hide()
    {
        isVisible = false;
        StopTypewriter();
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

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        if (dialogueContent != null)
        {
            dialogueContent.SetActive(false);
        }

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }

    public void Refresh()
    {
        if (!isVisible || currentNpc == null)
        {
            return;
        }

        if (npcNameText != null)
        {
            npcNameText.text = currentNpc.DisplayName;
        }
    }

    private void HandleContinueClicked()
    {
        if (isTypewriterPlaying)
        {
            StopTypewriter();

            if (dialogueText != null && currentNpc != null)
            {
                dialogueText.text = currentNpc.Description;
            }

            if (continueButton != null)
            {
                continueButton.interactable = true;
            }

            if (tradeButton != null)
            {
                tradeButton.gameObject.SetActive(true);
            }

            return;
        }

        DialogueEnded?.Invoke(currentNpc);
        Hide();
    }

    private void HandleTradeClicked()
    {
        if (currentNpc == null)
        {
            return;
        }

        TradeRequested?.Invoke(currentNpc);
    }

    private void StartTypewriter(string text)
    {
        StopTypewriter();

        if (string.IsNullOrEmpty(text))
        {
            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
            }

            if (continueButton != null)
            {
                continueButton.interactable = true;
            }

            if (tradeButton != null)
            {
                tradeButton.gameObject.SetActive(true);
            }

            return;
        }

        typewriterRoutine = StartCoroutine(TypewriterRoutine(text));
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        isTypewriterPlaying = true;

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        for (int index = 0; index < text.Length; index++)
        {
            if (dialogueText != null)
            {
                dialogueText.text += text[index];
            }

            yield return new WaitForSeconds(textSpeed);
        }

        isTypewriterPlaying = false;

        if (continueButton != null)
        {
            continueButton.interactable = true;
        }

        if (tradeButton != null)
        {
            tradeButton.gameObject.SetActive(true);
        }
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

    private void AutoBind()
    {
        if (interactionPanel == null)
        {
            interactionPanel = gameObject;
        }

        if (dialogueContent == null)
        {
            Transform content = transform.Find("DialogueContent");
            if (content != null)
            {
                dialogueContent = content.gameObject;
            }
        }

        Transform root = dialogueContent == null ? transform : dialogueContent.transform;

        if (npcAvatar == null)
        {
            npcAvatar = FindChildComponent<Image>(root, "NPCInfo/NPCAvatar");
        }

        if (npcNameText == null)
        {
            npcNameText = FindChildComponent<TMP_Text>(root, "NPCInfo/NPCNameText");
        }

        if (dialogueText == null)
        {
            dialogueText = FindChildComponent<TMP_Text>(root, "DialogueText");
        }

        if (continueButton == null)
        {
            continueButton = FindChildComponent<Button>(root, "ContinueButton");
        }

        if (tradeButton == null)
        {
            tradeButton = FindChildComponent<Button>(root, "TradeButton");
        }
    }

    private T FindChildComponent<T>(Transform parent, string childPath) where T : Component
    {
        Transform child = parent.Find(childPath);
        return child == null ? null : child.GetComponent<T>();
    }
}
