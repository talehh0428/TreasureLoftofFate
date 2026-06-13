using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂载到 Button 上，覆盖全局按钮音效为该字段指定的音效。
/// 留空则依然使用全局默认音效（wood_tap）。
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSoundOverride : MonoBehaviour
{
    [Header("特殊音效覆盖")]
    [Tooltip("留空 = 使用全局音效。拖入后该按钮将播放此音效（从 0s 开始）。")]
    [SerializeField] private AudioClip overrideClip;

    [Tooltip("此音效的开始时间（秒）。")]
    [SerializeField] private float startTime = 0f;

    [Tooltip("此音效的结束时间（秒），0 = 播完为止。")]
    [SerializeField] private float endTime = 0f;

    /// <summary>此按钮是否设定了特殊音效。</summary>
    public bool HasOverride => overrideClip != null;

    /// <summary>覆盖音效。</summary>
    public AudioClip OverrideClip => overrideClip;

    /// <summary>覆盖音效开始时间。</summary>
    public float StartTime => startTime;

    /// <summary>覆盖音效结束时间（0 = 播完为止）。</summary>
    public float EndTime => endTime;
}
