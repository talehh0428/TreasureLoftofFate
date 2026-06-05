using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCEventDatabase
{
    public readonly Dictionary<string, NPCEventConfig> EventById = new();
    public readonly List<NPCEventConfig> WorldEvents = new();
    public readonly List<NPCEventConfig> CommonEvents = new();
    public readonly List<NPCEventConfig> PersonalEvents = new();
    public readonly List<string> Warnings = new();

    public static NPCEventDatabase FromJson(string json)
    {
        NPCEventDatabase database = new NPCEventDatabase();

        if (string.IsNullOrWhiteSpace(json))
        {
            database.Warnings.Add("事件 JSON 为空。");
            return database;
        }

        NPCEventStoryConfig storyConfig = JsonUtility.FromJson<NPCEventStoryConfig>(json);
        if (storyConfig?.events == null)
        {
            database.Warnings.Add("事件 JSON 解析失败或 events 字段为空。");
            return database;
        }

        foreach (NPCEventConfig eventConfig in storyConfig.events)
        {
            if (eventConfig == null || string.IsNullOrWhiteSpace(eventConfig.id))
            {
                database.Warnings.Add("发现缺少 id 的事件，已跳过。");
                continue;
            }

            if (database.EventById.ContainsKey(eventConfig.id))
            {
                database.Warnings.Add($"事件 id 重复，已跳过后出现的配置: {eventConfig.id}");
                continue;
            }

            NormalizeEvent(eventConfig);
            database.EventById.Add(eventConfig.id, eventConfig);
            AddToTypedList(database, eventConfig);
        }

        SortByPriority(database.WorldEvents);
        SortByPriority(database.CommonEvents);
        SortByPriority(database.PersonalEvents);
        database.ValidateLinks();
        return database;
    }

    public bool TryGetEvent(string eventId, out NPCEventConfig eventConfig)
    {
        return EventById.TryGetValue(eventId, out eventConfig);
    }

    private static void NormalizeEvent(NPCEventConfig eventConfig)
    {
        eventConfig.type = string.IsNullOrWhiteSpace(eventConfig.type)
            ? NPCEventTypes.Personal
            : eventConfig.type.Trim().ToLowerInvariant();
        eventConfig.participants ??= new List<string>();
        eventConfig.trigger ??= new NPCEventTrigger();
        eventConfig.trigger.requirements ??= new List<NPCEventRequirement>();
        eventConfig.trigger.conditions ??= new List<NPCEventCondition>();
        eventConfig.outcomes ??= new List<NPCEventOutcome>();

        foreach (NPCEventOutcome outcome in eventConfig.outcomes)
        {
            outcome.conditions ??= new List<NPCEventCondition>();
            outcome.next ??= new List<NPCEventNext>();
        }
    }

    private static void AddToTypedList(NPCEventDatabase database, NPCEventConfig eventConfig)
    {
        switch (eventConfig.type)
        {
            case NPCEventTypes.World:
                database.WorldEvents.Add(eventConfig);
                break;
            case NPCEventTypes.Common:
                database.CommonEvents.Add(eventConfig);
                break;
            case NPCEventTypes.Personal:
                database.PersonalEvents.Add(eventConfig);
                break;
            default:
                database.Warnings.Add($"未知事件 type: {eventConfig.type}，事件 {eventConfig.id} 已按 personal 处理。");
                eventConfig.type = NPCEventTypes.Personal;
                database.PersonalEvents.Add(eventConfig);
                break;
        }
    }

    private static void SortByPriority(List<NPCEventConfig> events)
    {
        events.Sort((left, right) => right.priority.CompareTo(left.priority));
    }

    private void ValidateLinks()
    {
        foreach (NPCEventConfig eventConfig in EventById.Values)
        {
            if (eventConfig.outcomes.Count == 0)
            {
                Warnings.Add($"事件 {eventConfig.id} 没有配置 outcomes。");
                continue;
            }

            foreach (NPCEventOutcome outcome in eventConfig.outcomes)
            {
                if (string.IsNullOrWhiteSpace(outcome.id))
                {
                    Warnings.Add($"事件 {eventConfig.id} 存在缺少 id 的 outcome。");
                }

                foreach (NPCEventNext next in outcome.next.Where(next => !string.IsNullOrWhiteSpace(next.nextId)))
                {
                    if (NPCEventSpecialIds.IsSpecial(next.nextId))
                    {
                        continue;
                    }

                    if (!EventById.ContainsKey(next.nextId))
                    {
                        Warnings.Add($"事件 {eventConfig.id} 的 outcome {outcome.id} 指向不存在的 nextId: {next.nextId}");
                    }
                }
            }
        }
    }
}

public static class NPCEventTypes
{
    public const string World = "world";
    public const string Common = "common";
    public const string Personal = "personal";
}

public static class NPCEventSpecialIds
{
    public const string Finished = "EVENT_FINISHED";
    public const string Inactive = "EVENT_INACTIVE";

    public static bool IsSpecial(string eventId)
    {
        return eventId == Finished || eventId == Inactive;
    }
}
