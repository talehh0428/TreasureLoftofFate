using System;
using UnityEngine;

[Serializable]
public class ShopVisitor
{
    [SerializeField] private string runtimeId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite avatar;
    [SerializeField] private NPCDefinition definition;
    [SerializeField] private bool canTalk;
    [SerializeField] private bool canTrade = true;

    public string RuntimeId => runtimeId;
    public string DisplayName => definition != null ? definition.DisplayName : displayName;
    public Sprite Avatar => definition != null ? definition.Avatar : avatar;
    public NPCDefinition Definition => definition;
    public bool IsSpecial => definition != null;
    public bool CanTalk => canTalk && definition != null;
    public bool CanTrade => canTrade;

    public string StateKey
    {
        get
        {
            if (definition != null)
            {
                return definition.NpcId;
            }

            return string.IsNullOrWhiteSpace(runtimeId)
                ? displayName
                : runtimeId;
        }
    }

    public static ShopVisitor FromDefinition(NPCDefinition npc)
    {
        return new ShopVisitor
        {
            runtimeId = npc == null ? string.Empty : npc.NpcId,
            definition = npc,
            canTalk = npc != null,
            canTrade = npc != null
        };
    }

    public static ShopVisitor CreateCommon(string visitorId, string visitorName, Sprite defaultAvatar)
    {
        return new ShopVisitor
        {
            runtimeId = visitorId,
            displayName = string.IsNullOrWhiteSpace(visitorName) ? "来访修士" : visitorName,
            avatar = defaultAvatar,
            definition = null,
            canTalk = false,
            canTrade = true
        };
    }
}
