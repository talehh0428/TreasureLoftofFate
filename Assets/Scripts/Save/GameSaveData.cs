using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveRoot
{
    public int version = 1;
    public ArchiveSaveData archive = new ArchiveSaveData();
    public List<RunSaveSlotData> runSlots = new List<RunSaveSlotData>();
}

[Serializable]
public class ArchiveSaveData
{
    public List<string> unlockedItemIds = new List<string>();
    public List<EndingSaveData> unlockedEndings = new List<EndingSaveData>();
}

[Serializable]
public class RunSaveSlotData
{
    public int slotIndex;
    public bool hasData;
    public string savedAt;
    public RunSaveData run;
}

[Serializable]
public class RunSaveData
{
    public string resumeSceneName = "ShopMainScene";
    public int currentRound = 1;
    public int money;
    public EconomyBuffSaveData economyBuff = new EconomyBuffSaveData();
    public List<string> pendingSpecialVisitorNpcIds = new List<string>();
    public List<NpcSaveData> npcStates = new List<NpcSaveData>();
    public List<WarehouseItemSaveData> warehouseItems = new List<WarehouseItemSaveData>();
}

[Serializable]
public class EconomyBuffSaveData
{
    public List<int> levels = new List<int>();
    public int nextThresholdIndex;
    public int lastProcessedIncomeRound = -1;
    public int lastTriggeredUpgradeRound = -1;
}

[Serializable]
public class NpcSaveData
{
    public string npcId;
    public string currentEventId;
    public string nextEventId;
    public int attack;
    public int defense;
    public int movementSpeed;
    public string prompt;
}

[Serializable]
public class WarehouseItemSaveData
{
    public string itemId;
    public int quantity;
}

[Serializable]
public class EndingSaveData
{
    public string npcId;
    public string npcName;
    public string eventId;
    public string title;
    public string text;
}
