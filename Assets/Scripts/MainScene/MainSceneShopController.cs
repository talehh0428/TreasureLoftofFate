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
    [SerializeField] private ScreenFadeTransition transition;
    [SerializeField] private string marketToNpcMessage = "坊市结束";
    [SerializeField] private string nextRoundMessage = "新一回合";

    [Header("Dialogue")]
    [SerializeField] private NPCDialogueBackendConnector dialogueConnector;

    [Header("NPC Catalog")]
    [SerializeField] private List<NPCDefinition> npcCatalog = new List<NPCDefinition>();

    private ShopMainSceneController shopSceneController;
    private ShopController marketSceneController;
    private NPCDefinition selectedNpc;
    private bool isLoadingMarketScene;
    private bool isLoadingShopScene;
    private bool isLoadingTradeScene;
    private bool isDialogueRunning;

    private void Awake()
    {
        AutoBind();
    }

    private void OnEnable()
    {
        if (openShopButton != null)
        {
            openShopButton.onClick.AddListener(OpenMarketScene);
        }

        SubscribeDialogueConnector();
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

    public void SetNpcCatalog(IEnumerable<NPCDefinition> definitions)
    {
        npcCatalog = definitions == null
            ? new List<NPCDefinition>()
            : definitions.Where(npc => npc != null).Distinct().ToList();

        if (shopSceneController != null)
        {
            shopSceneController.SetNpcCatalog(npcCatalog);
            RefreshActionButtons();
        }
    }

    public void CompleteTradeForNpc(NPCDefinition npc)
    {
        if (npc == null || shopSceneController == null)
        {
            return;
        }

        shopSceneController.SetNpcTraded(npc);
        RefreshActionButtons();
    }

    public void CompleteTradeForSelectedNpc()
    {
        CompleteTradeForNpc(selectedNpc);
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
        shopSceneController.NpcSelected += HandleNpcSelected;
        shopSceneController.SetNpcCatalog(npcCatalog);

        BindShopButtons();
        selectedNpc = shopSceneController.SelectedNpc;
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
        shopSceneController.NpcSelected -= HandleNpcSelected;
        shopSceneController = null;
        selectedNpc = null;
    }

    private void HandleNpcSelected(NPCDefinition npc)
    {
        selectedNpc = npc;
        RefreshActionButtons();
    }

    private void HandleTalkClicked()
    {
        if (selectedNpc == null || shopSceneController == null || isDialogueRunning)
        {
            return;
        }

        if (shopSceneController.HasTalked(selectedNpc))
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
        dialogueConnector.StartBackendDialogue(selectedNpc);
    }

    private void HandleTradeClicked()
    {
        if (selectedNpc == null || shopSceneController == null || shopSceneController.HasTraded(selectedNpc))
        {
            return;
        }

        if (isLoadingTradeScene || string.IsNullOrWhiteSpace(tradeSceneName))
        {
            return;
        }

        TradeSceneContext.Set(selectedNpc, this);

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
            Debug.Log($"[MainSceneShopController] 已达到最大回合 {roundScheduler.MaxRound}，后续结算逻辑待补充。");
            return;
        }

        StartCoroutine(PlayTransitionRoutine(
            nextRoundMessage,
            ShopSceneToNextMarketRoutine()));
    }

    private IEnumerator MarketToShopSceneRoutine()
    {
        yield return UnloadMarketSceneRoutine();
        yield return OpenShopSceneRoutine();
    }

    private IEnumerator ShopSceneToNextMarketRoutine()
    {
        if (!roundScheduler.TryProcessNextRound())
        {
            yield break;
        }

        yield return UnloadShopSceneRoutine();
        yield return OpenMarketSceneRoutine();
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
            shopSceneController.SetNpcTalked(completedNpc);
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

        bool hasSelectedNpc = selectedNpc != null;
        bool canTalk = hasSelectedNpc && !shopSceneController.HasTalked(selectedNpc) && !isDialogueRunning;
        bool canTrade = hasSelectedNpc && !shopSceneController.HasTraded(selectedNpc);

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

    private void AutoBind()
    {
        if (dialogueConnector == null)
        {
            dialogueConnector = FindObjectOfType<NPCDialogueBackendConnector>(true);
        }

        if (roundScheduler == null)
        {
            roundScheduler = FindObjectOfType<NPCEventScheduler>(true);
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
