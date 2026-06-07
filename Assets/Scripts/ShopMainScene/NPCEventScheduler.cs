using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCEventScheduler : MonoBehaviour
{
    [SerializeField] private NPCEventJsonLoader jsonLoader;
    [SerializeField] private List<NPCDefinition> npcs = new();
    [SerializeField] private int currentRound = 1;
    [SerializeField] private string finishedEventID = NPCEventSpecialIds.Finished;
    [SerializeField] private string inactiveEventID = NPCEventSpecialIds.Inactive;

    private NPCEventDatabase database;
    private readonly HashSet<string> triggeredOnceEventIds = new();

    public event Action<int> RoundChanged;

    public int CurrentRound => currentRound;

    public void LoadDatabase()
    {
        if (jsonLoader == null)
        {
            Debug.LogWarning("NPCEventScheduler: 缺少 NPCEventJsonLoader。");
            database = new NPCEventDatabase();
            return;
        }

        database = jsonLoader.Load();
    }

    public void ProcessCurrentRound()
    {
        ProcessTurn(currentRound);
    }

    public void ProcessTurn(int round)
    {
        SetCurrentRound(round);
        database ??= jsonLoader != null ? jsonLoader.Load() : new NPCEventDatabase();

        Dictionary<string, NPCDefinition> npcById = BuildNpcLookup();
        NPCEventConditionEvaluator evaluator = new NPCEventConditionEvaluator(npcById, round, inactiveEventID);
        HashSet<string> occupiedNpcIds = new HashSet<string>();
        Dictionary<string, List<string>> promptTextsByNpcId = new Dictionary<string, List<string>>();

        ProcessWorldEvents(evaluator, promptTextsByNpcId);
        ProcessCommonEvents(evaluator, npcById, occupiedNpcIds, promptTextsByNpcId);
        ProcessPersonalEvents(evaluator, npcById, occupiedNpcIds, promptTextsByNpcId);
        AppendRoundPromptEntries(npcById, promptTextsByNpcId);
        AdvanceNpcEvents();
    }

    private void SetCurrentRound(int round)
    {
        int nextRound = Mathf.Max(1, round);
        if (currentRound == nextRound)
        {
            return;
        }

        currentRound = nextRound;
        RoundChanged?.Invoke(currentRound);
    }

    private Dictionary<string, NPCDefinition> BuildNpcLookup()
    {
        Dictionary<string, NPCDefinition> npcById = new Dictionary<string, NPCDefinition>();

        foreach (NPCDefinition npc in npcs)
        {
            if (npc == null)
            {
                continue;
            }

            if (npcById.ContainsKey(npc.NpcId))
            {
                Debug.LogWarning($"NPCEventScheduler: NPC ID 重复，后者已跳过: {npc.NpcId}");
                continue;
            }

            npcById.Add(npc.NpcId, npc);
        }

        return npcById;
    }

    private void ProcessWorldEvents(
        NPCEventConditionEvaluator evaluator,
        Dictionary<string, List<string>> promptTextsByNpcId)
    {
        foreach (NPCEventConfig eventConfig in database.WorldEvents)
        {
            if (!CanTriggerEvent(eventConfig, evaluator))
            {
                continue;
            }

            NPCEventOutcome outcome = SelectOutcome(eventConfig, evaluator);
            if (outcome == null)
            {
                continue;
            }

            ApplyOutcome(eventConfig, outcome, promptTextsByNpcId);
        }
    }

    private void ProcessCommonEvents(
        NPCEventConditionEvaluator evaluator,
        IReadOnlyDictionary<string, NPCDefinition> npcById,
        HashSet<string> occupiedNpcIds,
        Dictionary<string, List<string>> promptTextsByNpcId)
    {
        foreach (NPCEventConfig eventConfig in database.CommonEvents)
        {
            if (!CanTriggerEvent(eventConfig, evaluator) ||
                !AreRequirementsMet(eventConfig, npcById))
            {
                continue;
            }

            NPCEventOutcome outcome = SelectOutcome(eventConfig, evaluator);
            if (outcome == null)
            {
                continue;
            }

            ApplyOutcome(eventConfig, outcome, promptTextsByNpcId);
            foreach (string participant in eventConfig.participants)
            {
                occupiedNpcIds.Add(participant);
            }
        }
    }

    private void ProcessPersonalEvents(
        NPCEventConditionEvaluator evaluator,
        IReadOnlyDictionary<string, NPCDefinition> npcById,
        HashSet<string> occupiedNpcIds,
        Dictionary<string, List<string>> promptTextsByNpcId)
    {
        foreach (NPCDefinition npc in npcs)
        {
            if (npc == null ||
                occupiedNpcIds.Contains(npc.NpcId) ||
                string.IsNullOrWhiteSpace(npc.CurrentEventID) ||
                IsTerminalPersonalEvent(npc.CurrentEventID))
            {
                continue;
            }

            if (!database.TryGetEvent(npc.CurrentEventID, out NPCEventConfig eventConfig) ||
                eventConfig.type != NPCEventTypes.Personal ||
                !CanTriggerEvent(eventConfig, evaluator))
            {
                continue;
            }

            NPCEventOutcome outcome = SelectOutcome(eventConfig, evaluator);
            if (outcome == null)
            {
                continue;
            }

            ApplyOutcome(eventConfig, outcome, promptTextsByNpcId);
        }
    }

    private bool CanTriggerEvent(NPCEventConfig eventConfig, NPCEventConditionEvaluator evaluator)
    {
        if (eventConfig.once && triggeredOnceEventIds.Contains(eventConfig.id))
        {
            return false;
        }

        return evaluator.AreConditionsMet(eventConfig.trigger?.conditions);
    }

    private bool AreRequirementsMet(
        NPCEventConfig eventConfig,
        IReadOnlyDictionary<string, NPCDefinition> npcById)
    {
        List<NPCEventRequirement> requirements = eventConfig.trigger?.requirements;
        if (requirements == null || requirements.Count == 0)
        {
            return true;
        }

        foreach (NPCEventRequirement requirement in requirements)
        {
            if (requirement == null ||
                string.IsNullOrWhiteSpace(requirement.target) ||
                string.IsNullOrWhiteSpace(requirement.eventId) ||
                !npcById.TryGetValue(requirement.target, out NPCDefinition npc) ||
                npc.CurrentEventID == inactiveEventID ||
                npc.CurrentEventID != requirement.eventId)
            {
                return false;
            }
        }

        return true;
    }

    private static NPCEventOutcome SelectOutcome(NPCEventConfig eventConfig, NPCEventConditionEvaluator evaluator)
    {
        return eventConfig.outcomes
            .Where(outcome => evaluator.AreConditionsMet(outcome.conditions))
            .OrderByDescending(outcome => outcome.priority)
            .FirstOrDefault();
    }

    private void ApplyOutcome(
        NPCEventConfig eventConfig,
        NPCEventOutcome outcome,
        Dictionary<string, List<string>> promptTextsByNpcId)
    {
        if (!string.IsNullOrWhiteSpace(outcome.text))
        {
            foreach (string participantId in eventConfig.participants)
            {
                AddPromptText(promptTextsByNpcId, participantId, outcome.text);
            }
        }

        HashSet<string> explicitNextTargets = new HashSet<string>();

        foreach (NPCEventNext next in outcome.next)
        {
            NPCDefinition npc = npcs.FirstOrDefault(candidate => candidate != null && candidate.NpcId == next.target);
            if (npc != null)
            {
                npc.NextEventID = next.nextId;
                explicitNextTargets.Add(npc.NpcId);
            }
        }

        MarkParticipantsFinishedWhenNoNext(eventConfig, explicitNextTargets);

        if (eventConfig.once)
        {
            triggeredOnceEventIds.Add(eventConfig.id);
        }
    }

    private void MarkParticipantsFinishedWhenNoNext(
        NPCEventConfig eventConfig,
        HashSet<string> explicitNextTargets)
    {
        foreach (string participantId in eventConfig.participants)
        {
            if (explicitNextTargets.Contains(participantId))
            {
                continue;
            }

            NPCDefinition npc = npcs.FirstOrDefault(candidate => candidate != null && candidate.NpcId == participantId);
            if (npc == null || npc.CurrentEventID == inactiveEventID)
            {
                continue;
            }

            npc.NextEventID = finishedEventID;
        }
    }

    private bool IsTerminalPersonalEvent(string eventId)
    {
        return eventId == finishedEventID || eventId == inactiveEventID;
    }

    private static void AddPromptText(Dictionary<string, List<string>> promptTextsByNpcId, string npcId, string text)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return;
        }

        if (!promptTextsByNpcId.TryGetValue(npcId, out List<string> texts))
        {
            texts = new List<string>();
            promptTextsByNpcId.Add(npcId, texts);
        }

        texts.Add(text);
    }

    private static void AppendRoundPromptEntries(
        IReadOnlyDictionary<string, NPCDefinition> npcById,
        Dictionary<string, List<string>> promptTextsByNpcId)
    {
        foreach (KeyValuePair<string, List<string>> pair in promptTextsByNpcId)
        {
            if (!npcById.TryGetValue(pair.Key, out NPCDefinition npc) || pair.Value.Count == 0)
            {
                continue;
            }

            npc.AppendPromptEntry(string.Join("&&", pair.Value));
        }
    }

    private void AdvanceNpcEvents()
    {
        foreach (NPCDefinition npc in npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.NextEventID))
            {
                continue;
            }

            npc.CurrentEventID = npc.NextEventID;
            npc.ClearNextEventID();
        }
    }
}
