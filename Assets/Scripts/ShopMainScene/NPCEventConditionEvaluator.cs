using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCEventConditionEvaluator
{
    private readonly IReadOnlyDictionary<string, NPCDefinition> npcById;
    private readonly int round;
    private readonly string inactiveEventId;

    public NPCEventConditionEvaluator(
        IReadOnlyDictionary<string, NPCDefinition> npcById,
        int round,
        string inactiveEventId)
    {
        this.npcById = npcById;
        this.round = round;
        this.inactiveEventId = inactiveEventId;
    }

    public bool AreConditionsMet(List<NPCEventCondition> conditions)
    {
        if (conditions == null || conditions.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < conditions.Count; i++)
        {
            if (!IsConditionMet(conditions[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsConditionMet(NPCEventCondition condition)
    {
        if (condition == null)
        {
            return true;
        }

        if (!TryGetComparableValue(condition, out float leftValue))
        {
            return false;
        }

        return Compare(leftValue, condition.value, condition.op);
    }

    private bool TryGetComparableValue(NPCEventCondition condition, out float value)
    {
        value = 0f;
        string scope = condition.scope?.Trim().ToLowerInvariant();
        string attr = condition.attr?.Trim().ToLowerInvariant();

        if (scope == "world")
        {
            if (attr == "round")
            {
                value = round;
                return true;
            }

            return false;
        }

        if (scope != "npc" ||
            string.IsNullOrWhiteSpace(condition.target) ||
            !npcById.TryGetValue(condition.target, out NPCDefinition npc))
        {
            return false;
        }

        if (npc.CurrentEventID == inactiveEventId)
        {
            return false;
        }

        switch (attr)
        {
            case "attack":
                value = npc.Attack;
                return true;
            case "defense":
                value = npc.Defense;
                return true;
            case "speed":
            case "movementspeed":
            case "movement_speed":
                value = npc.MovementSpeed;
                return true;
            default:
                Debug.LogWarning($"NPCEventConditionEvaluator: 未知 NPC 属性 {condition.attr}");
                return false;
        }
    }

    private static bool Compare(float leftValue, float rightValue, string op)
    {
        return op switch
        {
            ">=" => leftValue >= rightValue,
            ">" => leftValue > rightValue,
            "<=" => leftValue <= rightValue,
            "<" => leftValue < rightValue,
            "==" => Math.Abs(leftValue - rightValue) < 0.0001f,
            "!=" => Math.Abs(leftValue - rightValue) >= 0.0001f,
            _ => false
        };
    }
}
