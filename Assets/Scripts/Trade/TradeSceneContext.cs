public static class TradeSceneContext
{
    public static NPCDefinition CurrentNpc { get; private set; }
    public static MainSceneShopController SourceController { get; private set; }

    public static bool HasContext => CurrentNpc != null;

    public static void Set(NPCDefinition npc, MainSceneShopController sourceController)
    {
        CurrentNpc = npc;
        SourceController = sourceController;
    }

    public static void Clear()
    {
        CurrentNpc = null;
        SourceController = null;
    }
}
