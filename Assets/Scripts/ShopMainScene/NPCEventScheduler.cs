using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCEventScheduler : MonoBehaviour
{
    [SerializeField] private NPCEventJsonLoader jsonLoader;
    [SerializeField] private TextAsset initialStateJson;
    [SerializeField] private List<NPCDefinition> npcs = new();
    [SerializeField] private int currentRound = 1;
    [SerializeField] [Min(1)] private int maxRound = 100;
    [SerializeField] private string finishedEventID = NPCEventSpecialIds.Finished;
    [SerializeField] private string inactiveEventID = NPCEventSpecialIds.Inactive;

    private NPCEventDatabase database;
    private readonly HashSet<string> triggeredOnceEventIds = new();
    private bool hasInitializedNpcStates;

    public event Action<int> RoundChanged;
    public event Action<NPCDefinition> NpcEventUpdated;

    public int CurrentRound => currentRound;

    public int MaxRound => maxRound;

    public bool CanAdvanceRound => currentRound < maxRound;

    public bool TryProcessNextRound()
    {
        if (!CanAdvanceRound)
        {
            Debug.Log($"NPCEventScheduler: 已达到最大回合 {maxRound}，后续逻辑待补充。");
            return false;
        }

        ProcessTurn(currentRound + 1);
        return true;
    }

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
        EnsureNpcInitialStatesInitialized();
        Debug.Log($"NPCEventScheduler: 开始处理第 {currentRound} 回合事件调度。");

        Dictionary<string, NPCDefinition> npcById = BuildNpcLookup();
        Dictionary<string, NpcEventStateSnapshot> beforeStates = CaptureNpcEventStates(npcById);
        NPCEventConditionEvaluator evaluator = new NPCEventConditionEvaluator(npcById, round, inactiveEventID);
        HashSet<string> occupiedNpcIds = new HashSet<string>();
        Dictionary<string, List<string>> promptTextsByNpcId = new Dictionary<string, List<string>>();

        ProcessWorldEvents(evaluator, npcById, promptTextsByNpcId);
        ProcessCommonEvents(evaluator, npcById, occupiedNpcIds, promptTextsByNpcId);
        ProcessPersonalEvents(evaluator, npcById, occupiedNpcIds, promptTextsByNpcId);
        AppendRoundPromptEntries(npcById, promptTextsByNpcId);
        AdvanceNpcEvents();
        PublishUpdatedNpcs(npcById, beforeStates);
        Debug.Log($"NPCEventScheduler: 第 {currentRound} 回合事件调度结束。");
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

    private void EnsureNpcInitialStatesInitialized()
    {
        if (hasInitializedNpcStates)
        {
            return;
        }

        hasInitializedNpcStates = true;
        Dictionary<string, NPCDefinition> npcById = BuildNpcLookup();

        if (initialStateJson == null || string.IsNullOrWhiteSpace(initialStateJson.text))
        {
            Debug.LogWarning("NPCEventScheduler: 未配置 NPC 初始事件状态 JSON，保持现有 CurrentEventID。");
            return;
        }

        NPCInitialEventStateConfig config = JsonUtility.FromJson<NPCInitialEventStateConfig>(initialStateJson.text);
        if (config?.initialStates == null)
        {
            Debug.LogWarning("NPCEventScheduler: NPC 初始事件状态 JSON 解析失败或 initialStates 字段为空。");
            return;
        }

        HashSet<string> initializedNpcIds = new HashSet<string>();
        foreach (NPCInitialEventStateEntry entry in config.initialStates)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.npcId))
            {
                Debug.LogWarning("NPCEventScheduler: NPC 初始事件状态 JSON 中存在缺少 npcId 的条目，已跳过。");
                continue;
            }

            if (!npcById.TryGetValue(entry.npcId, out NPCDefinition npc))
            {
                Debug.LogWarning($"NPCEventScheduler: 初始事件状态引用了未在 NPC 列表中找到的 NPC ID: {entry.npcId}");
                continue;
            }

            npc.CurrentEventID = entry.currentEventId;
            npc.ClearNextEventID();
            npc.ClearPrompt();
            initializedNpcIds.Add(npc.NpcId);
            Debug.Log($"NPCEventScheduler: 初始化 NPC 事件状态 -> {npc.NpcId} {npc.DisplayName}: {npc.CurrentEventID}");
        }

        foreach (NPCDefinition npc in npcs)
        {
            if (npc == null || initializedNpcIds.Contains(npc.NpcId))
            {
                continue;
            }

            Debug.LogWarning($"NPCEventScheduler: NPC {npc.NpcId} {npc.DisplayName} 未出现在初始事件状态 JSON 中，保持 CurrentEventID: {npc.CurrentEventID}");
        }
    }

    private void ProcessWorldEvents(
        NPCEventConditionEvaluator evaluator,
        IReadOnlyDictionary<string, NPCDefinition> npcById,
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

            ApplyOutcome(eventConfig, outcome, npcById, promptTextsByNpcId);
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

            ApplyOutcome(eventConfig, outcome, npcById, promptTextsByNpcId);
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

            ApplyOutcome(eventConfig, outcome, npcById, promptTextsByNpcId);
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
                string.IsNullOrWhiteSpace(requirement.eventId))
            {
                return false;
            }

            if (!npcById.TryGetValue(requirement.target, out NPCDefinition npc))
            {
                Debug.LogWarning($"NPCEventScheduler: 事件 {eventConfig.id} 的 requirement 引用了未在 NPC 列表中找到的 NPC ID: {requirement.target}");
                return false;
            }

            if (npc.CurrentEventID == inactiveEventID ||
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
        IReadOnlyDictionary<string, NPCDefinition> npcById,
        Dictionary<string, List<string>> promptTextsByNpcId)
    {
        if (!string.IsNullOrWhiteSpace(outcome.text))
        {
            foreach (string participantId in eventConfig.participants)
            {
                WarnIfNpcMissing(eventConfig.id, participantId, "participants", npcById);
                AddPromptText(promptTextsByNpcId, participantId, outcome.text);
                LogTriggeredEvent(participantId, outcome.text);
            }
        }

        HashSet<string> explicitNextTargets = new HashSet<string>();

        foreach (NPCEventNext next in outcome.next)
        {
            if (!npcById.TryGetValue(next.target, out NPCDefinition npc))
            {
                WarnIfNpcMissing(eventConfig.id, next.target, "outcome.next", npcById);
                continue;
            }

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

    private void WarnIfNpcMissing(
        string eventId,
        string npcId,
        string sourceField,
        IReadOnlyDictionary<string, NPCDefinition> npcById)
    {
        if (string.IsNullOrWhiteSpace(npcId) || npcById.ContainsKey(npcId))
        {
            return;
        }

        Debug.LogWarning($"NPCEventScheduler: 事件 {eventId} 的 {sourceField} 引用了未在 NPC 列表中找到的 NPC ID: {npcId}");
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

            string promptEntry = string.Join("&&", pair.Value);
            Debug.Log($"NPCEventScheduler: Prompt更改 -> {npc.NpcId} {npc.DisplayName}: {promptEntry}");
            npc.AppendPromptEntry(promptEntry);
        }
    }

    private void LogTriggeredEvent(string participantId, string text)
    {
        NPCDefinition npc = npcs.FirstOrDefault(candidate => candidate != null && candidate.NpcId == participantId);
        string npcName = npc == null ? "Unknown" : npc.DisplayName;
        Debug.Log($"NPCEventScheduler: 发生事件 -> {participantId} {npcName}: {text}");
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

    private Dictionary<string, NpcEventStateSnapshot> CaptureNpcEventStates(IReadOnlyDictionary<string, NPCDefinition> npcById)
    {
        Dictionary<string, NpcEventStateSnapshot> states = new Dictionary<string, NpcEventStateSnapshot>();
        foreach (KeyValuePair<string, NPCDefinition> pair in npcById)
        {
            NPCDefinition npc = pair.Value;
            if (npc == null)
            {
                continue;
            }

            states[pair.Key] = new NpcEventStateSnapshot(npc.CurrentEventID, npc.NextEventID);
        }

        return states;
    }

    private void PublishUpdatedNpcs(
        IReadOnlyDictionary<string, NPCDefinition> npcById,
        IReadOnlyDictionary<string, NpcEventStateSnapshot> beforeStates)
    {
        foreach (KeyValuePair<string, NPCDefinition> pair in npcById)
        {
            NPCDefinition npc = pair.Value;
            if (npc == null || string.IsNullOrWhiteSpace(npc.NpcId))
            {
                continue;
            }

            beforeStates.TryGetValue(pair.Key, out NpcEventStateSnapshot before);
            if (before.CurrentEventID == npc.CurrentEventID && before.NextEventID == npc.NextEventID)
            {
                continue;
            }

            if (npc.CurrentEventID == inactiveEventID)
            {
                continue;
            }

            Debug.Log($"NPCEventScheduler: 事件状态更新，加入下回合来访 -> {npc.NpcId} {npc.DisplayName}");
            NpcEventUpdated?.Invoke(npc);
        }
    }

    private readonly struct NpcEventStateSnapshot
    {
        public NpcEventStateSnapshot(string currentEventID, string nextEventID)
        {
            CurrentEventID = currentEventID ?? string.Empty;
            NextEventID = nextEventID ?? string.Empty;
        }

        public readonly string CurrentEventID;
        public readonly string NextEventID;
    }
}
