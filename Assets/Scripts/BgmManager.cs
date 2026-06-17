using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 单例 BGM 管理器，负责跨场景播放一首全局背景音乐。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BgmManager : MonoBehaviour
{
    [Header("Global BGM")]
    [SerializeField] private string globalBgmAddress = "Assets/BGM/Porcelain Lanterns.mp3";

    private static BgmManager instance;
    private AudioSource audioSource;
    private AudioClip loadedGlobalBgm;
    private Coroutine loadRoutine;
    private int loadVersion;

    public static BgmManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BgmManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject(nameof(BgmManager));
                    instance = go.AddComponent<BgmManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public void PlayGlobalBGM()
    {
        if (loadedGlobalBgm != null)
        {
            PlayClip(loadedGlobalBgm);
            return;
        }

        StartGlobalBgmLoad();
    }

    private void StartGlobalBgmLoad()
    {
        if (string.IsNullOrWhiteSpace(globalBgmAddress))
        {
            Debug.LogWarning("[BgmManager] globalBgmAddress 未配置！");
            return;
        }

        if (loadRoutine != null)
        {
            return;
        }

        loadVersion++;
        loadRoutine = StartCoroutine(LoadAndPlayGlobalBgmRoutine(globalBgmAddress.Trim(), loadVersion));
    }

    private IEnumerator LoadAndPlayGlobalBgmRoutine(string address, int version)
    {
        AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(address);
        yield return handle;

        loadRoutine = null;
        if (version != loadVersion)
        {
            yield break;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogWarning($"[BgmManager] Failed to load BGM: {address}");
            yield break;
        }

        loadedGlobalBgm = handle.Result;
        PlayClip(loadedGlobalBgm);
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource.clip == clip && audioSource.isPlaying)
            return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
