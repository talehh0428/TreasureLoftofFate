using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GuideBookController : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private string resourcesFolderName = "ShopItem";
    [SerializeField] private string editorAssetFolder = "Assets/ShopItem";
    [SerializeField] private List<ShopItemDefinition> itemDefinitions = new List<ShopItemDefinition>();
    [SerializeField] private string defaultSelectedItemId = "0001";

    [Header("Left List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GuideBookItemEntryUI entryPrefab;
    [SerializeField] private Sprite lockedIcon;
    [SerializeField] private RectTransform leftPageRoot;
    [SerializeField] private Image listBackgroundImage;

    [Header("Tabs")]
    [SerializeField] private Button itemTabButton;
    [SerializeField] private Button customerTabButton;
    [SerializeField] private Button closeButton;

    [Header("Panels")]
    [SerializeField] private GameObject itemListRoot;
    [SerializeField] private GuideBookDetailPanelController detailPanel;
    [SerializeField] private TMP_Text bottomHint;
    [SerializeField] [TextArea] private string itemTabHint = "已解锁的物品会显示真实信息。";
    [SerializeField] [TextArea] private string customerTabHint = "顾客图鉴功能暂未开放。";

    private readonly List<GuideBookItemEntryUI> spawnedEntries = new List<GuideBookItemEntryUI>();
    private readonly List<ShopItemDefinition> sortedDefinitions = new List<ShopItemDefinition>();
    private GuideBookItemEntryUI selectedEntry;
    private bool isUnloading;

    private void Awake()
    {
        AutoBind();
        SetupTabPresentation();
        LoadDefinitions();
        ShopItemUnlockRegistry.RegisterDefaults(sortedDefinitions);
        BuildEntries();
        ConfigureButtons();
        SelectItemTab();
        SelectDefaultEntry();
    }

    private void OnEnable()
    {
        ShopItemUnlockRegistry.ItemUnlocked += HandleItemUnlocked;

        if (itemTabButton != null)
        {
            itemTabButton.onClick.AddListener(SelectItemTab);
        }

        if (customerTabButton != null)
        {
            customerTabButton.onClick.AddListener(SelectCustomerTab);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseGuideBookScene);
        }
    }

    private void OnDisable()
    {
        ShopItemUnlockRegistry.ItemUnlocked -= HandleItemUnlocked;

        if (itemTabButton != null)
        {
            itemTabButton.onClick.RemoveListener(SelectItemTab);
        }

        if (customerTabButton != null)
        {
            customerTabButton.onClick.RemoveListener(SelectCustomerTab);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseGuideBookScene);
        }
    }

    private void OnValidate()
    {
        AutoBind();
        SyncDefinitionsInEditor();
        SetupTabPresentation();
    }

    private void ConfigureButtons()
    {
        if (itemTabButton != null)
        {
            itemTabButton.onClick.RemoveListener(SelectItemTab);
        }

        if (customerTabButton != null)
        {
            customerTabButton.onClick.RemoveListener(SelectCustomerTab);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseGuideBookScene);
        }
    }

    public void CloseGuideBookScene()
    {
        if (isUnloading)
        {
            return;
        }

        Scene currentScene = gameObject.scene;
        if (!currentScene.IsValid() || !currentScene.isLoaded)
        {
            return;
        }

        isUnloading = true;
        SceneManager.UnloadSceneAsync(currentScene);
    }

    private void LoadDefinitions()
    {
        List<ShopItemDefinition> loadedDefinitions = itemDefinitions
            .Where(definition => definition != null)
            .ToList();

        if (loadedDefinitions.Count == 0)
        {
            loadedDefinitions = Resources.LoadAll<ShopItemDefinition>(resourcesFolderName).ToList();
        }

        sortedDefinitions.Clear();
        sortedDefinitions.AddRange(loadedDefinitions
            .Where(definition => definition != null)
            .OrderBy(definition => definition.ItemId));
    }

    private void BuildEntries()
    {
        if (contentRoot == null || entryPrefab == null)
        {
            return;
        }

        ClearEntries();

        for (int index = 0; index < sortedDefinitions.Count; index++)
        {
            ShopItemDefinition definition = sortedDefinitions[index];
            GuideBookItemEntryUI entry = Instantiate(entryPrefab, contentRoot);
            entry.name = $"GuideBookItemEntry_{definition.ItemId}";
            entry.Clicked += HandleEntryClicked;
            entry.transform.SetSiblingIndex(index);
            entry.Setup(BuildEntryData(definition), lockedIcon);
            spawnedEntries.Add(entry);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot as RectTransform);
    }

    private void SelectDefaultEntry()
    {
        GuideBookItemEntryUI defaultEntry = spawnedEntries.FirstOrDefault(entry =>
            entry.CurrentEntry.ItemId == defaultSelectedItemId);

        if (defaultEntry == null && spawnedEntries.Count > 0)
        {
            defaultEntry = spawnedEntries[0];
        }

        if (defaultEntry != null)
        {
            SelectEntry(defaultEntry);
        }
        else if (detailPanel != null)
        {
            detailPanel.ShowEmpty(lockedIcon);
        }
    }

    private void HandleEntryClicked(GuideBookItemEntryUI clickedEntry)
    {
        if (clickedEntry == null)
        {
            return;
        }

        SelectEntry(clickedEntry);
    }

    private void SelectEntry(GuideBookItemEntryUI entryToSelect)
    {
        selectedEntry = entryToSelect;

        for (int index = 0; index < spawnedEntries.Count; index++)
        {
            GuideBookItemEntryUI entry = spawnedEntries[index];
            entry.SetSelected(entry == selectedEntry);
        }

        if (detailPanel != null && selectedEntry != null)
        {
            detailPanel.Show(selectedEntry.CurrentEntry, lockedIcon);
        }
    }

    private void HandleItemUnlocked(string itemId)
    {
        for (int index = 0; index < spawnedEntries.Count; index++)
        {
            GuideBookItemEntryUI entry = spawnedEntries[index];
            if (entry == null || entry.CurrentEntry.ItemId != itemId)
            {
                continue;
            }

            entry.Setup(BuildEntryData(entry.CurrentEntry.Definition), lockedIcon);
            entry.SetSelected(entry == selectedEntry);

            if (entry == selectedEntry && detailPanel != null)
            {
                detailPanel.Show(entry.CurrentEntry, lockedIcon);
            }
        }
    }

    private void SelectItemTab()
    {
        if (itemListRoot != null)
        {
            itemListRoot.SetActive(true);
        }

        if (bottomHint != null)
        {
            bottomHint.gameObject.SetActive(true);
            bottomHint.text = itemTabHint;
        }

        ApplyTabStacking(itemTabButton, customerTabButton);

        if (selectedEntry != null && detailPanel != null)
        {
            detailPanel.Show(selectedEntry.CurrentEntry, lockedIcon);
        }
    }

    private void SelectCustomerTab()
    {
        if (itemListRoot != null)
        {
            itemListRoot.SetActive(false);
        }

        if (bottomHint != null)
        {
            bottomHint.gameObject.SetActive(true);
            bottomHint.text = customerTabHint;
        }

        ApplyTabStacking(customerTabButton, itemTabButton);

        if (detailPanel != null)
        {
            detailPanel.ShowEmpty();
        }
    }

    private void SetupTabPresentation()
    {
        if (listBackgroundImage != null)
        {
            listBackgroundImage.raycastTarget = false;
        }
    }

    private void ApplyTabStacking(Button selectedButton, Button unselectedButton)
    {
        if (selectedButton == null || unselectedButton == null || listBackgroundImage == null)
        {
            return;
        }

        Transform selectedTransform = selectedButton.transform;
        Transform unselectedTransform = unselectedButton.transform;
        Transform backgroundTransform = listBackgroundImage.transform;

        if (selectedTransform.parent != backgroundTransform.parent ||
            unselectedTransform.parent != backgroundTransform.parent)
        {
            return;
        }

        int baseIndex = Mathf.Min(
            selectedTransform.GetSiblingIndex(),
            unselectedTransform.GetSiblingIndex(),
            backgroundTransform.GetSiblingIndex());

        unselectedTransform.SetSiblingIndex(baseIndex);
        backgroundTransform.SetSiblingIndex(baseIndex + 1);
        selectedTransform.SetSiblingIndex(baseIndex + 2);
    }

    private GuideBookEntryData BuildEntryData(ShopItemDefinition definition)
    {
        return new GuideBookEntryData(definition, ShopItemUnlockRegistry.IsUnlocked(definition));
    }

    private void ClearEntries()
    {
        for (int index = 0; index < spawnedEntries.Count; index++)
        {
            GuideBookItemEntryUI entry = spawnedEntries[index];
            if (entry != null)
            {
                entry.Clicked -= HandleEntryClicked;
                Destroy(entry.gameObject);
            }
        }

        spawnedEntries.Clear();
        selectedEntry = null;
    }

    private void AutoBind()
    {
        if (contentRoot == null)
        {
            Transform content = transform.Find("BookPanel/LeftPage/ItemScrollView/Viewport/Content");
            if (content != null)
            {
                contentRoot = content;
            }
        }

        if (leftPageRoot == null)
        {
            Transform leftPage = transform.Find("BookPanel/LeftPage");
            if (leftPage != null)
            {
                leftPageRoot = leftPage as RectTransform;
            }
        }

        if (listBackgroundImage == null)
        {
            listBackgroundImage = FindChildComponent<Image>("BookPanel/LeftPage/ListBackground");
        }

        if (itemTabButton == null)
        {
            itemTabButton = FindButtonByName("ItemTabButton", "物品");
        }

        if (customerTabButton == null)
        {
            customerTabButton = FindButtonByName("CustomerTabButton", "顾客");
        }

        if (closeButton == null)
        {
            closeButton = FindButtonByName("CloseButton", "ExitButton", "BackButton", "返回");
        }

        if (itemListRoot == null)
        {
            Transform scrollView = transform.Find("BookPanel/LeftPage/ItemScrollView");
            if (scrollView != null)
            {
                itemListRoot = scrollView.gameObject;
            }
        }

        if (bottomHint == null)
        {
            bottomHint = FindChildComponent<TMP_Text>("BookPanel/LeftPage/BottomHint");
        }

        if (detailPanel == null)
        {
            Transform rightPage = transform.Find("BookPanel/RightPage");
            if (rightPage != null)
            {
                detailPanel = rightPage.GetComponent<GuideBookDetailPanelController>();
            }
        }
    }

    private void SyncDefinitionsInEditor()
    {
#if UNITY_EDITOR
        if (Application.isPlaying || string.IsNullOrWhiteSpace(editorAssetFolder))
        {
            return;
        }

        string[] assetGuids = AssetDatabase.FindAssets("t:ShopItemDefinition", new[] { editorAssetFolder });
        List<ShopItemDefinition> discoveredDefinitions = new List<ShopItemDefinition>();

        for (int index = 0; index < assetGuids.Length; index++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[index]);
            ShopItemDefinition definition = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(assetPath);
            if (definition != null)
            {
                discoveredDefinitions.Add(definition);
            }
        }

        itemDefinitions = discoveredDefinitions
            .OrderBy(definition => definition.ItemId)
            .ToList();
#endif
    }

    private Button FindButtonByName(params string[] names)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (button.name == names[nameIndex])
                {
                    return button;
                }
            }
        }

        return null;
    }

    private T FindChildComponent<T>(string relativePath) where T : Component
    {
        Transform child = transform.Find(relativePath);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<T>();
    }
}
