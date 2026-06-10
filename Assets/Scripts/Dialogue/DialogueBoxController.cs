using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueBoxController : MonoBehaviour
{
    private const int ChoiceButtonCount = 3;

    [Header("UI References")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button[] choiceButtons = new Button[ChoiceButtonCount];
    [SerializeField] private TMP_Text[] choiceTexts = new TMP_Text[ChoiceButtonCount];

    [Header("Typing")]
    [SerializeField] private float charactersPerSecond = 35f;

    [Header("Loading")]
    [SerializeField] private string loadingPrefix = string.Empty;
    [SerializeField] private float loadingFrameSeconds = 0.35f;

    private Coroutine typingCoroutine;
    private Coroutine loadingCoroutine;
    private Action<DialogueChoiceResult> pendingChoiceCallback;
    private string activeFullText = string.Empty;
    private DialogueChoice[] activeChoices;

    private void Awake()
    {
        TryAutoBindMissingReferences();
        HideChoices();
    }

    public void Bind(
        CanvasGroup canvasGroup,
        TMP_Text nameLabel,
        Image portrait,
        TMP_Text bodyText,
        Button[] buttons,
        TMP_Text[] buttonTexts)
    {
        rootCanvasGroup = canvasGroup;
        npcNameText = nameLabel;
        portraitImage = portrait;
        dialogueText = bodyText;
        choiceButtons = buttons;
        choiceTexts = buttonTexts;
        HideChoices();
    }

    public void Show(DialogueBody body, Action<DialogueChoiceResult> onChoiceSelected)
    {
        if (body == null)
        {
            Debug.LogWarning("[DialogueBoxController] Dialogue body is null.");
            return;
        }

        TryAutoBindMissingReferences();
        if (!HasRequiredReferences())
        {
            Debug.LogError("[DialogueBoxController] UI references are incomplete. Please assign CanvasGroup, name text, portrait image, dialogue text, three choice buttons, and their TMP labels.");
            return;
        }

        pendingChoiceCallback = onChoiceSelected;
        StopLoading();
        activeFullText = body.text ?? string.Empty;
        activeChoices = body.choices;
        SetVisible(true);
        HideChoices();

        if (npcNameText != null)
        {
            npcNameText.text = body.npcName ?? string.Empty;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = body.portrait;
            portraitImage.enabled = body.portrait != null;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeText(activeFullText, activeChoices));
    }

    public void Hide()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        StopLoading();
        pendingChoiceCallback = null;
        HideChoices();
        SetVisible(false);
    }

    public void ShowLoading(string npcName, Sprite portrait)
    {
        TryAutoBindMissingReferences();
        if (!HasRequiredReferences())
        {
            Debug.LogError("[DialogueBoxController] UI references are incomplete. Please assign CanvasGroup, name text, portrait image, dialogue text, three choice buttons, and their TMP labels.");
            return;
        }

        SetVisible(true);
        HideChoices();

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (npcNameText != null)
        {
            npcNameText.text = npcName ?? string.Empty;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }

        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        loadingCoroutine = StartCoroutine(AnimateLoadingText());
    }

    public void SkipTyping()
    {
        if (typingCoroutine == null)
        {
            return;
        }

        StopCoroutine(typingCoroutine);
        typingCoroutine = null;

        if (dialogueText != null)
        {
            dialogueText.text = activeFullText;
        }

        ShowChoices(activeChoices);
    }

    private IEnumerator TypeText(string text, DialogueChoice[] choices)
    {
        if (dialogueText == null)
        {
            yield break;
        }

        dialogueText.text = string.Empty;

        if (string.IsNullOrEmpty(text))
        {
            ShowChoices(choices);
            typingCoroutine = null;
            yield break;
        }

        float delay = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;
        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }
        }

        typingCoroutine = null;
        ShowChoices(choices);
    }

    private IEnumerator AnimateLoadingText()
    {
        if (dialogueText == null)
        {
            yield break;
        }

        int dotCount = 1;
        while (true)
        {
            dialogueText.text = loadingPrefix + new string('.', dotCount);
            dotCount = dotCount >= 3 ? 1 : dotCount + 1;
            yield return new WaitForSeconds(Mathf.Max(0.05f, loadingFrameSeconds));
        }
    }

    private void StopLoading()
    {
        if (loadingCoroutine == null)
        {
            return;
        }

        StopCoroutine(loadingCoroutine);
        loadingCoroutine = null;
    }

    private void ShowChoices(DialogueChoice[] choices)
    {
        HideChoices();

        if (choices == null)
        {
            return;
        }

        int count = Mathf.Min(choiceButtons.Length, choiceTexts.Length, choices.Length, ChoiceButtonCount);
        for (int i = 0; i < count; i++)
        {
            int index = i;
            DialogueChoice choice = choices[index];
            Button button = choiceButtons[index];
            TMP_Text label = choiceTexts[index];

            if (button == null || label == null)
            {
                continue;
            }

            label.text = choice != null ? choice.text : string.Empty;
            button.gameObject.SetActive(true);
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectChoice(choice, index));
        }
    }

    private void HideChoices()
    {
        if (choiceButtons == null)
        {
            return;
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null)
            {
                continue;
            }

            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].interactable = false;
            choiceButtons[i].gameObject.SetActive(false);
        }
    }

    private void SelectChoice(DialogueChoice choice, int index)
    {
        HideChoices();

        if (choice == null)
        {
            return;
        }

        DialogueChoiceResult result = new DialogueChoiceResult(choice.id, choice.text, index);
        pendingChoiceCallback?.Invoke(result);
    }

    private void TryAutoBindMissingReferences()
    {
        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = GetComponent<CanvasGroup>();
        }

        TMP_Text[] textComponents = GetComponentsInChildren<TMP_Text>(true);
        if (npcNameText == null)
        {
            npcNameText = FindTextByName(textComponents, "NpcNameText", "NPCNameText");
        }

        if (dialogueText == null)
        {
            dialogueText = FindTextByName(textComponents, "DialogueText");
        }

        if (portraitImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].name == "PortraitImage")
                {
                    portraitImage = images[i];
                    break;
                }
            }
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        if (choiceButtons == null || choiceButtons.Length < ChoiceButtonCount)
        {
            choiceButtons = new Button[ChoiceButtonCount];
        }

        for (int i = 0; i < ChoiceButtonCount; i++)
        {
            if (choiceButtons[i] == null)
            {
                choiceButtons[i] = FindButtonByName(buttons, $"ChoiceButton_{i + 1}");
            }
        }

        if (choiceTexts == null || choiceTexts.Length < ChoiceButtonCount)
        {
            choiceTexts = new TMP_Text[ChoiceButtonCount];
        }

        for (int i = 0; i < ChoiceButtonCount; i++)
        {
            if (choiceTexts[i] != null || choiceButtons[i] == null)
            {
                continue;
            }

            choiceTexts[i] = choiceButtons[i].GetComponentInChildren<TMP_Text>(true);
        }
    }

    private TMP_Text FindTextByName(TMP_Text[] textComponents, params string[] names)
    {
        for (int i = 0; i < textComponents.Length; i++)
        {
            for (int j = 0; j < names.Length; j++)
            {
                if (textComponents[i].name == names[j])
                {
                    return textComponents[i];
                }
            }
        }

        return null;
    }

    private Button FindButtonByName(Button[] buttons, string buttonName)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == buttonName)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private bool HasRequiredReferences()
    {
        if (npcNameText == null || portraitImage == null || dialogueText == null)
        {
            return false;
        }

        if (choiceButtons == null || choiceTexts == null || choiceButtons.Length < ChoiceButtonCount || choiceTexts.Length < ChoiceButtonCount)
        {
            return false;
        }

        for (int i = 0; i < ChoiceButtonCount; i++)
        {
            if (choiceButtons[i] == null || choiceTexts[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void SetVisible(bool isVisible)
    {
        if (rootCanvasGroup == null)
        {
            gameObject.SetActive(isVisible);
            return;
        }

        rootCanvasGroup.alpha = isVisible ? 1f : 0f;
        rootCanvasGroup.interactable = isVisible;
        rootCanvasGroup.blocksRaycasts = isVisible;
        gameObject.SetActive(true);
    }
}
