using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShopMainSceneController : MonoBehaviour
{
    [Header("NPC Setup")]
    [SerializeField] private Transform npcContentRoot;
    [SerializeField] private NPCItemUI npcItemPrefab;
    [SerializeField] private List<NPCDefinition> npcCatalog = new List<NPCDefinition>();

    private readonly List<NPCItemUI> npcPool = new List<NPCItemUI>();

    private void Awake()
    {
        AutoBindSceneReferences();
    }

    private void Start()
    {
        PopulateNPCs();
    }

    public void PopulateNPCs()
    {
        List<NPCDefinition> validNpcs = npcCatalog
            .Where(npc => npc != null)
            .Distinct()
            .ToList();

        if (validNpcs.Count == 0)
        {
            return;
        }

        RebuildNpcPool(validNpcs);

        for (int index = 0; index < npcPool.Count; index++)
        {
            NPCItemUI npcSlot = npcPool[index];
            npcSlot.Clicked -= HandleNpcClicked;
            npcSlot.Clicked += HandleNpcClicked;
            npcSlot.Setup(validNpcs[index]);
        }

        RebuildNpcLayout();
    }

    private void HandleNpcClicked(NPCItemUI clickedNpc)
    {
        if (clickedNpc == null || !clickedNpc.HasNpc)
        {
            return;
        }

        // 预留给后续对话/商店界面打开逻辑。
        Debug.Log($"NPC 点击: {clickedNpc.CurrentNpc.DisplayName}");
    }

    private void RebuildNpcPool(IReadOnlyList<NPCDefinition> npcDefinitions)
    {
        if (npcContentRoot == null || npcItemPrefab == null)
        {
            return;
        }

        ClearExistingNpcObjects();
        npcPool.Clear();

        for (int index = 0; index < npcDefinitions.Count; index++)
        {
            NPCItemUI npcSlot = Instantiate(npcItemPrefab, npcContentRoot);
            npcSlot.name = $"NpcSlot_{index + 1:00}";
            npcSlot.Setup(npcDefinitions[index]);
            npcPool.Add(npcSlot);
        }
    }

    private void ClearExistingNpcObjects()
    {
        if (npcContentRoot == null)
        {
            return;
        }

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in npcContentRoot)
        {
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (GameObject childObject in childrenToDestroy)
        {
            Destroy(childObject);
        }
    }

    private void RebuildNpcLayout()
    {
        if (npcContentRoot == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        RectTransform contentRect = npcContentRoot as RectTransform;
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    private void AutoBindSceneReferences()
    {
        if (npcContentRoot == null)
        {
            Transform npcContent = transform.Find("NpcPanel/NpcScrollView/Viewport/Content");
            if (npcContent != null)
            {
                npcContentRoot = npcContent;
            }
        }
    }
}
