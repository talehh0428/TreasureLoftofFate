using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SceneLoadingOverlay : MonoBehaviour
{
    private static SceneLoadingOverlay instance;
    private CanvasGroup canvasGroup;
    private TMP_Text messageText;
    private RectTransform spinnerRect;
    private int showDepth;

    public static bool IsVisible => instance != null && instance.canvasGroup != null && instance.canvasGroup.alpha > 0f;

    public static void Show(string message = null)
    {
        SceneLoadingOverlay overlay = EnsureInstance();
        overlay.showDepth++;
        overlay.SetVisible(true, message);
    }

    public static void Hide()
    {
        if (instance == null)
        {
            return;
        }

        instance.showDepth = Mathf.Max(0, instance.showDepth - 1);
        if (instance.showDepth == 0)
        {
            instance.SetVisible(false, null);
        }
    }

    public static void ForceHide()
    {
        if (instance == null)
        {
            return;
        }

        instance.showDepth = 0;
        instance.SetVisible(false, null);
    }

    private static SceneLoadingOverlay EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject root = new GameObject("SceneLoadingOverlay");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<SceneLoadingOverlay>();
        instance.BuildUi();
        instance.SetVisible(false, null);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        GameObject background = new GameObject("Background");
        background.transform.SetParent(transform, false);
        Image image = background.AddComponent<Image>();
        image.color = Color.black;
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(480f, 260f);

        VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 36f;

        GameObject spinnerObject = new GameObject("Spinner");
        spinnerObject.transform.SetParent(content.transform, false);
        spinnerRect = spinnerObject.AddComponent<RectTransform>();
        spinnerRect.sizeDelta = new Vector2(96f, 96f);
        Image spinnerImage = spinnerObject.AddComponent<Image>();
        spinnerImage.sprite = CreateSpinnerSprite(96, 7f, 72f);
        spinnerImage.color = Color.white;
        spinnerImage.raycastTarget = false;
        spinnerImage.type = Image.Type.Simple;
        spinnerImage.preserveAspect = true;

        GameObject textObject = new GameObject("Message");
        textObject.transform.SetParent(content.transform, false);
        messageText = textObject.AddComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        messageText.fontSize = 56f;
        messageText.fontStyle = FontStyles.Bold;
        messageText.raycastTarget = false;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(480f, 80f);
    }

    private void Update()
    {
        if (canvasGroup == null || canvasGroup.alpha <= 0f || spinnerRect == null)
        {
            return;
        }

        spinnerRect.Rotate(0f, 0f, -360f * Time.unscaledDeltaTime);
    }

    private static Sprite CreateSpinnerSprite(int size, float thickness, float gapDegrees)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color transparent = new Color(1f, 1f, 1f, 0f);
        Color solid = Color.white;
        float center = (size - 1) * 0.5f;
        float outerRadius = size * 0.5f - 2f;
        float innerRadius = Mathf.Max(0f, outerRadius - thickness);
        float visibleDegrees = 360f - Mathf.Clamp(gapDegrees, 0f, 300f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 90f, 360f);
                bool isRingPixel = radius >= innerRadius && radius <= outerRadius && angle <= visibleDegrees;
                texture.SetPixel(x, y, isRingPixel ? solid : transparent);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void SetVisible(bool visible, string message)
    {
        if (canvasGroup == null)
        {
            return;
        }

        if (visible)
        {
            gameObject.SetActive(true);
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;

        if (messageText != null)
        {
            messageText.text = visible && !string.IsNullOrWhiteSpace(message) ? message : string.Empty;
        }

        if (!visible)
        {
            gameObject.SetActive(false);
        }
    }
}
