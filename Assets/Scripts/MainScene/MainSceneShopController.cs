using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainSceneShopController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string shopSceneName = "ShopMainScene";
    [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Additive;
    [SerializeField] private Button openShopButton;

    [Header("Dialogue")]
    [SerializeField] private NPCDialogueBackendConnector dialogueConnector;

    [Header("NPC Catalog")]
    [SerializeField] private List<NPCDefinition> npcCatalog = new List<NPCDefinition>();

    private ShopMainSceneController shopSceneController;
    private NPCDefinition selectedNpc;
    private bool isLoadingShopScene;
    private bool isDialogueRunning;

    private void Awake()
    {
        AutoBind();
    }

    private void OnEnable()
    {
        if (openShopButton != null)
        {
            openShopButton.onClick.AddListener(OpenShopScene);
        }

        SubscribeDialogueConnector();
    }

    private void OnDisable()
    {
        if (openShopButton != null)
        {
            openShopButton.onClick.RemoveListener(OpenShopScene);
        }

        UnbindShopSceneController();
        UnsubscribeDialogueConnector();
    }

    [ContextMenu("Open Shop Scene")]
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
            leaveButton.onClick.RemoveListener(CloseShopScene);
            leaveButton.onClick.AddListener(CloseShopScene);
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
            leaveButton.onClick.RemoveListener(CloseShopScene);
        }
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

        Debug.Log("[MainSceneShopController] Trade flow is not implemented yet. Call CompleteTradeForNpc after the future trade flow finishes.");
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

        if (openShopButton == null)
        {
            openShopButton = GetComponent<Button>();
        }
    }
}
