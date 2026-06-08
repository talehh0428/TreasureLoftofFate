public static class TradeSceneContext
{
    public static ShopVisitor CurrentVisitor { get; private set; }
    public static NPCDefinition CurrentNpc { get; private set; }
    public static MainSceneShopController SourceController { get; private set; }

    public static bool HasContext => CurrentVisitor != null || CurrentNpc != null;

    public static void Set(NPCDefinition npc, MainSceneShopController sourceController)
    {
        CurrentVisitor = ShopVisitor.FromDefinition(npc);
        CurrentNpc = npc;
        SourceController = sourceController;
    }

    public static void Set(ShopVisitor visitor, MainSceneShopController sourceController)
    {
        CurrentVisitor = visitor;
        CurrentNpc = visitor?.Definition;
        SourceController = sourceController;
    }

    public static void Clear()
    {
        CurrentVisitor = null;
        CurrentNpc = null;
        SourceController = null;
    }
}
