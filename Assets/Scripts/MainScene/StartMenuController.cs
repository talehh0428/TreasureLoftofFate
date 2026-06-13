using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button endingsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeEndingsButton;
    [SerializeField] private Button closeSettingsButton;

    [Header("Panels")]
    [SerializeField] private GameObject endingsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private EndingsPanelController endingsPanelController;
    [SerializeField] private SettingsPanelController settingsPanelController;

    private bool isStarting;

    private void Awake()
    {
        AutoBind();
        GameSaveService.LoadArchiveIntoRuntime();
        SetPanelActive(rootPanel, true);
        SetPanelActive(endingsPanel, false);
        SetPanelActive(settingsPanel, false);

        // 播放主场景背景音乐
        BgmManager.Instance.PlayMainSceneBGM();
    }

    private void OnEnable()
    {
        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
    }

    public void StartGame()
    {
        if (isStarting)
        {
            return;
        }

        MainSceneShopController shopController = FindShopController();
        if (shopController == null)
        {
            return;
        }

        shopController.StartNewGameFromMenu(this);
    }

    public void LoadGame(int slotIndex)
    {
        GameSaveService.LogRunSlotState(slotIndex, "读取流程档");
        if (isStarting || !GameSaveService.TryGetRunSlot(slotIndex, out RunSaveSlotData slot))
        {
            return;
        }

        MainSceneShopController shopController = FindShopController();
        if (shopController == null)
        {
            return;
        }

        shopController.LoadGameFromMenu(slot.run, this);
    }

    public void DeleteGame(int slotIndex)
    {
        if (isStarting || !GameSaveService.DeleteRunSlot(slotIndex))
        {
            return;
        }

        RefreshSaveSlotButtons();
    }

    public void RefreshSaveSlotButtons()
    {
        if (settingsPanelController != null)
        {
            settingsPanelController.RefreshSaveSlots();
        }
    }

    public void SetStartFlowInProgress(bool inProgress)
    {
        isStarting = inProgress;
        SetMenuInteractable(!inProgress);
        SetPanelActive(rootPanel, !inProgress);
    }

    private void ToggleEndingsPanel()
    {
        SetPanelActive(endingsPanel, endingsPanel == null || !endingsPanel.activeSelf);
        if (endingsPanelController != null && endingsPanel != null && endingsPanel.activeSelf)
        {
            endingsPanelController.Rebuild();
        }

        SetPanelActive(settingsPanel, false);
    }

    private void ToggleSettingsPanel()
    {
        SetPanelActive(settingsPanel, settingsPanel == null || !settingsPanel.activeSelf);
        if (settingsPanelController != null && settingsPanel != null && settingsPanel.activeSelf)
        {
            settingsPanelController.RefreshSaveSlots();
        }

        SetPanelActive(endingsPanel, false);
    }

    private void CloseEndingsPanel()
    {
        SetPanelActive(endingsPanel, false);
    }

    private void CloseSettingsPanel()
    {
        SetPanelActive(settingsPanel, false);
    }

    private void AddListeners()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }

        if (endingsButton != null)
        {
            endingsButton.onClick.AddListener(ToggleEndingsPanel);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ToggleSettingsPanel);
        }

        if (closeEndingsButton != null)
        {
            closeEndingsButton.onClick.AddListener(CloseEndingsPanel);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(CloseSettingsPanel);
        }
    }

    private void RemoveListeners()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartGame);
        }

        if (endingsButton != null)
        {
            endingsButton.onClick.RemoveListener(ToggleEndingsPanel);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(ToggleSettingsPanel);
        }

        if (closeEndingsButton != null)
        {
            closeEndingsButton.onClick.RemoveListener(CloseEndingsPanel);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveListener(CloseSettingsPanel);
        }
    }

    private void SetMenuInteractable(bool interactable)
    {
        if (startGameButton != null)
        {
            startGameButton.interactable = interactable;
        }

        if (endingsButton != null)
        {
            endingsButton.interactable = interactable;
        }

        if (settingsButton != null)
        {
            settingsButton.interactable = interactable;
        }
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private void AutoBind()
    {
        if (startGameButton == null)
        {
            startGameButton = FindButton("StartGameButton");
        }

        if (endingsButton == null)
        {
            endingsButton = FindButton("EndingButton", "EndingsButton");
        }

        if (settingsButton == null)
        {
            settingsButton = FindButton("SettingsButton");
        }

        if (closeEndingsButton == null)
        {
            closeEndingsButton = FindButton("CloseEndingsButton");
        }

        if (closeSettingsButton == null)
        {
            closeSettingsButton = FindButton("CloseSettingsButton");
        }
    }

    private Button FindButton(params string[] names)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (buttons[i].name == names[nameIndex])
                {
                    return buttons[i];
                }
            }
        }

        return null;
    }

    private MainSceneShopController FindShopController()
    {
        MainSceneShopController shopController = FindObjectOfType<MainSceneShopController>(true);
        if (shopController == null)
        {
            Debug.LogError("[StartMenuController] MainSceneShopController not found in MainScene.");
        }

        return shopController;
    }
}
