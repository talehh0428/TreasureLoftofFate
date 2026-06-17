# 任务清单: unify item rarity colors

> **@status:** completed | 2026-06-17 15:20

```yaml
@feature: unify item rarity colors
@created: 2026-06-17
@status: completed
@mode: R2
```

<!-- LIVE_STATUS_BEGIN -->
状态: completed | 进度: 4/4 (100%) | 更新: 2026-06-17 15:20:00
当前: 稀有度颜色统一完成
<!-- LIVE_STATUS_END -->

## 进度概览

| 完成 | 失败 | 跳过 | 总数 |
|------|------|------|------|
| 4 | 0 | 0 | 4 |

---

## 任务列表

### 1. 上下文与定位

- [√] 1.1 定位 GuideBook、Warehouse、TradeScene、shichang 的 item 展示脚本与 prefab | depends_on: []
- [√] 1.2 确认四个场景没有稀有度颜色字段的场景级覆盖 | depends_on: [1.1]

### 2. 颜色同步

- [√] 2.1 更新 5 个 item UI 脚本中的稀有度颜色默认值 | depends_on: [1.2]
- [√] 2.2 更新 5 个 item prefab 中已序列化的稀有度颜色字段 | depends_on: [2.1]

---

## 执行日志

| 时间 | 任务 | 状态 | 备注 |
|------|------|------|------|
| 2026-06-17 15:20 | 1.1-2.2 | completed | 已统一脚本默认值与 prefab 序列化颜色 |

---

## 执行备注

> 记录执行过程中的重要说明、决策变更、风险提示等

- shichang 的 `ShopItemSlotUI` 当前将稀有度色应用在 `RarityBorder`，不是主背景；本次保持既有交互逻辑，仅统一稀有度视觉色。
