using System;
using System.Collections.Generic;

[Serializable]
public class NPCInitialEventStateConfig
{
    public List<NPCInitialEventStateEntry> initialStates = new List<NPCInitialEventStateEntry>();
}

[Serializable]
public class NPCInitialEventStateEntry
{
    public string npcId;
    public string currentEventId;
}
