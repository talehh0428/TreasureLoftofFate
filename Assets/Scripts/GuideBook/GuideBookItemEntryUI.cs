using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GuideBookItemEntryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;

    [Header("Display")]
    [SerializeField] private string lockedName = "???";
    [SerializeField] private Color normalBackgroundColor = new Color(0.58f, 0.48f, 0.29f, 1f);
    [SerializeField] private Color selectedBackgroundColor = new Color(0.83f, 0.69f, 0.42f, 1f);
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color selectedTextColor = new Color(0.17f, 0.12f, 0.05f, 1f);
    [SerializeField] private Color lockedTintColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color unlockedTintColor = Color.white;

    private GuideBookEntryData currentEntry;

    public event Action<GuideBookItemEntryUI> Clicked;

    public GuideBookEntryData CurrentEntry => currentEntry;

    private void Awake()
    {
        AutoBind();
        button.onClick.AddListener(HandleClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }

    private void Reset()
    {
        AutoBind();
    }

    private void OnValidate()
    {
        AutoBind();
    }

    public void Setup(GuideBookEntryData entryData, Sprite lockedIcon)
    {
        currentEntry = entryData;
        AutoBind();

        bool isUnlocked = entryData.IsUnlocked;
        if (iconImage != null)
        {
            iconImage.sprite = isUnlocked ? entryData.Icon : lockedIcon;
            iconImage.enabled = iconImage.sprite != null;
            iconImage.color = isUnlocked ? unlockedTintColor : lockedTintColor;
        }

        if (nameText != null)
        {
            nameText.text = isUnlocked ? entryData.DisplayName : lockedName;
        }

        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedBackgroundColor : normalBackgroundColor;
        }

        if (nameText != null)
        {
            nameText.color = isSelected ? selectedTextColor : normalTextColor;
        }
    }

    private void HandleClicked()
    {
        Clicked?.Invoke(this);
    }

    private void AutoBind()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (iconImage == null)
        {
            iconImage = FindChildComponent<Image>("EntryIcon");
        }

        if (nameText == null)
        {
            nameText = FindChildComponent<TMP_Text>("EntryName");
        }
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<T>();
    }
}
