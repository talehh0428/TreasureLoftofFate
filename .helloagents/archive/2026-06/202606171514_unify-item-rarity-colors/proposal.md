# 变更提案: unify item rarity colors

## 元信息
```yaml
类型: 优化
方案类型: implementation
优先级: P2
状态: 已实施
创建: 2026-06-17
```

---

## 1. 需求

### 背景
GuideBook、Warehouse、TradeScene、shichang 中的物品展示项各自维护了一套稀有度颜色，视觉口径不统一。

### 目标
将凡品、良品、上品、极品、仙品五档稀有度的展示颜色统一为用户指定色值。

### 约束条件
```yaml
时间约束: 无
性能约束: 仅调整序列化颜色和默认颜色，不引入运行时额外开销
兼容性约束: 保持现有交互逻辑、prefab 引用和字段名不变
业务约束: 色值严格按用户给定的五档稀有度映射
```

### 验收标准
- [x] 五档稀有度颜色映射为 #D8D2C4、#7E9F6E、#B08A4A、#7A5C8E、#9B4E3F
- [x] GuideBook、Warehouse、TradeScene、shichang 相关 item 展示脚本默认值已同步
- [x] 相关 prefab 的已序列化颜色字段已同步，避免 Unity 资源覆盖脚本默认值

---

## 2. 方案

### 技术方案
更新 item 展示 UI 脚本中的稀有度颜色默认值，并同步修改相关 prefab 上已序列化的稀有度颜色字段。四个场景本身没有检测到这些字段的场景级覆盖，因此不修改场景文件。

### 影响范围
```yaml
涉及模块:
  - GuideBook: GuideBookItemEntryUI 稀有度图标框颜色
  - Warehouse: WarehouseItemSlotUI 稀有度背景颜色
  - Trade: TradeInventorySlotUI / TradeOfferSlotUI 稀有度背景颜色
  - ShiChang: ShopItemSlotUI 稀有度边框颜色
预计变更文件: 10
```

### 风险评估
| 风险 | 等级 | 应对 |
|------|------|------|
| Unity 已有 prefab 序列化值覆盖脚本默认值 | 中 | 同步更新脚本默认值与 prefab 序列化字段 |
| shichang 当前稀有度色用于边框而非主背景 | 低 | 保持现有展示逻辑，仅统一稀有度视觉颜色 |

---

## 4. 核心场景

### 场景: 物品稀有度颜色展示
**模块**: GuideBook / Warehouse / Trade / ShiChang
**条件**: 物品展示项根据 ShopItemRarity 刷新视觉
**行为**: 读取对应稀有度颜色字段
**结果**: 各场景中同一稀有度使用一致色值

## 5. 技术决策

### unify item rarity colors#D001: 同步脚本默认值与 prefab 序列化值
**日期**: 2026-06-17
**状态**: ✅采纳
**背景**: Unity 已创建的 prefab 会保留序列化字段值，单独修改 C# 字段默认值无法保证现有资源生效。
**选项分析**:
| 选项 | 优点 | 缺点 |
|------|------|------|
| A: 只改脚本默认值 | 改动少 | 现有 prefab 可能继续使用旧色 |
| B: 同步脚本默认值和 prefab 序列化值 | 新实例和现有资源都一致 | 需要多文件同步 |
**决策**: 选择方案 B
**理由**: 保证四个场景实际引用的 prefab 展示结果立即统一。
**影响**: 相关 item UI 脚本与 prefab 颜色字段。
