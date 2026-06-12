using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class NPCEventEndingRegistry
{
    public static event Action Changed;

    private static readonly Dictionary<string, List<NPCEventEndingRecord>> EndingsByNpcId =
        new Dictionary<string, List<NPCEventEndingRecord>>();
    private static bool isRestoring;

    public static bool RegisterEnding(NPCDefinition npc, NPCEventConfig eventConfig, NPCEventOutcome outcome)
    {
        if (npc == null || eventConfig == null || outcome == null || !IsEndingEvent(eventConfig.id))
        {
            return false;
        }

        string text = outcome.text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string npcId = npc.NpcId;
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return false;
        }

        if (!EndingsByNpcId.TryGetValue(npcId, out List<NPCEventEndingRecord> endings))
        {
            endings = new List<NPCEventEndingRecord>();
            EndingsByNpcId.Add(npcId, endings);
        }

        bool alreadyRegistered = endings.Any(ending =>
            ending.EventId == eventConfig.id &&
            ending.Text == text);

        if (alreadyRegistered)
        {
            return false;
        }

        endings.Add(new NPCEventEndingRecord(
            npc,
            npc.NpcId,
            npc.DisplayName,
            npc.Avatar,
            eventConfig.id,
            string.IsNullOrWhiteSpace(eventConfig.title) ? eventConfig.id : eventConfig.title,
            text));

        Changed?.Invoke();
        if (!isRestoring)
        {
            GameSaveService.SaveArchiveFromRuntime();
        }
        return true;
    }

    public static IReadOnlyList<NPCEventEndingGroup> GetEndingGroups()
    {
        return EndingsByNpcId.Values
            .Where(endings => endings.Count > 0)
            .Select(endings => new NPCEventEndingGroup(endings[0], endings))
            .OrderBy(group => group.NpcId)
            .ToList();
    }

    public static IReadOnlyList<NPCEventEndingRecord> GetEndingRecords()
    {
        return EndingsByNpcId.Values
            .SelectMany(endings => endings)
            .ToList();
    }

    public static void RestoreEndings(IEnumerable<EndingSaveData> endingSaves)
    {
        isRestoring = true;
        EndingsByNpcId.Clear();

        if (endingSaves != null)
        {
            foreach (EndingSaveData endingSave in endingSaves)
            {
                if (endingSave == null ||
                    string.IsNullOrWhiteSpace(endingSave.npcId) ||
                    string.IsNullOrWhiteSpace(endingSave.eventId) ||
                    string.IsNullOrWhiteSpace(endingSave.text))
                {
                    continue;
                }

                NPCDefinition npc = Resources.FindObjectsOfTypeAll<NPCDefinition>()
                    .FirstOrDefault(candidate => candidate != null && candidate.NpcId == endingSave.npcId);

                if (!EndingsByNpcId.TryGetValue(endingSave.npcId, out List<NPCEventEndingRecord> endings))
                {
                    endings = new List<NPCEventEndingRecord>();
                    EndingsByNpcId.Add(endingSave.npcId, endings);
                }

                bool alreadyRegistered = endings.Any(ending =>
                    ending.EventId == endingSave.eventId &&
                    ending.Text == endingSave.text);

                if (!alreadyRegistered)
                {
                    endings.Add(new NPCEventEndingRecord(
                        npc,
                        endingSave.npcId,
                        npc == null ? endingSave.npcName : npc.DisplayName,
                        npc == null ? null : npc.Avatar,
                        endingSave.eventId,
                        endingSave.title,
                        endingSave.text));
                }
            }
        }

        isRestoring = false;
        Changed?.Invoke();
    }

    public static void ResetRuntimeState()
    {
        EndingsByNpcId.Clear();
        Changed?.Invoke();
    }

    private static bool IsEndingEvent(string eventId)
    {
        return !string.IsNullOrWhiteSpace(eventId) &&
            eventId.StartsWith("E", StringComparison.OrdinalIgnoreCase);
    }
}

public readonly struct NPCEventEndingRecord
{
    public NPCEventEndingRecord(
        NPCDefinition npc,
        string npcId,
        string npcName,
        Sprite avatar,
        string eventId,
        string title,
        string text)
    {
        Npc = npc;
        NpcId = npcId ?? string.Empty;
        NpcName = npcName ?? string.Empty;
        Avatar = avatar;
        EventId = eventId ?? string.Empty;
        Title = title ?? string.Empty;
        Text = text ?? string.Empty;
    }

    public readonly NPCDefinition Npc;
    public readonly string NpcId;
    public readonly string NpcName;
    public readonly Sprite Avatar;
    public readonly string EventId;
    public readonly string Title;
    public readonly string Text;
}

public readonly struct NPCEventEndingGroup
{
    public NPCEventEndingGroup(NPCEventEndingRecord firstRecord, IReadOnlyList<NPCEventEndingRecord> endings)
    {
        Npc = firstRecord.Npc;
        npcId = firstRecord.NpcId;
        npcName = firstRecord.NpcName;
        avatar = firstRecord.Avatar;
        Endings = endings ?? Array.Empty<NPCEventEndingRecord>();
    }

    private readonly string npcId;
    private readonly string npcName;
    private readonly Sprite avatar;

    public NPCDefinition Npc { get; }
    public IReadOnlyList<NPCEventEndingRecord> Endings { get; }
    public string NpcId => Npc == null ? npcId : Npc.NpcId;
    public string NpcName => Npc == null ? npcName : Npc.DisplayName;
    public Sprite Avatar => Npc == null ? avatar : Npc.Avatar;
}
