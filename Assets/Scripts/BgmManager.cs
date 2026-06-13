using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 单例 BGM 管理器，负责跨场景播放背景音乐。
/// 不随场景销毁，保证音乐在场景切换时不中断。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BgmManager : MonoBehaviour
{
    [Header("BGM Clips")]
    [SerializeField] private AudioClip mainSceneBgm;
    [SerializeField] private AudioClip marketBgm;

    private static BgmManager instance;
    private AudioSource audioSource;

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
        DontDestroyOnLoad(gameObject);
    }

    public void PlayMainSceneBGM()
    {
        if (mainSceneBgm == null)
        {
            Debug.LogWarning("[BgmManager] mainSceneBgm 未赋值！");
            return;
        }
        PlayClip(mainSceneBgm);
    }

    public void PlayMarketBGM()
    {
        if (marketBgm == null)
        {
            Debug.LogWarning("[BgmManager] marketBgm 未赋值！");
            return;
        }
        PlayClip(marketBgm);
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
