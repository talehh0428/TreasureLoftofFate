using UnityEngine;

public static class GameStartContext
{
    public static RunSaveData PendingRunSave { get; private set; }

    public static bool HasPendingRunSave => PendingRunSave != null;

    public static bool IsLoadingRunSave { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        PendingRunSave = null;
        IsLoadingRunSave = false;
    }

    public static void SetPendingRunSave(RunSaveData runSave)
    {
        PendingRunSave = runSave;
        IsLoadingRunSave = runSave != null;
        Debug.Log($"[GameStartContext] 设置待读流程档 hasData={PendingRunSave != null} round={PendingRunSave?.currentRound ?? 0}");
    }

    public static RunSaveData ConsumePendingRunSave()
    {
        RunSaveData runSave = PendingRunSave;
        PendingRunSave = null;
        Debug.Log($"[GameStartContext] 消费待读流程档 hasData={runSave != null} round={runSave?.currentRound ?? 0}");
        return runSave;
    }

    public static void ClearPendingRunLoad()
    {
        PendingRunSave = null;
        IsLoadingRunSave = false;
        Debug.Log("[GameStartContext] 结束读档流程上下文。");
    }
}
