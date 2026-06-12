# 任务清单: webgl_save_system

```yaml
@feature: webgl_save_system
@created: 2026-06-11
@status: completed
@mode: R3
```

<!-- LIVE_STATUS_BEGIN -->
状态: completed | 进度: 8/8 (100%) | 更新: 2026-06-11 23:40:00
当前: 已完成编译验证
<!-- LIVE_STATUS_END -->

## 进度概览

| 完成 | 失败 | 跳过 | 总数 |
|------|------|------|------|
| 8 | 0 | 0 | 8 |

---

## 任务列表

### 1. 存档核心

- [√] 1.1 新增存档数据模型 `GameSaveRoot`、`ArchiveSaveData`、`RunSaveData` | depends_on: []
- [√] 1.2 新增 `GameSaveService`，通过 `PlayerPrefs` 保存/读取 JSON 根对象 | depends_on: [1.1]
- [√] 1.3 新增 `GameStartContext`，支持读档加载 `MainScene` 时跳过新游戏重置 | depends_on: [1.2]

### 2. 运行时状态接入

- [√] 2.1 为图鉴和人物结局注册表增加长期档导出/恢复 | depends_on: [1.2]
- [√] 2.2 为钱包、仓库、NPC、回合调度、商店增益增加流程档快照/恢复 | depends_on: [1.2]
- [√] 2.3 修正新游戏初始化，只重置流程状态，不清长期档 | depends_on: [2.1, 2.2]

### 3. 流程与 UI 接入

- [√] 3.1 回合结束后、进入下一回合开始前显示 3 槽存档界面 | depends_on: [2.2]
- [√] 3.2 开始菜单支持读取已有流程槽位并进入下一回合开始状态 | depends_on: [1.3, 2.2]

---

## 执行日志

| 时间 | 任务 | 状态 | 备注 |
|------|------|------|------|
| 2026-06-11 23:20 | 方案设计 | completed | 确定 PlayerPrefs + JSON 根对象方案 |
| 2026-06-11 23:30 | 存档核心 | completed | 新增 `Assets/Scripts/Save` |
| 2026-06-11 23:36 | 流程接入 | completed | 新游戏、回合结束、读档路径已接入 |
| 2026-06-11 23:40 | 编译验证 | completed | `dotnet build --no-restore` 通过 |

---

## 执行备注

- 子代理调用因当前工具要求用户显式授权而降级为主流程执行。
- `SaveSlotPanelController` 提供运行时默认按钮兜底，正式上线前建议在 Unity 场景中制作并绑定存档面板视觉。
