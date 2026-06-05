using System;
using System.Collections.Generic;

[Serializable]
public class NPCEventStoryConfig
{
    public string version;
    public List<NPCEventConfig> events = new();
}

[Serializable]
public class NPCEventConfig
{
    public string id;
    public string type;
    public string title;
    public List<string> participants = new();
    public int priority;
    public bool once;
    public NPCEventTrigger trigger = new();
    public List<NPCEventOutcome> outcomes = new();
}

[Serializable]
public class NPCEventTrigger
{
    public List<NPCEventRequirement> requirements = new();
    public List<NPCEventCondition> conditions = new();
}

[Serializable]
public class NPCEventRequirement
{
    public string target;
    public string eventId;
}

[Serializable]
public class NPCEventCondition
{
    public string scope;
    public string target;
    public string attr;
    public string op;
    public float value;
}

[Serializable]
public class NPCEventOutcome
{
    public string id;
    public int priority;
    public List<NPCEventCondition> conditions = new();
    public string text;
    public List<NPCEventNext> next = new();
}

[Serializable]
public class NPCEventNext
{
    public string target;
    public string nextId;
}
