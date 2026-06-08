using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuideBookDetailPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailRarity;
    [SerializeField] private TMP_Text detailPrice;
    [SerializeField] private TMP_Text detailAttack;
    [SerializeField] private TMP_Text detailDefense;
    [SerializeField] private TMP_Text detailSpeed;
    [SerializeField] private TMP_Text detailDescription;

    [Header("Locked State")]
    [SerializeField] private string lockedName = "???";
    [SerializeField] [TextArea] private string lockedDescription = "尚未解锁该物品。";
    [SerializeField] private string lockedPrice = "???灵石";
    [SerializeField] private string lockedStat = "???";

    private void Awake()
    {
        AutoBind();
    }

    public void Show(GuideBookEntryData entryData, Sprite lockedIcon)
    {
        AutoBind();

        if (entryData.IsNpc)
        {
            ShowNpc(entryData);
            return;
        }

        if (entryData.Definition == null)
        {
            ShowEmpty(lockedIcon);
            return;
        }

        if (entryData.IsUnlocked)
        {
            ApplyUnlocked(entryData);
            return;
        }

        ApplyLocked(entryData, lockedIcon);
    }

    public void ShowNpc(GuideBookEntryData entryData)
    {
        AutoBind();

        if (!entryData.IsNpc)
        {
            ShowEmpty();
            return;
        }

        if (detailIcon != null)
        {
            detailIcon.sprite = entryData.Icon;
            detailIcon.enabled = entryData.Icon != null;
        }

        SetActive(detailRarity, false);
        SetActive(detailPrice, false);
        SetText(detailName, entryData.DisplayName);
        SetText(detailRarity, string.Empty);
        SetText(detailPrice, string.Empty);
        SetText(detailAttack, $"攻击—{entryData.Attack}");
        SetText(detailDefense, $"防御—{entryData.Defense}");
        SetText(detailSpeed, $"遁速—{entryData.MovementSpeed}");
        SetText(detailDescription, entryData.Description);
    }

    public void ShowEmpty(Sprite lockedIcon = null)
    {
        if (detailIcon != null)
        {
            detailIcon.sprite = lockedIcon;
            detailIcon.enabled = lockedIcon != null;
        }

        SetActive(detailRarity, true);
        SetActive(detailPrice, true);
        SetText(detailName, string.Empty);
        SetText(detailRarity, string.Empty);
        SetText(detailPrice, string.Empty);
        SetText(detailAttack, string.Empty);
        SetText(detailDefense, string.Empty);
        SetText(detailSpeed, string.Empty);
        SetText(detailDescription, string.Empty);
    }

    private void ApplyUnlocked(GuideBookEntryData entryData)
    {
        if (detailIcon != null)
        {
            detailIcon.sprite = entryData.Icon;
            detailIcon.enabled = entryData.Icon != null;
        }

        SetActive(detailRarity, true);
        SetActive(detailPrice, true);
        SetText(detailName, entryData.DisplayName);
        SetText(detailRarity, entryData.Rarity.ToDisplayName());
        SetText(detailPrice, $"{entryData.Price}灵石");
        SetText(detailAttack, $"攻击—{entryData.Attack}");
        SetText(detailDefense, $"防御—{entryData.Defense}");
        SetText(detailSpeed, $"遁速—{entryData.MovementSpeed}");
        SetText(detailDescription, entryData.Description);
    }

    private void ApplyLocked(GuideBookEntryData entryData, Sprite lockedIcon)
    {
        if (detailIcon != null)
        {
            detailIcon.sprite = lockedIcon;
            detailIcon.enabled = lockedIcon != null;
        }

        SetActive(detailRarity, true);
        SetActive(detailPrice, true);
        SetText(detailName, lockedName);
        SetText(detailRarity, entryData.Rarity.ToDisplayName());
        SetText(detailPrice, lockedPrice);
        SetText(detailAttack, $"攻击—{lockedStat}");
        SetText(detailDefense, $"防御—{lockedStat}");
        SetText(detailSpeed, $"遁速—{lockedStat}");
        SetText(detailDescription, lockedDescription);
    }

    private void AutoBind()
    {
        if (detailIcon == null)
        {
            detailIcon = FindChildComponent<Image>("DetailIcon");
        }

        if (detailName == null)
        {
            detailName = FindChildComponent<TMP_Text>("DetailName");
        }

        if (detailRarity == null)
        {
            detailRarity = FindChildComponent<TMP_Text>("DetailRarity");
        }

        if (detailPrice == null)
        {
            detailPrice = FindChildComponent<TMP_Text>("DetailPrice");
        }

        if (detailAttack == null)
        {
            detailAttack = FindChildComponent<TMP_Text>("DetailAttack");
        }

        if (detailDefense == null)
        {
            detailDefense = FindChildComponent<TMP_Text>("DetailDefense");
        }

        if (detailSpeed == null)
        {
            detailSpeed = FindChildComponent<TMP_Text>("DetailSpeed");
        }

        if (detailDescription == null)
        {
            detailDescription = FindChildComponent<TMP_Text>("DetailDescription");
        }
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private void SetActive(Component target, bool isActive)
    {
        if (target != null)
        {
            target.gameObject.SetActive(isActive);
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
