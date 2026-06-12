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
    [SerializeField] private Button[] loadSlotButtons = new Button[GameSaveService.RunSlotCount];
    [SerializeField] private Button[] deleteSlotButtons = new Button[GameSaveService.RunSlotCount];
    [SerializeField] private TMP_Text[] slotLabels = new TMP_Text[GameSaveService.RunSlotCount];

    [Header("Danger Zone")]
    [SerializeField] private Button clearAllPersistentDataButton;
    [SerializeField] private TMP_Text feedbackText;

    private void Awake()
    {
        AutoBind();
        ApplySavedVolume();
        RefreshSaveSlots();
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

            if (loadSlotButtons[index] != null)
            {
                loadSlotButtons[index].interactable = hasSlot;
            }

            if (deleteSlotButtons[index] != null)
            {
                deleteSlotButtons[index].interactable = hasSlot;
            }

            if (slotLabels[index] != null)
            {
                slotLabels[index].text = BuildSlotLabel(index, hasSlot ? slot : null);
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

    private static string BuildSlotLabel(int slotIndex, RunSaveSlotData slot)
    {
        if (slot == null || !slot.hasData || slot.run == null)
        {
            return $"存档 {slotIndex + 1}\n空";
        }

        return $"存档 {slotIndex + 1}\n第{Mathf.Max(1, slot.run.currentRound)}回合  {slot.run.money}灵石";
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
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
            if (loadSlotButtons[index] == null)
            {
                loadSlotButtons[index] = FindChildComponent<Button>($"LoadSlotButton_{displayIndex}", $"LoadSlot{displayIndex}Button");
            }

            if (deleteSlotButtons[index] == null)
            {
                deleteSlotButtons[index] = FindChildComponent<Button>($"DeleteSlotButton_{displayIndex}", $"DeleteSlot{displayIndex}Button");
            }

            if (slotLabels[index] == null)
            {
                slotLabels[index] = FindChildComponent<TMP_Text>($"SlotLabel_{displayIndex}", $"Slot{displayIndex}Label");
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
