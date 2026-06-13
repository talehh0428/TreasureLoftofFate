using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 单例 BGM 管理器，负责跨场景播放一首全局背景音乐。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BgmManager : MonoBehaviour
{
    [Header("Global BGM")]
    [FormerlySerializedAs("mainSceneBgm")]
    [SerializeField] private AudioClip globalBgm;

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
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        PlayGlobalBGM();
    }

    public void PlayGlobalBGM()
    {
        if (globalBgm == null)
        {
            Debug.LogWarning("[BgmManager] globalBgm 未赋值！");
            return;
        }
        PlayClip(globalBgm);
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
