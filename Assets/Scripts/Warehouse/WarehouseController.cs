using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WarehouseController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private WarehouseItemSlotUI itemSlotPrefab;
    [SerializeField] private ScrollRect itemScrollView;
    [SerializeField] private WarehouseDetailPanelController detailPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text emptyText;

    private readonly List<WarehouseItemSlotUI> slotPool = new List<WarehouseItemSlotUI>();
    private WarehouseItemSlotUI selectedSlot;
    private bool isUnloading;

    private void Awake()
    {
        AutoBind();
        ConfigureScrollView();
        Rebuild();
    }

    private void OnEnable()
    {
        WarehouseInventory.Changed += Rebuild;

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWarehouseScene);
        }
    }

    private void OnDisable()
    {
        WarehouseInventory.Changed -= Rebuild;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseWarehouseScene);
        }
    }

    public void Rebuild()
    {
        AutoBind();

        IReadOnlyList<WarehouseItemStack> stacks = WarehouseInventory.GetStacks()
            .OrderBy(stack => stack.ItemId)
            .ToList();

        RebuildSlotPool(stacks);

        for (int index = 0; index < slotPool.Count; index++)
        {
            WarehouseItemSlotUI slot = slotPool[index];
            slot.Clicked -= HandleSlotClicked;
            slot.Clicked += HandleSlotClicked;
            slot.Setup(stacks[index]);
        }

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(stacks.Count == 0);
        }

        SelectDefaultSlot();
        RebuildLayout();
    }

    public void CloseWarehouseScene()
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

    private void HandleSlotClicked(WarehouseItemSlotUI clickedSlot)
    {
        if (clickedSlot == null || !clickedSlot.HasItem)
        {
            return;
        }

        SelectSlot(clickedSlot);
    }

    private void SelectDefaultSlot()
    {
        WarehouseItemSlotUI firstSlot = slotPool.FirstOrDefault(slot => slot.HasItem);
        if (firstSlot != null)
        {
            SelectSlot(firstSlot);
        }
        else
        {
            selectedSlot = null;
            if (detailPanel != null)
            {
                detailPanel.ShowEmpty();
            }
        }
    }

    private void SelectSlot(WarehouseItemSlotUI slotToSelect)
    {
        selectedSlot = slotToSelect;

        for (int index = 0; index < slotPool.Count; index++)
        {
            WarehouseItemSlotUI slot = slotPool[index];
            slot.SetSelected(slot == selectedSlot);
        }

        if (detailPanel != null && selectedSlot != null)
        {
            detailPanel.Show(selectedSlot.CurrentStack);
        }
    }

    private void RebuildSlotPool(IReadOnlyList<WarehouseItemStack> stacks)
    {
        if (contentRoot == null || itemSlotPrefab == null)
        {
            return;
        }

        ClearExistingSlotObjects();
        slotPool.Clear();

        for (int index = 0; index < stacks.Count; index++)
        {
            WarehouseItemSlotUI slot = Instantiate(itemSlotPrefab, contentRoot);
            slot.name = $"WarehouseItemSlot_{index + 1:00}";
            slotPool.Add(slot);
        }
    }

    private void ClearExistingSlotObjects()
    {
        if (contentRoot == null)
        {
            return;
        }

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in contentRoot)
        {
            childrenToDestroy.Add(child.gameObject);
        }

        for (int index = 0; index < childrenToDestroy.Count; index++)
        {
            Destroy(childrenToDestroy[index]);
        }
    }

    private void AutoBind()
    {
        if (contentRoot == null)
        {
            Transform content = transform.Find("WarehousePanel/ItemScrollView/Viewport/Content");
            if (content != null)
            {
                contentRoot = content;
            }
        }

        if (itemScrollView == null)
        {
            Transform scrollView = transform.Find("WarehousePanel/ItemScrollView");
            if (scrollView != null)
            {
                itemScrollView = scrollView.GetComponent<ScrollRect>();
            }
        }

        if (detailPanel == null)
        {
            Transform detailRoot = transform.Find("WarehousePanel/DetailPanel");
            if (detailRoot != null)
            {
                detailPanel = detailRoot.GetComponent<WarehouseDetailPanelController>();
            }
        }

        if (closeButton == null)
        {
            closeButton = FindButtonByName("CloseButton", "ExitButton", "BackButton", "返回");
        }

        if (emptyText == null)
        {
            emptyText = FindChildComponent<TMP_Text>("WarehousePanel/EmptyText");
        }
    }

    private void ConfigureScrollView()
    {
        if (itemScrollView == null || contentRoot == null)
        {
            return;
        }

        RectTransform contentRect = contentRoot as RectTransform;
        RectTransform viewportRect = contentRoot.parent as RectTransform;
        itemScrollView.content = contentRect;
        itemScrollView.viewport = viewportRect;
        itemScrollView.vertical = true;
        itemScrollView.movementType = ScrollRect.MovementType.Clamped;
    }

    private void RebuildLayout()
    {
        Canvas.ForceUpdateCanvases();

        RectTransform contentRect = contentRoot as RectTransform;
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        if (itemScrollView != null)
        {
            itemScrollView.verticalNormalizedPosition = 1f;
        }
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
        return child == null ? null : child.GetComponent<T>();
    }
}
