# CHANGELOG

## [0.1.2] - 2026-06-17

### 优化
- **[ItemUI]**: 统一 GuideBook、Warehouse、TradeScene、shichang 物品展示项的五档稀有度视觉颜色 — by beihaihaihai
  - 方案: [202606171514_unify-item-rarity-colors](archive/2026-06/202606171514_unify-item-rarity-colors/)
  - 决策: unify item rarity colors#D001(同步脚本默认值与 prefab 序列化值)
- **[EditorImport]**: 调整商品 JSON 导入工具，支持从 `修仙作品物品数据.json` 导入并按 itemID 匹配图标 — by beihaihaihai
  - 方案: [202606171542_import-xianxia-shop-items-json](archive/2026-06/202606171542_import-xianxia-shop-items-json/)
  - 决策: import xianxia shop items json#D001(字段规范化后复用 JsonUtility)

### 快速修改
- **[GuideBook]**: 图鉴物品列表改为运行时按 `Resources/ShopItem` 加载，不再依赖场景序列化的 `itemDefinitions` 缓存列表 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/GuideBook/GuideBookController.cs:141
- **[ItemUI]**: 交换物品展示项上品与极品稀有度颜色，上品改为藏经紫、极品改为金砂褐 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/GuideBook/GuideBookItemEntryUI.cs:29
- **[Dialogue]**: JSON 剧情播放时复用 `CloseEndingButton` 作为跳过按钮，序章和终章跳过后走原剧情完成收尾流程 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/Dialogue/DialogueJsonStoryPlayer.cs:109
- **[Dialogue]**: JSON 剧情推进改为点击屏幕，隐藏原本用于 NPC 对话选择的选项按钮 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/Dialogue/DialogueBody.cs:12

## [0.1.1] - 2026-06-13

### 快速修改
- **[NPCEventScheduler]**: personal 事件触发时复用 requirements 校验，确保个人事件按 NPC `CurrentEventID` 顺序推进 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/ShopMainScene/NPCEventScheduler.cs:290
- **[ShopItem]**: 新增临时 Editor 导入工具，从 `Assets/Text/商品数据.json` 导入并覆盖/新建 `Assets/Resources/ShopItem` 商品资源 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/Editor/ImportShopItemsFromJson.cs:1
- **[NPCEventScheduler]**: 本轮事件 text 写入 NPC prompt 时改为覆盖旧 prompt，而不是追加历史条目 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/ShopMainScene/NPCEventScheduler.cs:76
- **[MainScene]**: 新游戏序章和读档进入坊市时等待 `shichang` 加载完成后再卸载覆盖场景，避免露出 MainScene 空背景 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/MainScene/MainSceneShopController.cs:274
- **[MainScene]**: 返回主菜单重置流程时同步卸载 `shichang`，避免坊市场景残留 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/MainScene/MainSceneShopController.cs:211
- **[MainScene]**: 开始游戏进入序章时等待 Dialogue 首帧显示后再隐藏 StartMenu，避免点击开始后的空帧闪屏 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/MainScene/MainSceneShopController.cs:249

## 2026-06-11

### 存档系统

- **Save**: 新增 WebGL 方向的 JSON 存档根对象和 `GameSaveService`，使用 `PlayerPrefs` 持久化长期档与 3 个流程槽。
- **MainScene**: 新游戏流程不再清空图鉴和人物结局；读档路径通过 `GameStartContext` 跳过新游戏重置。
- **Round Flow**: 回合结束处理增益升级和事件调度后，显示 3 槽存档选择，再进入下一回合开始的坊市。
- **Runtime State**: 增加仓库、NPCDefinition、NPCEventScheduler、EconomyBuffSystem 的快照与恢复接口。
- **Archive**: 物品图鉴和人物结局解锁会写入长期档，流程槽覆盖/删除不影响长期档。
