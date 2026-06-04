using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GuideBookSceneLoader : MonoBehaviour
{
    [Header("Scene Setup")]
    [SerializeField] private string guideBookSceneName = "GuideBook";
    [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Additive;
    [SerializeField] private Button openButton;

    private bool isLoading;

    private void Awake()
    {
        if (openButton == null)
        {
            openButton = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(LoadGuideBookScene);
        }
    }

    private void OnDisable()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(LoadGuideBookScene);
        }
    }

    public void LoadGuideBookScene()
    {
        if (isLoading || string.IsNullOrWhiteSpace(guideBookSceneName))
        {
            return;
        }

        Scene loadedScene = SceneManager.GetSceneByName(guideBookSceneName);
        if (loadedScene.isLoaded)
        {
            return;
        }

        StartCoroutine(LoadGuideBookSceneRoutine());
    }

    private IEnumerator LoadGuideBookSceneRoutine()
    {
        isLoading = true;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(guideBookSceneName, loadSceneMode);
        if (loadOperation == null)
        {
            isLoading = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        isLoading = false;
    }
}
