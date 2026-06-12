using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    private const string MasterVolumeKey = "TreasureLoftOfFate.Settings.MasterVolume";

    [Header("References")]
    [SerializeField] private StartMenuController startMenuController;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeText;

    [Header("Save Slots")]
    [SerializeField] private Transform[] saveSlotRoots = new Transform[GameSaveService.RunSlotCount];
    private Button[] loadSlotButtons = new Button[GameSaveService.RunSlotCount];
    private Button[] deleteSlotButtons = new Button[GameSaveService.RunSlotCount];

    [Header("Danger Zone")]
    [SerializeField] private Button clearAllPersistentDataButton;
    [SerializeField] private TMP_Text feedbackText;

    private Coroutine feedbackHideCoroutine;

    private void Awake()
    {
        AutoBind();
        ApplySavedVolume();
        RefreshSaveSlots();

        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        AddListeners();
        ApplySavedVolume();
        RefreshSaveSlots();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void RefreshSaveSlots()
    {
        AutoBind();

        for (int index = 0; index < GameSaveService.RunSlotCount; index++)
        {
            bool hasSlot = GameSaveService.TryGetRunSlot(index, out RunSaveSlotData slot);

            Button loadButton = loadSlotButtons[index];
            Button deleteButton = deleteSlotButtons[index];
            Transform root = saveSlotRoots[index];

            if (loadButton != null)
            {
                loadButton.interactable = hasSlot;
            }

            if (deleteButton != null)
            {
                deleteButton.interactable = hasSlot;
            }

            if (root != null)
            {
                TMP_Text roundLabel = FindChildText(root, "RoundText");
                TMP_Text moneyLabel = FindChildText(root, "MoneyText");
                TMP_Text timeLabel  = FindChildText(root, "TimeText");

                RunSaveSlotData slotData = hasSlot ? slot : null;

                if (roundLabel != null)
                {
                    roundLabel.text = BuildRoundLabel(index, slotData);
                }

                if (moneyLabel != null)
                {
                    moneyLabel.text = BuildMoneyLabel(index, slotData);
                }

                if (timeLabel != null)
                {
                    timeLabel.text = BuildTimeLabel(index, slotData);
                }
            }
        }
    }

    private void ApplySavedVolume()
    {
        float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        AudioListener.volume = volume;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(volume);
        }

        RefreshVolumeText(volume);
    }

    private void HandleVolumeChanged(float value)
    {
        float volume = Mathf.Clamp01(value);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(MasterVolumeKey, volume);
        PlayerPrefs.Save();
        RefreshVolumeText(volume);
    }

    private void HandleClearAllPersistentDataClicked()
    {
        GameSaveService.ClearAllPersistentData();
        GameSaveService.LoadArchiveIntoRuntime();
        RefreshSaveSlots();

        if (startMenuController != null)
        {
            startMenuController.RefreshSaveSlotButtons();
        }

        SetFeedback("已清除所有图鉴、人物结局和游戏存档。");
    }

    private void LoadSlot(int slotIndex)
    {
        if (startMenuController == null)
        {
            SetFeedback("未绑定 StartMenuController。");
            return;
        }

        startMenuController.LoadGame(slotIndex);
    }

    private void DeleteSlot(int slotIndex)
    {
        if (!GameSaveService.DeleteRunSlot(slotIndex))
        {
            SetFeedback($"存档 {slotIndex + 1} 删除失败。");
            return;
        }

        RefreshSaveSlots();
        if (startMenuController != null)
        {
            startMenuController.RefreshSaveSlotButtons();
        }

        SetFeedback($"已删除存档 {slotIndex + 1}。");
    }

    private void RefreshVolumeText(float volume)
    {
        if (masterVolumeText != null)
        {
            masterVolumeText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
        }
    }

    private static TMP_Text FindChildText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
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
        if (feedbackText == null)
        {
            return;
        }

        feedbackText.gameObject.SetActive(true);
        feedbackText.text = message;

        if (feedbackHideCoroutine != null)
        {
            StopCoroutine(feedbackHideCoroutine);
        }

        feedbackHideCoroutine = StartCoroutine(HideFeedbackAfterDelay(2f));
    }

    private System.Collections.IEnumerator HideFeedbackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }

        feedbackHideCoroutine = null;
    }

    private void AddListeners()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
        }

        for (int index = 0; index < GameSaveService.RunSlotCount; index++)
        {
            int slotIndex = index;
            if (loadSlotButtons[index] != null)
            {
                loadSlotButtons[index].onClick.AddListener(() => LoadSlot(slotIndex));
            }

            if (deleteSlotButtons[index] != null)
            {
                deleteSlotButtons[index].onClick.AddListener(() => DeleteSlot(slotIndex));
            }
        }

        if (clearAllPersistentDataButton != null)
        {
            clearAllPersistentDataButton.onClick.AddListener(HandleClearAllPersistentDataClicked);
        }
    }

    private void RemoveListeners()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
        }

        for (int index = 0; index < GameSaveService.RunSlotCount; index++)
        {
            if (loadSlotButtons[index] != null)
            {
                loadSlotButtons[index].onClick.RemoveAllListeners();
            }

            if (deleteSlotButtons[index] != null)
            {
                deleteSlotButtons[index].onClick.RemoveAllListeners();
            }
        }

        if (clearAllPersistentDataButton != null)
        {
            clearAllPersistentDataButton.onClick.RemoveListener(HandleClearAllPersistentDataClicked);
        }
    }

    private void AutoBind()
    {
        if (startMenuController == null)
        {
            startMenuController = FindObjectOfType<StartMenuController>(true);
        }

        if (masterVolumeSlider == null)
        {
            masterVolumeSlider = FindChildComponent<Slider>("MasterVolumeSlider", "VolumeSlider");
        }

        if (masterVolumeText == null)
        {
            masterVolumeText = FindChildComponent<TMP_Text>("MasterVolumeText", "VolumeValueText");
        }

        for (int index = 0; index < GameSaveService.RunSlotCount; index++)
        {
            int displayIndex = index + 1;

            if (saveSlotRoots[index] == null)
            {
                saveSlotRoots[index] = FindChildComponent<Transform>($"SaveSlot_{displayIndex}", $"SaveSlot{displayIndex}");
            }

            Transform root = saveSlotRoots[index];
            if (root == null)
            {
                continue;
            }

            if (loadSlotButtons[index] == null)
            {
                Transform btn = root.Find("LoadButton");
                loadSlotButtons[index] = btn == null ? null : btn.GetComponent<Button>();
            }

            if (deleteSlotButtons[index] == null)
            {
                Transform btn = root.Find("DeleteButton");
                deleteSlotButtons[index] = btn == null ? null : btn.GetComponent<Button>();
            }
        }

        if (clearAllPersistentDataButton == null)
        {
            clearAllPersistentDataButton = FindChildComponent<Button>("ClearAllPersistentDataButton", "ClearAllSaveButton");
        }

        if (feedbackText == null)
        {
            feedbackText = FindChildComponent<TMP_Text>("FeedbackText", "SettingsFeedbackText");
        }
    }

    private T FindChildComponent<T>(params string[] names) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
        {
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (components[componentIndex].name == names[nameIndex])
                {
                    return components[componentIndex];
                }
            }
        }

        return null;
    }
}
