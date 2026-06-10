using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Start Flow")]
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private string prologueJsonPath = "Assets/Text/xuzhang.json";
    [SerializeField] private bool continueWhenPrologueFails = true;

    private bool isStarting;
    private bool isWaitingForPrologue;
    private DialogueJsonStoryPlayer activeStoryPlayer;

    private void Awake()
    {
        AutoBind();
        SetPanelActive(endingsPanel, false);
        SetPanelActive(settingsPanel, false);
    }

    private void OnEnable()
    {
        AddListeners();
    }

    private void OnDisable()
    {
        RemoveListeners();
        UnsubscribeStoryPlayer();
    }

    public void StartGame()
    {
        if (isStarting)
        {
            return;
        }

        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        isStarting = true;
        SetMenuInteractable(false);

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Single);
        if (loadOperation == null)
        {
            Debug.LogError($"[StartMenuController] Failed to load scene: {mainSceneName}");
            Destroy(gameObject);
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return null;

        yield return PlayPrologueRoutine();
        OpenMarketScene();

        Destroy(gameObject);
    }

    private IEnumerator PlayPrologueRoutine()
    {
        if (string.IsNullOrWhiteSpace(prologueJsonPath))
        {
            yield break;
        }

        activeStoryPlayer = FindObjectOfType<DialogueJsonStoryPlayer>(true);
        if (activeStoryPlayer == null)
        {
            Debug.LogError("[StartMenuController] DialogueJsonStoryPlayer not found in MainScene.");
            yield break;
        }

        isWaitingForPrologue = true;
        activeStoryPlayer.StoryCompleted += HandleStoryCompleted;
        activeStoryPlayer.StoryFailed += HandleStoryFailed;
        activeStoryPlayer.StartDialogueFromJsonPath(prologueJsonPath);

        while (isWaitingForPrologue)
        {
            yield return null;
        }

        UnsubscribeStoryPlayer();
    }

    private void OpenMarketScene()
    {
        MainSceneShopController shopController = FindObjectOfType<MainSceneShopController>(true);
        if (shopController == null)
        {
            Debug.LogError("[StartMenuController] MainSceneShopController not found in MainScene.");
            return;
        }

        shopController.OpenMarketScene();
    }

    private void HandleStoryCompleted()
    {
        isWaitingForPrologue = false;
    }

    private void HandleStoryFailed(string message)
    {
        Debug.LogWarning($"[StartMenuController] Prologue dialogue failed: {message}");
        if (continueWhenPrologueFails)
        {
            isWaitingForPrologue = false;
        }
    }

    private void ToggleEndingsPanel()
    {
        SetPanelActive(endingsPanel, endingsPanel == null || !endingsPanel.activeSelf);
        SetPanelActive(settingsPanel, false);
    }

    private void ToggleSettingsPanel()
    {
        SetPanelActive(settingsPanel, settingsPanel == null || !settingsPanel.activeSelf);
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

    private void UnsubscribeStoryPlayer()
    {
        if (activeStoryPlayer == null)
        {
            return;
        }

        activeStoryPlayer.StoryCompleted -= HandleStoryCompleted;
        activeStoryPlayer.StoryFailed -= HandleStoryFailed;
        activeStoryPlayer = null;
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
}
