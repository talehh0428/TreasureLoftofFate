using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainSceneShopController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string marketSceneName = "shichang";
    [SerializeField] private string shopSceneName = "ShopMainScene";
    [SerializeField] private string tradeSceneName = "TradeScene";
    [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Additive;
    [SerializeField] private Button openShopButton;

    [Header("Round Flow")]
    [SerializeField] private NPCEventScheduler roundScheduler;
    [SerializeField] private EconomyBuffSystem economyBuffSystem;
    [SerializeField] private SaveSlotPanelController saveSlotPanel;
    [SerializeField] private ScreenFadeTransition transition;
    [SerializeField] private string marketToNpcMessage = "坊市结束";
    [SerializeField] private string nextRoundMessage = "新一回合";

    [Header("Dialogue")]
    [SerializeField] private NPCDialogueBackendConnector dialogueConnector;
    [SerializeField] private DialogueJsonStoryPlayer storyPlayer;

    [Header("Start Flow")]
    [SerializeField] private string prologueJsonPath = "Assets/Text/xuzhang.json";
    [SerializeField] private bool showDialogueBackgroundForPrologue = true;
    [SerializeField] private bool continueWhenPrologueFails = true;

    [Header("Ending Flow")]
    [SerializeField] private string endingJsonPath = "Assets/Text/zhongzhang.json";
    [SerializeField] private bool showDialogueBackgroundForEnding = true;
    [SerializeField] private bool returnToStartMenuWhenEndingFails = true;

    [Header("Common Visitors")]
    [SerializeField] private Sprite commonVisitorAvatar;
    [SerializeField] [Min(0)] private int minCommonVisitorsPerRound = 2;
    [SerializeField] [Min(0)] private int maxCommonVisitorsPerRound = 5;
    [SerializeField] private List<string> commonVisitorNames = new List<string>
    {
        "青衣散修",
        "云游道人",
        "赶路剑客",
        "外门弟子",
        "采药修士"
    };

    private ShopMainSceneController shopSceneController;
    private ShopController marketSceneController;
    private ShopVisitor selectedVisitor;
    private readonly List<NPCDefinition> nextRoundSpecialVisitors = new List<NPCDefinition>();
    private readonly List<ShopVisitor> currentRoundVisitors = new List<ShopVisitor>();
    private const string InactiveEventId = NPCEventSpecialIds.Inactive;
    private bool isLoadingMarketScene;
    private bool isLoadingShopScene;
    private bool isLoadingTradeScene;
    private bool isDialogueRunning;
    private bool isStartFlowRunning;
    private bool isWaitingForPrologueStory;
    private bool isEndingRunning;
    private bool isWaitingForEndingStory;

    private void Awake()
    {
        AutoBind();
        ApplyPendingRunSaveIfNeeded();
    }

    private void OnEnable()
    {
        if (openShopButton != null)
        {
            openShopButton.onClick.AddListener(OpenMarketScene);
        }

        SubscribeDialogueConnector();
        SubscribeRoundScheduler();
    }

    private void OnDisable()
    {
        if (openShopButton != null)
        {
            openShopButton.onClick.RemoveListener(OpenMarketScene);
        }

        UnbindMarketSceneController();
        UnbindShopSceneController();
        UnsubscribeDialogueConnector();
        UnsubscribeRoundScheduler();
    }

    [ContextMenu("Open Market Scene")]
    public void OpenMarketScene()
    {
        if (isLoadingMarketScene || string.IsNullOrWhiteSpace(marketSceneName))
        {
            return;
        }

        Scene loadedScene = SceneManager.GetSceneByName(marketSceneName);
        if (loadedScene.isLoaded)
        {
            BindLoadedMarketScene();
            return;
        }

        StartCoroutine(OpenMarketSceneRoutine());
    }

    public void OpenShopScene()
    {
        if (isLoadingShopScene || string.IsNullOrWhiteSpace(shopSceneName))
        {
            return;
        }

        Scene loadedScene = SceneManager.GetSceneByName(shopSceneName);
        if (loadedScene.isLoaded)
        {
            BindLoadedShopScene();
            return;
        }

        StartCoroutine(OpenShopSceneRoutine());
    }

    public void CloseShopScene()
    {
        Scene loadedScene = SceneManager.GetSceneByName(shopSceneName);
        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            return;
        }

        UnbindShopSceneController();
        SceneManager.UnloadSceneAsync(loadedScene);
    }

    public void CloseMarketScene()
    {
        Scene loadedScene = SceneManager.GetSceneByName(marketSceneName);
        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            return;
        }

        UnbindMarketSceneController();
        SceneManager.UnloadSceneAsync(loadedScene);
    }

    public void CompleteTradeForNpc(NPCDefinition npc)
    {
        CompleteTradeForVisitor(FindCurrentVisitorByDefinition(npc));
    }

    public void CompleteTradeForVisitor(ShopVisitor visitor)
    {
        if (visitor != null && !currentRoundVisitors.Contains(visitor) && visitor.Definition != null)
        {
            visitor = FindCurrentVisitorByDefinition(visitor.Definition);
        }

        if (visitor == null || shopSceneController == null)
        {
            return;
        }

        shopSceneController.SetVisitorTraded(visitor);
        RefreshActionButtons();
    }

    public void CompleteTradeForSelectedNpc()
    {
        CompleteTradeForVisitor(selectedVisitor);
    }

    public IReadOnlyList<string> CapturePendingSpecialVisitorNpcIds()
    {
        return nextRoundSpecialVisitors
            .Where(npc => npc != null)
            .Select(npc => npc.NpcId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
    }

    public void RestoreFromRunSave(RunSaveData runData)
    {
        if (runData == null)
        {
            return;
        }

        GameSaveService.ApplyRun(runData, roundScheduler, economyBuffSystem);
        nextRoundSpecialVisitors.Clear();
        currentRoundVisitors.Clear();
        selectedVisitor = null;
        RestorePendingSpecialVisitors(runData.pendingSpecialVisitorNpcIds);
        CloseShopScene();
        OpenMarketScene();
    }

    public void ResetCurrentRunState()
    {
        nextRoundSpecialVisitors.Clear();
        currentRoundVisitors.Clear();
        selectedVisitor = null;
        CloseMarketScene();
        CloseShopScene();
    }

    public void StartNewGameFromMenu(StartMenuController startMenuController)
    {
        if (isStartFlowRunning)
        {
            return;
        }

        StartCoroutine(StartGameFromMenuRoutine(null, startMenuController));
    }

    public void LoadGameFromMenu(RunSaveData runSave, StartMenuController startMenuController)
    {
        if (isStartFlowRunning || runSave == null)
        {
            return;
        }

        StartCoroutine(StartGameFromMenuRoutine(runSave, startMenuController));
    }

    private void ApplyPendingRunSaveIfNeeded()
    {
        RunSaveData pendingRunSave = GameStartContext.ConsumePendingRunSave();
        if (pendingRunSave != null)
        {
            GameSaveService.ApplyRun(pendingRunSave, roundScheduler, economyBuffSystem);
            RestorePendingSpecialVisitors(pendingRunSave.pendingSpecialVisitorNpcIds);
        }
    }

    private IEnumerator StartGameFromMenuRoutine(RunSaveData runSave, StartMenuController startMenuController)
    {
        isStartFlowRunning = true;
        if (startMenuController != null)
        {
            startMenuController.SetStartFlowInProgress(true, true);
        }

        if (runSave != null)
        {
            GameStartContext.SetPendingRunSave(runSave);
        }

        yield return null;

        bool restoredFromRunSave = false;
        if (runSave != null)
        {
            yield return null;
            restoredFromRunSave = ApplyPendingRunSaveFromStartFlowIfNeeded();
            if (restoredFromRunSave || !GameStartContext.HasPendingRunSave)
            {
                GameStartContext.ClearPendingRunLoad();
            }
        }

        if (runSave == null)
        {
            yield return PlayPrologueRoutine(startMenuController);
            yield return OpenMarketSceneWhenNeededRoutine();
            UnloadStoryDialogue();
        }
        else
        {
            yield return OpenMarketSceneWhenNeededRoutine();
        }

        if (startMenuController != null)
        {
            Scene startMenuScene = startMenuController.gameObject.scene;
            if (startMenuScene.IsValid() && startMenuScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(startMenuScene);
            }
        }

        isStartFlowRunning = false;
    }

    private bool ApplyPendingRunSaveFromStartFlowIfNeeded()
    {
        if (!GameStartContext.HasPendingRunSave)
        {
            return false;
        }

        RunSaveData pendingRunSave = GameStartContext.ConsumePendingRunSave();
        if (pendingRunSave == null)
        {
            return false;
        }

        Debug.Log($"[MainSceneShopController] 主场景初始化后补应用流程档 round={pendingRunSave.currentRound} money={pendingRunSave.money}");
        RestoreFromRunSave(pendingRunSave);
        return true;
    }

    private IEnumerator PlayPrologueRoutine(StartMenuController startMenuController)
    {
        if (string.IsNullOrWhiteSpace(prologueJsonPath))
        {
            yield break;
        }

        if (storyPlayer == null)
        {
            storyPlayer = FindObjectOfType<DialogueJsonStoryPlayer>(true);
        }

        if (storyPlayer == null)
        {
            Debug.LogError("[MainSceneShopController] DialogueJsonStoryPlayer not found in MainScene.");
            yield break;
        }

        isWaitingForPrologueStory = true;
        bool hasHiddenStartMenu = false;
        bool unloadDialogueWhenFinished = storyPlayer.UnloadDialogueWhenFinished;
        storyPlayer.UnloadDialogueWhenFinished = false;
        void HideStartMenuWhenDialogueShown()
        {
            if (hasHiddenStartMenu)
            {
                return;
            }

            hasHiddenStartMenu = true;
            if (startMenuController != null)
            {
                startMenuController.SetStartFlowInProgress(true);
            }
        }

        storyPlayer.DialogueShown += HideStartMenuWhenDialogueShown;
        storyPlayer.StoryCompleted += HandlePrologueStoryCompleted;
        storyPlayer.StoryFailed += HandlePrologueStoryFailed;
        storyPlayer.StartDialogueFromJsonPath(prologueJsonPath, showDialogueBackgroundForPrologue);

        while (isWaitingForPrologueStory)
        {
            yield return null;
        }

        storyPlayer.DialogueShown -= HideStartMenuWhenDialogueShown;
        storyPlayer.StoryCompleted -= HandlePrologueStoryCompleted;
        storyPlayer.StoryFailed -= HandlePrologueStoryFailed;
        storyPlayer.UnloadDialogueWhenFinished = unloadDialogueWhenFinished;
    }

    private void HandlePrologueStoryCompleted()
    {
        isWaitingForPrologueStory = false;
    }

    private void HandlePrologueStoryFailed(string message)
    {
        Debug.LogWarning($"[MainSceneShopController] Prologue dialogue failed: {message}");
        if (continueWhenPrologueFails)
        {
            isWaitingForPrologueStory = false;
        }
    }

    private IEnumerator OpenShopSceneRoutine()
    {
        isLoadingShopScene = true;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(shopSceneName, loadSceneMode);
        if (loadOperation == null)
        {
            isLoadingShopScene = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        isLoadingShopScene = false;
        BindLoadedShopScene();
    }

    private IEnumerator OpenMarketSceneRoutine()
    {
        isLoadingMarketScene = true;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(marketSceneName, loadSceneMode);
        if (loadOperation == null)
        {
            isLoadingMarketScene = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        isLoadingMarketScene = false;
        BindLoadedMarketScene();
    }

    private IEnumerator OpenMarketSceneWhenNeededRoutine()
    {
        if (string.IsNullOrWhiteSpace(marketSceneName))
        {
            yield break;
        }

        Scene loadedScene = SceneManager.GetSceneByName(marketSceneName);
        if (loadedScene.isLoaded)
        {
            BindLoadedMarketScene();
            yield break;
        }

        if (isLoadingMarketScene)
        {
            while (isLoadingMarketScene)
            {
                yield return null;
            }

            BindLoadedMarketScene();
            yield break;
        }

        yield return OpenMarketSceneRoutine();
    }

    private void UnloadStoryDialogue()
    {
        if (storyPlayer != null)
        {
            storyPlayer.UnloadDialogue();
        }
    }

    private void BindLoadedMarketScene()
    {
        ShopController controller = FindMarketSceneController();
        if (controller == null)
        {
            Debug.LogError($"[MainSceneShopController] ShopController not found in scene: {marketSceneName}");
            return;
        }

        if (marketSceneController == controller)
        {
            return;
        }

        UnbindMarketSceneController();
        marketSceneController = controller;
        marketSceneController.LeaveRequested += HandleMarketLeaveRequested;
    }

    private void BindLoadedShopScene()
    {
        ShopMainSceneController controller = FindShopSceneController();
        if (controller == null)
        {
            Debug.LogError($"[MainSceneShopController] ShopMainSceneController not found in scene: {shopSceneName}");
            return;
        }

        if (shopSceneController == controller)
        {
            RefreshActionButtons();
            return;
        }

        UnbindShopSceneController();

        shopSceneController = controller;
        shopSceneController.VisitorSelected += HandleVisitorSelected;
        RebuildCurrentRoundVisitors();
        shopSceneController.SetVisitors(currentRoundVisitors);

        BindShopButtons();
        selectedVisitor = shopSceneController.SelectedVisitor;
        RefreshActionButtons();
    }

    private ShopController FindMarketSceneController()
    {
        Scene loadedScene = SceneManager.GetSceneByName(marketSceneName);
        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = loadedScene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            ShopController controller = roots[rootIndex].GetComponentInChildren<ShopController>(true);
            if (controller != null)
            {
                return controller;
            }
        }

        return null;
    }

    private ShopMainSceneController FindShopSceneController()
    {
        Scene loadedScene = SceneManager.GetSceneByName(shopSceneName);
        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = loadedScene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            ShopMainSceneController controller = roots[rootIndex].GetComponentInChildren<ShopMainSceneController>(true);
            if (controller != null)
            {
                return controller;
            }
        }

        return null;
    }

    private void BindShopButtons()
    {
        if (shopSceneController == null)
        {
            return;
        }

        Button talkButton = shopSceneController.TalkButton;
        if (talkButton != null)
        {
            talkButton.onClick.RemoveListener(HandleTalkClicked);
            talkButton.onClick.AddListener(HandleTalkClicked);
        }

        Button tradeButton = shopSceneController.TradeButton;
        if (tradeButton != null)
        {
            tradeButton.onClick.RemoveListener(HandleTradeClicked);
            tradeButton.onClick.AddListener(HandleTradeClicked);
        }

        Button leaveButton = shopSceneController.LeaveButton;
        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(HandleShopSceneLeaveClicked);
            leaveButton.onClick.AddListener(HandleShopSceneLeaveClicked);
        }
    }

    private void UnbindShopButtons()
    {
        if (shopSceneController == null)
        {
            return;
        }

        Button talkButton = shopSceneController.TalkButton;
        if (talkButton != null)
        {
            talkButton.onClick.RemoveListener(HandleTalkClicked);
        }

        Button tradeButton = shopSceneController.TradeButton;
        if (tradeButton != null)
        {
            tradeButton.onClick.RemoveListener(HandleTradeClicked);
        }

        Button leaveButton = shopSceneController.LeaveButton;
        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(HandleShopSceneLeaveClicked);
        }
    }

    private void UnbindMarketSceneController()
    {
        if (marketSceneController == null)
        {
            return;
        }

        marketSceneController.LeaveRequested -= HandleMarketLeaveRequested;
        marketSceneController = null;
    }

    private void UnbindShopSceneController()
    {
        if (shopSceneController == null)
        {
            return;
        }

        UnbindShopButtons();
        shopSceneController.VisitorSelected -= HandleVisitorSelected;
        shopSceneController = null;
        selectedVisitor = null;
    }

    private void HandleVisitorSelected(ShopVisitor visitor)
    {
        selectedVisitor = visitor;
        RefreshActionButtons();
    }

    private void HandleTalkClicked()
    {
        if (selectedVisitor == null || shopSceneController == null || isDialogueRunning || !selectedVisitor.CanTalk)
        {
            return;
        }

        if (shopSceneController.HasTalked(selectedVisitor))
        {
            RefreshActionButtons();
            return;
        }

        if (dialogueConnector == null)
        {
            Debug.LogError("[MainSceneShopController] NPCDialogueBackendConnector is not assigned.");
            return;
        }

        isDialogueRunning = true;
        RefreshActionButtons();
        dialogueConnector.StartBackendDialogue(selectedVisitor.Definition);
    }

    private void HandleTradeClicked()
    {
        if (selectedVisitor == null || shopSceneController == null || !selectedVisitor.CanTrade || shopSceneController.HasTraded(selectedVisitor))
        {
            return;
        }

        if (isLoadingTradeScene || string.IsNullOrWhiteSpace(tradeSceneName))
        {
            return;
        }

        TradeSceneContext.Set(selectedVisitor, this);

        Scene loadedScene = SceneManager.GetSceneByName(tradeSceneName);
        if (loadedScene.isLoaded)
        {
            return;
        }

        StartCoroutine(OpenTradeSceneRoutine());
    }

    private void HandleMarketLeaveRequested()
    {
        StartCoroutine(PlayTransitionRoutine(
            marketToNpcMessage,
            MarketToShopSceneRoutine()));
    }

    private void HandleShopSceneLeaveClicked()
    {
        if (roundScheduler == null)
        {
            Debug.LogWarning("[MainSceneShopController] NPCEventScheduler is not assigned.");
            return;
        }

        if (!roundScheduler.CanAdvanceRound)
        {
            if (!isEndingRunning)
            {
                StartCoroutine(PlayEndingAndReturnToMenuRoutine());
            }

            return;
        }

        StartCoroutine(ShopSceneToNextMarketRoutine());
    }

    private IEnumerator PlayEndingAndReturnToMenuRoutine()
    {
        isEndingRunning = true;
        yield return UnloadShopSceneRoutine();

        if (string.IsNullOrWhiteSpace(endingJsonPath))
        {
            Debug.LogWarning("[MainSceneShopController] Ending JSON path is empty. Returning to start menu.");
            ReturnToStartMenu();
            isEndingRunning = false;
            yield break;
        }

        if (storyPlayer == null)
        {
            storyPlayer = FindObjectOfType<DialogueJsonStoryPlayer>(true);
        }

        if (storyPlayer == null)
        {
            Debug.LogError("[MainSceneShopController] DialogueJsonStoryPlayer is not assigned.");
            ReturnToStartMenu();
            isEndingRunning = false;
            yield break;
        }

        isWaitingForEndingStory = true;
        storyPlayer.StoryCompleted += HandleEndingStoryCompleted;
        storyPlayer.StoryFailed += HandleEndingStoryFailed;
        storyPlayer.StartDialogueFromJsonPath(endingJsonPath, showDialogueBackgroundForEnding);

        while (isWaitingForEndingStory)
        {
            yield return null;
        }

        storyPlayer.StoryCompleted -= HandleEndingStoryCompleted;
        storyPlayer.StoryFailed -= HandleEndingStoryFailed;
        ReturnToStartMenu();
        isEndingRunning = false;
    }

    private void HandleEndingStoryCompleted()
    {
        isWaitingForEndingStory = false;
    }

    private void HandleEndingStoryFailed(string message)
    {
        Debug.LogWarning($"[MainSceneShopController] Ending dialogue failed: {message}");
        if (returnToStartMenuWhenEndingFails)
        {
            isWaitingForEndingStory = false;
        }
    }

    private void ReturnToStartMenu()
    {
        MainSceneHudController hudController = FindObjectOfType<MainSceneHudController>(true);
        if (hudController == null)
        {
            Debug.LogError("[MainSceneShopController] MainSceneHudController not found. Cannot return to StartMenu.");
            return;
        }

        hudController.GoBackToMenu();
    }

    private void HandleNpcEventUpdated(NPCDefinition npc)
    {
        if (npc == null || nextRoundSpecialVisitors.Contains(npc))
        {
            return;
        }

        nextRoundSpecialVisitors.Add(npc);
    }

    private IEnumerator MarketToShopSceneRoutine()
    {
        yield return UnloadMarketSceneRoutine();
        yield return OpenShopSceneRoutine();
    }

    private IEnumerator ShopSceneToNextMarketRoutine()
    {
        bool advancedRound = false;
        yield return PlayTransitionRoutine(nextRoundMessage, ProcessEndRoundAndAdvanceRoutine());

        if (!advancedRound)
        {
            yield break;
        }

        IEnumerator ProcessEndRoundAndAdvanceRoutine()
        {
            if (economyBuffSystem != null)
            {
                int endingRound = roundScheduler == null ? 1 : roundScheduler.CurrentRound;
                yield return economyBuffSystem.ProcessEndRoundRoutine(endingRound);
            }

            if (roundScheduler != null)
            {
                advancedRound = roundScheduler.TryProcessNextRound();
            }

            if (advancedRound)
            {
                yield return ShowEndRoundSaveChoiceRoutine();
                yield return UnloadShopSceneRoutine();
                yield return OpenMarketSceneRoutine();
            }
        }
    }

    private IEnumerator ShowEndRoundSaveChoiceRoutine()
    {
        if (saveSlotPanel == null)
        {
            saveSlotPanel = FindObjectOfType<SaveSlotPanelController>(true);
        }

        if (saveSlotPanel == null)
        {
            Debug.LogWarning("[MainSceneShopController] SaveSlotPanelController is not assigned. End round save choice skipped.");
            yield break;
        }

        bool panelClosed = false;
        bool transitionBlockedRaycasts = transition != null && transition.BlocksRaycasts;
        if (transition != null)
        {
            transition.BlocksRaycasts = false;
        }

        yield return saveSlotPanel.ShowSaveChoiceRoutine(() => panelClosed = true);

        while (!panelClosed)
        {
            yield return null;
        }

        if (transition != null)
        {
            transition.BlocksRaycasts = transitionBlockedRaycasts;
        }
    }

    private IEnumerator UnloadMarketSceneRoutine()
    {
        Scene loadedScene = SceneManager.GetSceneByName(marketSceneName);
        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            yield break;
        }

        UnbindMarketSceneController();
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
        if (unloadOperation == null)
        {
            yield break;
        }

        while (!unloadOperation.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator UnloadShopSceneRoutine()
    {
        Scene loadedScene = SceneManager.GetSceneByName(shopSceneName);
        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            yield break;
        }

        UnbindShopSceneController();
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
        if (unloadOperation == null)
        {
            yield break;
        }

        while (!unloadOperation.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator PlayTransitionRoutine(string message, IEnumerator coveredOperation)
    {
        if (transition == null)
        {
            yield return coveredOperation;
            yield break;
        }

        yield return transition.Play(message, coveredOperation);
    }

    private void RebuildCurrentRoundVisitors()
    {
        currentRoundVisitors.Clear();

        List<NPCDefinition> specialVisitors = nextRoundSpecialVisitors
            .Where(npc => npc != null)
            .Where(npc => npc.CurrentEventID != InactiveEventId)
            .Distinct()
            .ToList();

        for (int index = 0; index < specialVisitors.Count; index++)
        {
            currentRoundVisitors.Add(ShopVisitor.FromDefinition(specialVisitors[index]));
        }

        nextRoundSpecialVisitors.Clear();
        currentRoundVisitors.AddRange(CreateCommonVisitors());
    }

    private List<ShopVisitor> CreateCommonVisitors()
    {
        List<ShopVisitor> visitors = new List<ShopVisitor>();
        int minCount = Mathf.Min(minCommonVisitorsPerRound, maxCommonVisitorsPerRound);
        int maxCount = Mathf.Max(minCommonVisitorsPerRound, maxCommonVisitorsPerRound);
        int count = Random.Range(minCount, maxCount + 1);

        for (int index = 0; index < count; index++)
        {
            string visitorName = PickCommonVisitorName();
            string visitorId = $"CommonVisitor_{roundScheduler?.CurrentRound ?? 1}_{index + 1}_{Random.Range(1000, 9999)}";
            visitors.Add(ShopVisitor.CreateCommon(
                visitorId,
                visitorName,
                commonVisitorAvatar));
        }

        return visitors;
    }

    private string PickCommonVisitorName()
    {
        List<string> validNames = commonVisitorNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        if (validNames.Count == 0)
        {
            return "来访修士";
        }

        return validNames[Random.Range(0, validNames.Count)];
    }

    private ShopVisitor FindCurrentVisitorByDefinition(NPCDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        return currentRoundVisitors.FirstOrDefault(visitor => visitor != null && visitor.Definition == definition);
    }

    private void RestorePendingSpecialVisitors(IEnumerable<string> npcIds)
    {
        nextRoundSpecialVisitors.Clear();
        if (npcIds == null || roundScheduler == null)
        {
            return;
        }

        IReadOnlyList<NPCDefinition> npcs = roundScheduler.Npcs;
        foreach (string npcId in npcIds)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                continue;
            }

            NPCDefinition npc = npcs.FirstOrDefault(candidate => candidate != null && candidate.NpcId == npcId);
            if (npc != null && !nextRoundSpecialVisitors.Contains(npc))
            {
                nextRoundSpecialVisitors.Add(npc);
            }
        }
    }

    private IEnumerator OpenTradeSceneRoutine()
    {
        isLoadingTradeScene = true;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(tradeSceneName, loadSceneMode);
        if (loadOperation == null)
        {
            isLoadingTradeScene = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        isLoadingTradeScene = false;
    }

    private void HandleDialogueCompleted(NPCDefinition completedNpc)
    {
        isDialogueRunning = false;

        if (completedNpc != null && shopSceneController != null)
        {
            shopSceneController.SetVisitorTalked(FindCurrentVisitorByDefinition(completedNpc));
        }

        RefreshActionButtons();
    }

    private void HandleDialogueFailed(NPCDefinition failedNpc)
    {
        isDialogueRunning = false;
        RefreshActionButtons();
    }

    private void RefreshActionButtons()
    {
        if (shopSceneController == null)
        {
            return;
        }

        bool hasSelectedVisitor = selectedVisitor != null;
        bool canTalk = hasSelectedVisitor && selectedVisitor.CanTalk && !shopSceneController.HasTalked(selectedVisitor) && !isDialogueRunning;
        bool canTrade = hasSelectedVisitor && selectedVisitor.CanTrade && !shopSceneController.HasTraded(selectedVisitor);

        shopSceneController.SetTalkAvailable(canTalk);
        shopSceneController.SetTradeAvailable(canTrade);
    }

    private void SubscribeDialogueConnector()
    {
        if (dialogueConnector == null)
        {
            return;
        }

        dialogueConnector.DialogueCompleted -= HandleDialogueCompleted;
        dialogueConnector.DialogueCompleted += HandleDialogueCompleted;
        dialogueConnector.DialogueFailed -= HandleDialogueFailed;
        dialogueConnector.DialogueFailed += HandleDialogueFailed;
    }

    private void UnsubscribeDialogueConnector()
    {
        if (dialogueConnector == null)
        {
            return;
        }

        dialogueConnector.DialogueCompleted -= HandleDialogueCompleted;
        dialogueConnector.DialogueFailed -= HandleDialogueFailed;
    }

    private void SubscribeRoundScheduler()
    {
        if (roundScheduler == null)
        {
            return;
        }

        roundScheduler.NpcEventUpdated -= HandleNpcEventUpdated;
        roundScheduler.NpcEventUpdated += HandleNpcEventUpdated;
    }

    private void UnsubscribeRoundScheduler()
    {
        if (roundScheduler == null)
        {
            return;
        }

        roundScheduler.NpcEventUpdated -= HandleNpcEventUpdated;
    }

    private void AutoBind()
    {
        if (dialogueConnector == null)
        {
            dialogueConnector = FindObjectOfType<NPCDialogueBackendConnector>(true);
        }

        if (storyPlayer == null)
        {
            storyPlayer = FindObjectOfType<DialogueJsonStoryPlayer>(true);
        }

        if (roundScheduler == null)
        {
            roundScheduler = FindObjectOfType<NPCEventScheduler>(true);
        }

        if (economyBuffSystem == null)
        {
            economyBuffSystem = FindObjectOfType<EconomyBuffSystem>(true);
        }

        if (saveSlotPanel == null)
        {
            saveSlotPanel = FindObjectOfType<SaveSlotPanelController>(true);
        }

        if (transition == null)
        {
            transition = FindObjectOfType<ScreenFadeTransition>(true);
        }

        if (openShopButton == null)
        {
            openShopButton = GetComponent<Button>();
        }
    }
}
