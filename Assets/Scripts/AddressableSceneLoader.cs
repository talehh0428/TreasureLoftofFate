using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public static class AddressableSceneLoader
{
    private const string AddressPrefix = "Scenes/";
    private const string DefaultLoadingMessage = "加载中";
    private static readonly Dictionary<string, SceneInstance> LoadedScenes = new Dictionary<string, SceneInstance>();

    public static IEnumerator LoadSceneRoutine(
        string sceneKey,
        LoadSceneMode mode = LoadSceneMode.Single,
        bool showOverlay = true,
        bool hideOverlayWhenDone = true,
        string loadingMessage = DefaultLoadingMessage)
    {
        string address = ToSceneAddress(sceneKey);
        if (string.IsNullOrWhiteSpace(address))
        {
            yield break;
        }

        if (IsSceneLoaded(sceneKey))
        {
            yield break;
        }

        if (showOverlay)
        {
            SceneLoadingOverlay.Show(loadingMessage);
        }

        AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(address, mode);
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[AddressableSceneLoader] Failed to load scene: {address}");
            if (showOverlay)
            {
                SceneLoadingOverlay.Hide();
            }

            yield break;
        }

        SceneInstance sceneInstance = handle.Result;
        RememberLoadedScene(sceneKey, sceneInstance);
        yield return null;

        if (showOverlay && hideOverlayWhenDone)
        {
            SceneLoadingOverlay.Hide();
        }
    }

    public static IEnumerator UnloadSceneRoutine(string sceneKey)
    {
        string address = ToSceneAddress(sceneKey);
        if (string.IsNullOrWhiteSpace(address))
        {
            yield break;
        }

        if (LoadedScenes.TryGetValue(address, out SceneInstance sceneInstance))
        {
            AsyncOperationHandle<SceneInstance> unloadHandle = Addressables.UnloadSceneAsync(sceneInstance);
            yield return unloadHandle;
            ForgetLoadedScene(address);
            yield break;
        }

        Scene loadedScene = SceneManager.GetSceneByName(ToSceneName(sceneKey));
        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            yield break;
        }

        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedScene);
        if (unloadOperation != null)
        {
            yield return unloadOperation;
        }
    }

    public static IEnumerator UnloadSceneRoutine(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            yield break;
        }

        string sceneName = scene.name;
        string address = ToSceneAddress(sceneName);
        if (LoadedScenes.TryGetValue(address, out SceneInstance sceneInstance))
        {
            AsyncOperationHandle<SceneInstance> unloadHandle = Addressables.UnloadSceneAsync(sceneInstance);
            yield return unloadHandle;
            ForgetLoadedScene(address);
            yield break;
        }

        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);
        if (unloadOperation != null)
        {
            yield return unloadOperation;
        }
    }

    public static bool IsSceneLoaded(string sceneKey)
    {
        string sceneName = ToSceneName(sceneKey);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    public static string ToSceneAddress(string sceneKey)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            return string.Empty;
        }

        string trimmed = sceneKey.Trim();
        return trimmed.Contains("/") ? trimmed : $"{AddressPrefix}{trimmed}";
    }

    public static string ToSceneName(string sceneKey)
    {
        if (string.IsNullOrWhiteSpace(sceneKey))
        {
            return string.Empty;
        }

        string trimmed = sceneKey.Trim();
        int slashIndex = trimmed.LastIndexOf('/');
        return slashIndex >= 0 ? trimmed.Substring(slashIndex + 1) : trimmed;
    }

    private static void RememberLoadedScene(string sceneKey, SceneInstance sceneInstance)
    {
        string address = ToSceneAddress(sceneKey);
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        LoadedScenes[address] = sceneInstance;
    }

    private static void ForgetLoadedScene(string address)
    {
        LoadedScenes.Remove(address);
    }
}
