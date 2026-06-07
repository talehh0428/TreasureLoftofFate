using System.Collections;
using TMPro;
using UnityEngine;

public class ScreenFadeTransition : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] [Min(0f)] private float fadeDuration = 0.6f;
    [SerializeField] [Min(0f)] private float holdDuration = 0.6f;
    [SerializeField] [Min(0.05f)] private float ellipsisInterval = 0.25f;

    private void Awake()
    {
        AutoBind();
        SetVisible(false);
    }

    private void OnValidate()
    {
        AutoBind();
    }

    public IEnumerator Play(string message, IEnumerator coveredOperation)
    {
        gameObject.SetActive(true);
        AutoBind();
        SetVisible(true);
        ResetText(message);

        IEnumerator fadeIn = FadeTo(1f);
        while (fadeIn.MoveNext())
        {
            UpdateAnimatedText(message);
            yield return fadeIn.Current;
        }

        if (coveredOperation != null)
        {
            while (coveredOperation.MoveNext())
            {
                UpdateAnimatedText(message);
                yield return coveredOperation.Current;
            }
        }

        float held = 0f;
        while (held < holdDuration)
        {
            held += Time.unscaledDeltaTime;
            UpdateAnimatedText(message);
            yield return null;
        }

        IEnumerator fadeOut = FadeTo(0f);
        while (fadeOut.MoveNext())
        {
            UpdateAnimatedText(message);
            yield return fadeOut.Current;
        }

        SetVisible(false);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (overlayGroup == null)
        {
            yield break;
        }

        float startAlpha = overlayGroup.alpha;
        if (fadeDuration <= 0f)
        {
            overlayGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlayGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        overlayGroup.alpha = targetAlpha;
    }

    private float textTick;
    private int dotCount;

    private void ResetText(string message)
    {
        if (messageText == null)
        {
            return;
        }

        textTick = 0f;
        dotCount = 0;
        messageText.text = message;
    }

    private void UpdateAnimatedText(string message)
    {
        if (messageText == null)
        {
            return;
        }

        textTick += Time.unscaledDeltaTime;

        if (textTick >= ellipsisInterval)
        {
            textTick = 0f;
            dotCount = (dotCount + 1) % 4;
            messageText.text = $"{message}{new string('.', dotCount)}";
        }
    }

    private void SetVisible(bool visible)
    {
        if (overlayGroup == null)
        {
            return;
        }

        overlayGroup.alpha = visible ? overlayGroup.alpha : 0f;
        overlayGroup.blocksRaycasts = visible;
        overlayGroup.interactable = visible;
        overlayGroup.gameObject.SetActive(visible);

        if (messageText != null && !visible)
        {
            messageText.text = string.Empty;
        }
    }

    private void AutoBind()
    {
        if (overlayGroup == null)
        {
            overlayGroup = GetComponent<CanvasGroup>();
        }

        if (messageText == null)
        {
            messageText = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
