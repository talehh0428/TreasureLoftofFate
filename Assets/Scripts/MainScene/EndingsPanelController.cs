using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class EndingsPanelController : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private EndingEntryUI entryPrefab;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text emptyText;

    private readonly List<EndingEntryUI> spawnedEntries = new List<EndingEntryUI>();

    private void OnEnable()
    {
        NPCEventEndingRegistry.Changed += Rebuild;
        Rebuild();
    }

    private void OnDisable()
    {
        NPCEventEndingRegistry.Changed -= Rebuild;
    }

    public void Rebuild()
    {
        AutoBind();
        ClearEntries();

        IReadOnlyList<NPCEventEndingGroup> groups = NPCEventEndingRegistry.GetEndingGroups();
        bool hasEndings = groups.Count > 0;

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(!hasEndings);
        }

        if (summaryText != null)
        {
            summaryText.text = BuildSummaryText(groups);
            summaryText.gameObject.SetActive(entryPrefab == null || contentRoot == null);
        }

        if (entryPrefab == null || contentRoot == null)
        {
            return;
        }

        for (int i = 0; i < groups.Count; i++)
        {
            EndingEntryUI entry = Instantiate(entryPrefab, contentRoot);
            entry.name = $"EndingEntry_{groups[i].NpcId}";
            entry.Setup(groups[i]);
            spawnedEntries.Add(entry);
        }
    }

    private static string BuildSummaryText(IReadOnlyList<NPCEventEndingGroup> groups)
    {
        if (groups == null || groups.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            NPCEventEndingGroup group = groups[groupIndex];
            if (groupIndex > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine(group.NpcName);
            for (int endingIndex = 0; endingIndex < group.Endings.Count; endingIndex++)
            {
                NPCEventEndingRecord ending = group.Endings[endingIndex];
                builder.Append(ending.Title);
                builder.Append("——");
                builder.AppendLine(ending.Text);
            }
        }

        return builder.ToString().TrimEnd();
    }

    private void ClearEntries()
    {
        for (int i = 0; i < spawnedEntries.Count; i++)
        {
            if (spawnedEntries[i] != null)
            {
                Destroy(spawnedEntries[i].gameObject);
            }
        }

        spawnedEntries.Clear();
    }

    private void AutoBind()
    {
        if (contentRoot == null)
        {
            Transform content = transform.Find("ScrollView/Viewport/Content");
            if (content != null)
            {
                contentRoot = content;
            }
        }

        if (summaryText == null)
        {
            summaryText = FindChildComponent<TMP_Text>("SummaryText", "EndingSummaryText");
        }

        if (emptyText == null)
        {
            emptyText = FindChildComponent<TMP_Text>("EmptyText");
        }
    }

    private T FindChildComponent<T>(params string[] names) where T : Component
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = transform.Find(names[i]);
            if (child == null)
            {
                continue;
            }

            T component = child.GetComponent<T>();
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }
}
