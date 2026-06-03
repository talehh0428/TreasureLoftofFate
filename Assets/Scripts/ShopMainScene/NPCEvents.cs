using System;

public static class NPCEvents
{
    public static event Action<NPCDefinition> NPCSelected;
    public static event Action NPCSelectionCleared;

    public static void RaiseNPCSelected(NPCDefinition npc)
    {
        NPCSelected?.Invoke(npc);
    }

    public static void RaiseNPCSelectionCleared()
    {
        NPCSelectionCleared?.Invoke();
    }
}
