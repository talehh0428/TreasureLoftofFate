# CHANGELOG

## 2026-06-11

### 存档系统

- **Save**: 新增 WebGL 方向的 JSON 存档根对象和 `GameSaveService`，使用 `PlayerPrefs` 持久化长期档与 3 个流程槽。
- **MainScene**: 新游戏流程不再清空图鉴和人物结局；读档路径通过 `GameStartContext` 跳过新游戏重置。
- **Round Flow**: 回合结束处理增益升级和事件调度后，显示 3 槽存档选择，再进入下一回合开始的坊市。
- **Runtime State**: 增加仓库、NPCDefinition、NPCEventScheduler、EconomyBuffSystem 的快照与恢复接口。
- **Archive**: 物品图鉴和人物结局解锁会写入长期档，流程槽覆盖/删除不影响长期档。
