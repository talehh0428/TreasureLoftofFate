# CHANGELOG

## [0.1.1] - 2026-06-13

### 快速修改
- **[ShopItem]**: 新增临时 Editor 导入工具，从 `Assets/Text/商品数据.json` 导入并覆盖/新建 `Assets/Resources/ShopItem` 商品资源 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/Editor/ImportShopItemsFromJson.cs:1
- **[NPCEventScheduler]**: 本轮事件 text 写入 NPC prompt 时改为覆盖旧 prompt，而不是追加历史条目 — by beihaihaihai
  - 类型: 快速修改（无方案包）
  - 文件: Assets/Scripts/ShopMainScene/NPCEventScheduler.cs:76

## 2026-06-11

### 存档系统

- **Save**: 新增 WebGL 方向的 JSON 存档根对象和 `GameSaveService`，使用 `PlayerPrefs` 持久化长期档与 3 个流程槽。
- **MainScene**: 新游戏流程不再清空图鉴和人物结局；读档路径通过 `GameStartContext` 跳过新游戏重置。
- **Round Flow**: 回合结束处理增益升级和事件调度后，显示 3 槽存档选择，再进入下一回合开始的坊市。
- **Runtime State**: 增加仓库、NPCDefinition、NPCEventScheduler、EconomyBuffSystem 的快照与恢复接口。
- **Archive**: 物品图鉴和人物结局解锁会写入长期档，流程槽覆盖/删除不影响长期档。
