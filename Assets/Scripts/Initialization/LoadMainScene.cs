using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadMainScene : MonoBehaviour
{
    [SerializeField] private string mainSceneAddress = "Scenes/MainScene";

    private IEnumerator Start()
    {
        yield return AddressableSceneLoader.LoadSceneRoutine(mainSceneAddress, LoadSceneMode.Single);
    }
}
