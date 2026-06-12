using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotPanelController : MonoBehaviour
{
    [Header("Runtime References")]
    [SerializeField] private NPCEventScheduler roundScheduler;
    [SerializeField] private EconomyBuffSystem economyBuffSystem;
    [SerializeField] private MainSceneShopController mainSceneShopController;
    [SerializeField] private string resumeSceneName = "ShopMainScene";

    [Header("UI References")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Button[] saveSlotButtons = new Button[GameSaveService.RunSlotCount];
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text feedbackText;

    private Action onClosed;
    private bool isWaiting;

    private void Awake()
    {
        AutoBind();
        HideImmediate();
    }

    public IEnumerator ShowSaveChoiceRoutine(Action completed)
    {
        ActivateHierarchy();
        AutoBind();
        onClosed = completed;
        isWaiting = true;

        RefreshSlotButtons();
        SetVisible(true);

        while (isWaiting)
        {
            yield return null;
        }
    }

    private void HandleSlotClicked(int slotIndex)
    {
        RunSaveData data = GameSaveService.CaptureRun(
            resumeSceneName,
            roundScheduler,
            economyBuffSystem,
            mainSceneShopController == null ? null : mainSceneShopController.CapturePendingSpecialVisitorNpcIds());
        if (GameSaveService.SaveRunSlot(slotIndex, data))
        {
            SetFeedback($"已保存到存档 {slotIndex + 1}");
        }
        else
        {
            SetFeedback("保存失败");
        }

        Close();
    }

    private void Close()
    {
        SetVisible(false);
        isWaiting = false;
        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        if (visible)
        {
            ActivateHierarchy();
        }

        if (panelGroup == null)
        {
            gameObject.SetActive(visible);
            return;
        }

        panelGroup.alpha = visible ? 1f : 0f;
        panelGroup.interactable = visible;
        panelGroup.blocksRaycasts = visible;
        panelGroup.gameObject.SetActive(visible);
    }

    private void ActivateHierarchy()
    {
        List<Transform> parents = new List<Transform>();
        Transform current = transform;
        while (current != null)
        {
            parents.Add(current);
            current = current.parent;
        }

        for (int index = parents.Count - 1; index >= 0; index--)
        {
            if (!parents[index].gameObject.activeSelf)
            {
                parents[index].gameObject.SetActive(true);
            }
        }
    }

    private void HideImmediate()
    {
        SetVisible(false);
    }

    private void RefreshSlotButtons()
    {
        IReadOnlyList<RunSaveSlotData> slots = GameSaveService.RunSlots;

        for (int index = 0; index < saveSlotButtons.Length; index++)
        {
            Button button = saveSlotButtons[index];
            if (button == null)
            {
                continue;
            }

            int slotIndex = index;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => HandleSlotClicked(slotIndex));

            TMP_Text roundLabel = FindChildText(button, "RoundText");
            TMP_Text moneyLabel = FindChildText(button, "MoneyText");
            TMP_Text timeLabel  = FindChildText(button, "TimeText");

            RunSaveSlotData slot = slotIndex < slots.Count ? slots[slotIndex] : null;

            if (roundLabel != null)
            {
                roundLabel.text = BuildRoundLabel(slotIndex, slot);
            }

            if (moneyLabel != null)
            {
                moneyLabel.text = BuildMoneyLabel(slotIndex, slot);
            }

            if (timeLabel != null)
            {
                timeLabel.text = BuildTimeLabel(slotIndex, slot);
            }
        }
    }

    private static TMP_Text FindChildText(Button button, string childName)
    {
        Transform child = button.transform.Find(childName);
        return child == null ? null : child.GetComponent<TMP_Text>();
    }

    private static string BuildRoundLabel(int slotIndex, RunSaveSlotData slot)
    {
        if (slot == null || !slot.hasData || slot.run == null)
        {
            return "空";
        }

        return $"第{Mathf.Max(1, slot.run.currentRound)}回合";
    }

    private static string BuildMoneyLabel(int slotIndex, RunSaveSlotData slot)
    {
        if (slot == null || !slot.hasData || slot.run == null)
        {
            return string.Empty;
        }

        return $"{slot.run.money}灵石";
    }

    private static string BuildTimeLabel(int slotIndex, RunSaveSlotData slot)
    {
        if (slot == null || !slot.hasData || slot.run == null || string.IsNullOrWhiteSpace(slot.savedAt))
        {
            return string.Empty;
        }

        if (DateTime.TryParse(slot.savedAt, null, DateTimeStyles.RoundtripKind, out DateTime utcTime))
        {
            DateTime localTime = utcTime.ToLocalTime();
            return localTime.ToString("yyyy/MM/dd HH:mm");
        }

        return string.Empty;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    private void AutoBind()
    {
        if (roundScheduler == null)
        {
            roundScheduler = FindObjectOfType<NPCEventScheduler>(true);
        }

        if (economyBuffSystem == null)
        {
            economyBuffSystem = FindObjectOfType<EconomyBuffSystem>(true);
        }

        if (mainSceneShopController == null)
        {
            mainSceneShopController = FindObjectOfType<MainSceneShopController>(true);
        }

        if (panelGroup == null)
        {
            panelGroup = GetComponent<CanvasGroup>();
        }

        if (skipButton == null)
        {
            skipButton = FindButton("SkipButton", "不存档");
        }

        if (titleText == null)
        {
            titleText = FindText("Panel/TitleText");
        }

        if (feedbackText == null)
        {
            feedbackText = FindText("Panel/FeedbackText");
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(Close);
            skipButton.onClick.AddListener(Close);
        }

        if (titleText != null)
        {
            titleText.text = "回合结束，选择存档";
        }
    }

    private Button FindButton(params string[] names)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int index = 0; index < buttons.Length; index++)
        {
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (buttons[index].name == names[nameIndex])
                {
                    return buttons[index];
                }
            }
        }

        return null;
    }

    private TMP_Text FindText(string path)
    {
        Transform child = transform.Find(path);
        return child == null ? null : child.GetComponent<TMP_Text>();
    }
}
