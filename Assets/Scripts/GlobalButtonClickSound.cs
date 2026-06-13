using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 全局按钮点击音效管理器。
/// 挂载到场景中任意对象上（建议放在 EventSystem 或 Canvas 上），
/// 自动检测所有 Button 点击并播放 wood_tap 音效（从 0.9s 开始）。
/// </summary>
public class GlobalButtonClickSound : MonoBehaviour
{
    [Header("全局按钮点击音效")]
    [Tooltip("默认音效：wood_tap（从 0.9s 播放到 1.1s）。")]
    [SerializeField] private AudioClip clickSound;

    [Tooltip("音效开始时间（秒）。")]
    [SerializeField] private float startTime = 0.9f;

    [Tooltip("音效结束时间（秒）。")]
    [SerializeField] private float endTime = 1.1f;

    private static GlobalButtonClickSound instance;
    private AudioSource audioSource;
    private HashSet<Button> trackedButtons = new HashSet<Button>();

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 创建独立的 AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.enabled = true;

        // 查找当前场景所有 Button
        HookAllButtons();

        // 场景加载后自动重新扫描
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 启动定时扫描，捕获动态生成或后激活的 Button
        StartCoroutine(PeriodicRefresh());
    }

    private IEnumerator PeriodicRefresh()
    {
        WaitForSeconds interval = new WaitForSeconds(1f);
        while (true)
        {
            yield return interval;
            HookAllButtons();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookAllButtons();
    }

    /// <summary>
    /// 扫描场景中所有 Button 并绑定点击事件。
    /// 使用 EventTrigger 而非 btn.onClick，避免被 RemoveAllListeners 清掉。
    /// </summary>
    private void HookAllButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button btn in buttons)
        {
            if (trackedButtons.Contains(btn)) continue;

            trackedButtons.Add(btn);

            // EventTrigger 独立于 Button.onClick，不会被 RemoveAllListeners 影响
            EventTrigger trigger = btn.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = btn.gameObject.AddComponent<EventTrigger>();
            }
            if (trigger.triggers == null)
            {
                trigger.triggers = new List<EventTrigger.Entry>();
            }

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((BaseEventData data) => PlayClickSound(btn));
            trigger.triggers.Add(entry);
        }
    }

    /// <summary>
    /// 播放点击音效。检查按钮是否有 ButtonSoundOverride，有则使用覆盖音效。
    /// </summary>
    private void PlayClickSound(Button btn)
    {
        if (clickSound == null) return;

        // 检查是否绑定了特殊音效覆盖
        AudioClip clip = clickSound;
        float start = startTime;
        float end = endTime;

        ButtonSoundOverride overrideComp = btn.GetComponent<ButtonSoundOverride>();
        if (overrideComp != null && overrideComp.HasOverride)
        {
            clip = overrideComp.OverrideClip;
            start = overrideComp.StartTime;
            end = overrideComp.EndTime;
        }

        // 不中断正在播放的同一音效（避免连续点击重叠）
        if (audioSource.isPlaying && audioSource.clip == clip) return;

        audioSource.clip = clip;
        audioSource.time = Mathf.Clamp(start, 0f, clip.length);
        audioSource.Play();

        // 如果设定了结束时间（> start），则在结束时间处截断
        if (end > start)
        {
            float duration = Mathf.Max(0f, end - start);
            StartCoroutine(StopAfterDelay(duration, clip));
        }
    }

    private IEnumerator StopAfterDelay(float delay, AudioClip clip)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource.isPlaying && audioSource.clip == clip)
        {
            audioSource.Stop();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 手动刷新，重新扫描场景中所有 Button 并绑定音效。
    /// 动态实例化按钮后调用此方法。 </summary>
    public static void Refresh()
    {
        if (instance != null)
        {
            instance.HookAllButtons();
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        if (clickSound != null) return;
        AutoAssignDefaultSound();
    }

    [ContextMenu("Auto Assign Default Sound")]
    private void AutoAssignDefaultSound()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("wood_tap t:AudioClip");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            clickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[GlobalButtonClickSound] 已自动分配默认音效: {path}");
        }
        else
        {
            Debug.LogWarning("[GlobalButtonClickSound] 未找到 wood_tap 音效文件，请手动拖入。");
        }
    }
#endif
}
