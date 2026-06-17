# 变更提案: import xianxia shop items json

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
现有临时导入工具最初面向 `商品数据.json`，字段名为小写 `itemId/displayName`。新数据文件 `修仙作品物品数据.json` 使用 `ItemId/DisplayName` 等大写键，直接导入会导致 JsonUtility 无法匹配字段。

### 目标
让导入工具默认读取 `Assets/Text/修仙作品物品数据.json`，按 JSON 设定写入 `Assets/Resources/ShopItem` 下的 `ShopItemDefinition`，并按 itemID 在 `Assets/Images/ShopItemImage` 查找同名 Sprite 作为 icon。

### 约束条件
```yaml
时间约束: 无
性能约束: Editor 工具一次性导入，使用 AssetDatabase 搜索可接受
兼容性约束: 保持现有菜单入口、资源目录和 ShopItemDefinition 字段不变
业务约束: JSON 字段值为导入事实来源，icon 按 itemID 同名资源匹配
```

### 验收标准
- [x] 默认 JSON 路径指向 `Assets/Text/修仙作品物品数据.json`
- [x] 兼容 `ItemId/DisplayName/Price/Icon/Description/Rarity/Attack/Defense/MovementSpeed` 等大写键
- [x] 按 itemID 在 `Assets/Images/ShopItemImage` 精确查找同名 Sprite
- [x] 缺失图标时记录警告，更新已有资源时保留原 icon 引用

---

## 2. 方案

### 技术方案
在读取 JSON 后先规范化字段名，再交给 Unity `JsonUtility` 解析；导入时通过属性访问规范化后的字段，写入 `ShopItemDefinition` 的 setter。图标加载改为 AssetDatabase 精确匹配文件名，不再只拼接 `.png` 路径。

### 影响范围
```yaml
涉及模块:
  - EditorImport: 修改 ImportShopItemsFromJson 的 JSON 字段兼容和 icon 查找逻辑
预计变更文件: 1
```

### 风险评估
| 风险 | 等级 | 应对 |
|------|------|------|
| 新 JSON 的 itemID 与现有图标文件名不一致 | 中 | 缺图标时警告；更新已有资源时保留原 icon 引用 |
| JsonUtility 不支持大小写字段自动映射 | 中 | 导入前统一 NormalizeJsonKeys |

---

## 4. 核心场景

### 场景: 从修仙作品物品数据导入商品资源
**模块**: EditorImport
**条件**: 执行菜单 `Tools/Temp/Import Shop Items From Json`
**行为**: 读取新 JSON，创建或更新 `ShopItemDefinition`，按 itemID 查找同名 Sprite
**结果**: `Assets/Resources/ShopItem` 中资源字段与 JSON 保持一致

---

## 5. 技术决策

### import xianxia shop items json#D001: 字段规范化后复用 JsonUtility
**日期**: 2026-06-17
**状态**: ✅采纳
**背景**: Unity 自带 `JsonUtility` 要求 JSON key 与字段名一致，新 JSON 使用 PascalCase。
**选项分析**:
| 选项 | 优点 | 缺点 |
|------|------|------|
| A: 引入外部 JSON 库 | 映射灵活 | 增加依赖，超出当前需求 |
| B: 读取后规范化 key 再用 JsonUtility | 改动小，无新依赖 | 需要维护 key 替换列表 |
**决策**: 选择方案 B
**理由**: 当前字段集合固定，轻量兼容最符合临时 Editor 导入工具定位。
**影响**: 仅影响 `ImportShopItemsFromJson`。
