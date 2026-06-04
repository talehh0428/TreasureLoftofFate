using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WarehouseSceneLoader : MonoBehaviour
{
    [Header("Scene Setup")]
    [SerializeField] private string warehouseSceneName = "Warehouse";
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
            openButton.onClick.AddListener(LoadWarehouseScene);
        }
    }

    private void OnDisable()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(LoadWarehouseScene);
        }
    }

    public void LoadWarehouseScene()
    {
        if (isLoading || string.IsNullOrWhiteSpace(warehouseSceneName))
        {
            return;
        }

        Scene loadedScene = SceneManager.GetSceneByName(warehouseSceneName);
        if (loadedScene.isLoaded)
        {
            return;
        }

        StartCoroutine(LoadWarehouseSceneRoutine());
    }

    private IEnumerator LoadWarehouseSceneRoutine()
    {
        isLoading = true;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(warehouseSceneName, loadSceneMode);
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
