using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadMainScene : MonoBehaviour
{
    [SerializeField] private string mainSceneAddress = "Scenes/MainScene";
    [SerializeField] private string loadingMessage = "加载中";

    private IEnumerator Start()
    {
        yield return AddressableSceneLoader.LoadSceneRoutine(
            mainSceneAddress,
            LoadSceneMode.Single,
            showOverlay: true,
            hideOverlayWhenDone: false,
            loadingMessage: loadingMessage);
    }
}
