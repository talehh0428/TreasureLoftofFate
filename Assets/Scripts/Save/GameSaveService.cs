using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameSaveService
{
    private const string SaveRootKey = "TreasureLoftOfFate.SaveRoot";
    public const int RunSlotCount = 3;

    private static GameSaveRoot cachedRoot;

    public static GameSaveRoot Root
    {
        get
        {
            EnsureLoaded();
            return cachedRoot;
        }
    }

    public static IReadOnlyList<RunSaveSlotData> RunSlots
    {
        get
        {
            EnsureLoaded();
            EnsureRunSlots(cachedRoot);
            return cachedRoot.runSlots;
        }
    }

    public static void LoadArchiveIntoRuntime()
    {
        EnsureLoaded();
        LogPersistentJson("加载长期档");
        ShopItemUnlockRegistry.RestoreUnlockedItems(cachedRoot.archive.unlockedItemIds);
        NPCEventEndingRegistry.RestoreEndings(cachedRoot.archive.unlockedEndings);
    }

    public static void SaveArchiveFromRuntime()
    {
        EnsureLoaded();
        cachedRoot.archive.unlockedItemIds = ShopItemUnlockRegistry.GetExplicitUnlockedItemIds().ToList();
        cachedRoot.archive.unlockedEndings = NPCEventEndingRegistry.GetEndingRecords()
            .Select(record => new EndingSaveData
            {
                npcId = record.NpcId,
                npcName = record.NpcName,
                eventId = record.EventId,
                title = record.Title,
                text = record.Text,
            })
            .Where(record => !string.IsNullOrWhiteSpace(record.npcId) &&
                !string.IsNullOrWhiteSpace(record.eventId) &&
                !string.IsNullOrWhiteSpace(record.text))
            .ToList();
        SaveRoot();
    }

    public static bool SaveRunSlot(int slotIndex, RunSaveData runData)
    {
        if (runData == null || !IsValidSlotIndex(slotIndex))
        {
            return false;
        }

        EnsureLoaded();
        EnsureRunSlots(cachedRoot);

        RunSaveSlotData slot = cachedRoot.runSlots[slotIndex];
        slot.slotIndex = slotIndex;
        slot.hasData = true;
        slot.savedAt = DateTime.UtcNow.ToString("o");
        slot.run = runData;
        SaveRoot();
        Debug.Log($"[GameSaveService] 已保存流程档 slot={slotIndex + 1} round={runData.currentRound} money={runData.money}");
        LogPersistentJson("保存流程档");
        return true;
    }

    public static bool TryGetRunSlot(int slotIndex, out RunSaveSlotData slot)
    {
        EnsureLoaded();
        EnsureRunSlots(cachedRoot);

        if (!IsValidSlotIndex(slotIndex))
        {
            slot = null;
            return false;
        }

        slot = cachedRoot.runSlots[slotIndex];
        return slot != null && slot.hasData && slot.run != null;
    }

    public static void LogRunSlotState(int slotIndex, string action)
    {
        EnsureLoaded();
        EnsureRunSlots(cachedRoot);
        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogWarning($"[GameSaveService] {action}: invalid slot={slotIndex + 1}");
            LogPersistentJson(action);
            return;
        }

        RunSaveSlotData slot = cachedRoot.runSlots[slotIndex];
        bool exists = slot != null && slot.hasData && slot.run != null;
        Debug.Log($"[GameSaveService] {action}: slot={slotIndex + 1} exists={exists}");
        LogPersistentJson(action);
    }

    public static bool DeleteRunSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return false;
        }

        EnsureLoaded();
        EnsureRunSlots(cachedRoot);
        cachedRoot.runSlots[slotIndex] = CreateEmptySlot(slotIndex);
        SaveRoot();
        Debug.Log($"[GameSaveService] 已删除流程档 slot={slotIndex + 1}");
        LogPersistentJson("删除流程档");
        return true;
    }

    public static void ClearAllPersistentData()
    {
        cachedRoot = new GameSaveRoot();
        EnsureRunSlots(cachedRoot);

        PlayerPrefs.DeleteKey(SaveRootKey);
        PlayerPrefs.Save();

        ShopItemUnlockRegistry.ResetRuntimeState();
        NPCEventEndingRegistry.ResetRuntimeState();
        WarehouseInventory.ResetRuntimeState();
        Debug.Log("[GameSaveService] 已清除所有长期存档、人物结局、图鉴记录和流程存档。");
    }

    public static RunSaveData CaptureRun(
        string resumeSceneName,
        NPCEventScheduler scheduler,
        EconomyBuffSystem economyBuffSystem,
        IEnumerable<string> pendingSpecialVisitorNpcIds = null)
    {
        return new RunSaveData
        {
            resumeSceneName = string.IsNullOrWhiteSpace(resumeSceneName) ? "ShopMainScene" : resumeSceneName,
            currentRound = scheduler == null ? 1 : scheduler.CurrentRound,
            money = ShopWallet.CurrentMoney,
            economyBuff = economyBuffSystem == null ? new EconomyBuffSaveData() : economyBuffSystem.CaptureSaveData(),
            pendingSpecialVisitorNpcIds = pendingSpecialVisitorNpcIds == null
                ? new List<string>()
                : pendingSpecialVisitorNpcIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList(),
            npcStates = scheduler == null ? new List<NpcSaveData>() : scheduler.CaptureNpcSaveData().ToList(),
            warehouseItems = WarehouseInventory.CaptureSaveData().ToList(),
        };
    }

    public static void ApplyRun(
        RunSaveData runData,
        NPCEventScheduler scheduler,
        EconomyBuffSystem economyBuffSystem)
    {
        if (runData == null)
        {
            return;
        }

        Debug.Log($"[GameSaveService] 应用流程档 round={runData.currentRound} money={runData.money} npcStates={runData.npcStates?.Count ?? 0} warehouseItems={runData.warehouseItems?.Count ?? 0}");
        LogPersistentJson("应用流程档");

        WarehouseInventory.RestoreSaveData(runData.warehouseItems);
        ShopWallet.SetMoney(runData.money);

        if (economyBuffSystem != null)
        {
            economyBuffSystem.RestoreSaveData(runData.economyBuff);
        }

        if (scheduler != null)
        {
            scheduler.RestoreRunState(runData.currentRound, runData.npcStates);
        }
    }

    public static void ResetRunStateForNewGame(
        int startingMoney,
        NPCEventScheduler scheduler,
        EconomyBuffSystem economyBuffSystem)
    {
        WarehouseInventory.ResetRuntimeState();
        ShopWallet.SetMoney(startingMoney);

        if (economyBuffSystem != null)
        {
            economyBuffSystem.ResetRuntimeState();
        }

        if (scheduler != null)
        {
            scheduler.ResetRuntimeState();
        }
    }

    private static void EnsureLoaded()
    {
        if (cachedRoot != null)
        {
            return;
        }

        string json = PlayerPrefs.GetString(SaveRootKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                cachedRoot = JsonUtility.FromJson<GameSaveRoot>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameSaveService] Failed to parse save data. A new save root will be created. {exception.Message}");
                cachedRoot = null;
            }
        }

        cachedRoot ??= new GameSaveRoot();
        cachedRoot.archive ??= new ArchiveSaveData();
        EnsureRunSlots(cachedRoot);
    }

    private static void SaveRoot()
    {
        EnsureRunSlots(cachedRoot);
        PlayerPrefs.SetString(SaveRootKey, JsonUtility.ToJson(cachedRoot));
        PlayerPrefs.Save();
    }

    private static void LogPersistentJson(string action)
    {
        EnsureLoaded();
        EnsureRunSlots(cachedRoot);
        string json = PlayerPrefs.GetString(SaveRootKey, string.Empty);
        string slotState = string.Join(", ", cachedRoot.runSlots.Select(slot =>
            $"slot{slot.slotIndex + 1}={(slot.hasData && slot.run != null ? "exists" : "empty")}"));
        Debug.Log($"[GameSaveService] {action}: {slotState}");
        Debug.Log($"[GameSaveService] Persistent JSON ({SaveRootKey}): {json}");
    }

    private static void EnsureRunSlots(GameSaveRoot root)
    {
        root.runSlots ??= new List<RunSaveSlotData>();

        while (root.runSlots.Count < RunSlotCount)
        {
            root.runSlots.Add(CreateEmptySlot(root.runSlots.Count));
        }

        for (int index = 0; index < RunSlotCount; index++)
        {
            root.runSlots[index] ??= CreateEmptySlot(index);
            root.runSlots[index].slotIndex = index;
        }

        if (root.runSlots.Count > RunSlotCount)
        {
            root.runSlots.RemoveRange(RunSlotCount, root.runSlots.Count - RunSlotCount);
        }
    }

    private static RunSaveSlotData CreateEmptySlot(int slotIndex)
    {
        return new RunSaveSlotData
        {
            slotIndex = slotIndex,
            hasData = false,
            savedAt = string.Empty,
            run = null,
        };
    }

    private static bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < RunSlotCount;
    }
}
