using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainSceneHudController : MonoBehaviour
{
    [Header("State")]
    [SerializeField] [Min(0)] private int startingMoney = 1000;
    [SerializeField] private NPCEventScheduler roundScheduler;
    [SerializeField] private EconomyBuffSystem economyBuffSystem;

    [Header("UI References")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text moneyText;

    [Header("Navigation")]
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private string startMenuSceneName = "StartMenu";

    private bool isSubscribedToScheduler;
    private int previewWalletDelta;
    private bool isLoadingOrLoadedStartMenu;
    private static bool hasInitializedNewGameState;
    private static bool hasShownStartMenu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetNewGameStateInitializationFlag()
    {
        hasInitializedNewGameState = false;
        hasShownStartMenu = false;
    }

    private void Awake()
    {
        AutoBind();
        GameSaveService.LoadArchiveIntoRuntime();
        if (GameStartContext.IsLoadingRunSave)
        {
            Debug.Log("[MainSceneHudController] 检测到读档流程，跳过新游戏状态初始化。");
        }
        else
        {
            InitializeNewGameStateIfNeeded();
        }

        RefreshMoneyText(ShopWallet.CurrentMoney);
        RefreshRoundText(GetCurrentRoundValue());

        // 首次启动时，在 MainScene 之上 Additive 加载 StartMenu
        if (!hasShownStartMenu)
        {
            hasShownStartMenu = true;
            SceneManager.LoadScene(startMenuSceneName, LoadSceneMode.Additive);
        }
    }

    private void OnEnable()
    {
        ShopWallet.MoneyChanged += HandleMoneyChanged;
        ShopEvents.WalletPreviewChanged += HandleWalletPreviewChanged;
        SubscribeScheduler();
        RefreshMoneyText(ShopWallet.CurrentMoney);
        RefreshRoundText(GetCurrentRoundValue());

        if (backToMenuButton == null)
        {
            TryFindBackToMenuButton();
        }

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(GoBackToMenu);
        }

        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        ShopWallet.MoneyChanged -= HandleMoneyChanged;
        ShopEvents.WalletPreviewChanged -= HandleWalletPreviewChanged;
        UnsubscribeScheduler();

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveListener(GoBackToMenu);
        }

        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnValidate()
    {
        AutoBind();
    }

    private void HandleMoneyChanged(int currentMoney)
    {
        RefreshMoneyText(currentMoney);
    }

    private void HandleWalletPreviewChanged(int previewDelta)
    {
        previewWalletDelta = previewDelta;
        RefreshMoneyText(ShopWallet.CurrentMoney);
    }

    private void HandleRoundChanged(int currentRound)
    {
        RefreshRoundText(currentRound);
    }

    private void InitializeNewGameStateIfNeeded()
    {
        if (hasInitializedNewGameState)
        {
            ShopWallet.InitializeIfNeeded(startingMoney);
            return;
        }

        GameSaveService.ResetRunStateForNewGame(startingMoney, roundScheduler, economyBuffSystem);
        hasInitializedNewGameState = true;
    }

    private void RefreshMoneyText(int currentMoney)
    {
        if (moneyText == null)
        {
            return;
        }

        if (previewWalletDelta == 0)
        {
            moneyText.text = $"{currentMoney}";
            return;
        }

        int previewMoney = Mathf.Max(0, currentMoney + previewWalletDelta);
        moneyText.text = $"{currentMoney}\uff08{previewMoney}\uff09";
    }

    private void RefreshRoundText(int currentRound)
    {
        if (roundText == null)
        {
            return;
        }

        roundText.text = $"第{Mathf.Max(1, currentRound)}回合";
    }

    private int GetCurrentRoundValue()
    {
        return roundScheduler == null ? 1 : roundScheduler.CurrentRound;
    }

    public void GoBackToMenu()
    {
        if (isLoadingOrLoadedStartMenu || string.IsNullOrWhiteSpace(startMenuSceneName))
        {
            return;
        }

        // 重置游戏状态，以便从菜单再次开始新游戏时能正确初始化
        hasInitializedNewGameState = false;
        GameSaveService.ResetRunStateForNewGame(startingMoney, roundScheduler, economyBuffSystem);
        FindObjectOfType<MainSceneShopController>()?.ResetCurrentRunState();

        isLoadingOrLoadedStartMenu = true;
        SceneManager.sceneLoaded += OnStartMenuLoaded;
        SceneManager.LoadScene(startMenuSceneName, LoadSceneMode.Additive);
    }

    private void OnStartMenuLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != startMenuSceneName)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnStartMenuLoaded;
        isLoadingOrLoadedStartMenu = false;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == startMenuSceneName)
        {
            isLoadingOrLoadedStartMenu = false;
        }
    }

    private void SubscribeScheduler()
    {
        if (roundScheduler == null || isSubscribedToScheduler)
        {
            return;
        }

        roundScheduler.RoundChanged += HandleRoundChanged;
        isSubscribedToScheduler = true;
    }

    private void UnsubscribeScheduler()
    {
        if (roundScheduler == null || !isSubscribedToScheduler)
        {
            return;
        }

        roundScheduler.RoundChanged -= HandleRoundChanged;
        isSubscribedToScheduler = false;
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

        if (backToMenuButton == null)
        {
            TryFindBackToMenuButton();
        }
    }

    private void TryFindBackToMenuButton()
    {
        // 优先搜索子级
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < allButtons.Length; i++)
        {
            if (allButtons[i].name == "BackToMenuButton" || allButtons[i].name == "BackToStartMenuButton")
            {
                backToMenuButton = allButtons[i];
                return;
            }
        }

        // 子级没找到，全局搜索整个场景（兼容按钮在 Canvas 其他分支下的情况）
        Button[] sceneButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneButtons.Length; i++)
        {
            if (sceneButtons[i].name == "BackToMenuButton" || sceneButtons[i].name == "BackToStartMenuButton")
            {
                backToMenuButton = sceneButtons[i];
                return;
            }
        }
    }
}
