using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DialogueSceneController : MonoBehaviour
{
    public static DialogueSceneController Instance { get; private set; }
    public event Action DialogueShown;

    [Header("Scene Loading")]
    [SerializeField] private string dialogueSceneName = string.Empty;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Manual UI")]
    [SerializeField] private DialogueBoxController dialogueBox;
    [SerializeField] private GameObject background;
    [SerializeField] private string backgroundObjectName = "Background";

    private Action<DialogueChoiceResult> currentChoiceCallback;
    private DialogueBody pendingBody;
    private bool isLoaded;
    private bool isLoadingScene;
    private bool shouldShowBackground;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad && gameObject.scene == SceneManager.GetActiveScene())
        {
            DontDestroyOnLoad(transform.root.gameObject);
        }

        if (dialogueBox != null)
        {
            dialogueBox.Hide();
        }

        ApplyBackgroundVisibility();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void LoadDialogue()
    {
        if (!string.IsNullOrWhiteSpace(dialogueSceneName) && !IsSceneLoaded(dialogueSceneName))
        {
            StartCoroutine(LoadDialogueSceneRoutine());
            return;
        }

        EnsureDialogueBox(true);
        EnsureBackground();
        isLoaded = true;

        if (dialogueBox != null)
        {
            dialogueBox.gameObject.SetActive(true);
        }

        ApplyBackgroundVisibility();
    }

    public void UnloadDialogue()
    {
        currentChoiceCallback = null;
        pendingBody = null;

        if (dialogueBox != null)
        {
            dialogueBox.Hide();
        }

        SetBackgroundVisible(false);

        if (!string.IsNullOrWhiteSpace(dialogueSceneName) && IsSceneLoaded(dialogueSceneName) && CanUnloadDialogueScene())
        {
            SceneManager.UnloadSceneAsync(dialogueSceneName);
        }

        isLoaded = false;
    }

    public void ShowDialogue(DialogueBody body, Action<DialogueChoiceResult> onChoiceSelected)
    {
        currentChoiceCallback = onChoiceSelected;
        pendingBody = body;

        if (!isLoaded || dialogueBox == null)
        {
            LoadDialogue();
            if (isLoaded && dialogueBox != null)
            {
                TryShowPendingDialogue();
            }
            return;
        }

        TryShowPendingDialogue();
    }

    public void ShowLoading(string npcName, Sprite portrait)
    {
        if (!isLoaded || dialogueBox == null)
        {
            LoadDialogue();
            StartCoroutine(ShowLoadingWhenReady(npcName, portrait));
            return;
        }

        dialogueBox.ShowLoading(npcName, portrait);
    }

    public void SetBackgroundVisible(bool visible)
    {
        shouldShowBackground = visible;
        if (!isLoadingScene)
        {
            EnsureBackground();
            ApplyBackgroundVisibility();
        }
    }

    private IEnumerator LoadDialogueSceneRoutine()
    {
        if (isLoadingScene)
        {
            yield break;
        }

        isLoadingScene = true;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(dialogueSceneName, LoadSceneMode.Additive);
        if (loadOperation == null)
        {
            Debug.LogError($"[DialogueSceneController] Failed to load dialogue scene: {dialogueSceneName}");
            isLoadingScene = false;
            yield break;
        }

        yield return loadOperation;

        isLoadingScene = false;
        EnsureDialogueBox(false);
        EnsureBackground();
        isLoaded = true;

        if (dialogueBox != null)
        {
            dialogueBox.gameObject.SetActive(true);
        }

        ApplyBackgroundVisibility();
        TryShowPendingDialogue();
    }

    private IEnumerator ShowLoadingWhenReady(string npcName, Sprite portrait)
    {
        while (isLoadingScene)
        {
            yield return null;
        }

        EnsureDialogueBox(true);
        if (dialogueBox != null)
        {
            dialogueBox.ShowLoading(npcName, portrait);
        }
    }

    private void TryShowPendingDialogue()
    {
        EnsureDialogueBox(true);

        if (dialogueBox == null)
        {
            Debug.LogError("[DialogueSceneController] DialogueBoxController is missing. Build the dialogue UI in the scene, add DialogueBoxController to the panel, then drag it into this component.");
            return;
        }

        if (pendingBody == null)
        {
            return;
        }

        DialogueBody body = pendingBody;
        pendingBody = null;
        dialogueBox.Show(body, HandleChoiceSelected);
        DialogueShown?.Invoke();
    }

    private void HandleChoiceSelected(DialogueChoiceResult result)
    {
        Action<DialogueChoiceResult> callback = currentChoiceCallback;
        currentChoiceCallback = null;
        callback?.Invoke(result);
    }

    private void EnsureDialogueBox(bool warnIfMissing)
    {
        if (dialogueBox != null)
        {
            return;
        }

        dialogueBox = FindObjectOfType<DialogueBoxController>(true);
        if (dialogueBox == null && warnIfMissing)
        {
            Debug.LogWarning("[DialogueSceneController] DialogueBoxController is not assigned.");
        }
    }

    private void EnsureBackground()
    {
        if (background != null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(backgroundObjectName))
        {
            background = FindBackgroundByName(backgroundObjectName);
        }
    }

    private void ApplyBackgroundVisibility()
    {
        if (background != null)
        {
            background.SetActive(shouldShowBackground);
        }
    }

    private GameObject FindBackgroundByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(dialogueSceneName))
        {
            Scene dialogueScene = SceneManager.GetSceneByName(dialogueSceneName);
            GameObject foundInDialogueScene = FindObjectInScene(dialogueScene, objectName);
            if (foundInDialogueScene != null)
            {
                return foundInDialogueScene;
            }
        }

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            GameObject found = FindObjectInScene(SceneManager.GetSceneAt(sceneIndex), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindObjectInScene(Scene scene, string objectName)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                if (transforms[transformIndex].name == objectName)
                {
                    return transforms[transformIndex].gameObject;
                }
            }
        }

        return null;
    }

    private bool IsSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    private bool CanUnloadDialogueScene()
    {
        if (SceneManager.sceneCount > 1)
        {
            return true;
        }

        Debug.Log("[DialogueSceneController] Dialogue scene is the only loaded scene, so only the UI was hidden. Open a main scene and load Dialogue additively to test scene unloading.");
        return false;
    }
}
