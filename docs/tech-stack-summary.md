# 《仙阁：掌柜改命录》技术栈概览

> 用途：PPT 技术介绍素材。内容基于当前 Unity 项目配置、包依赖与 `Assets/Scripts` 代码结构整理。

## 项目定位

- 类型：2D 经营 / 剧情向 Unity 游戏项目
- 项目名：`TreasureLoftofFate`
- 中文名：`《仙阁：掌柜改命录》`
- 核心玩法模块：商店经营、商品交易、仓库管理、NPC 事件、剧情对话、图鉴、存档

## 基础技术栈

| 类别 | 技术 / 工具 | 项目中的用途 |
|---|---|---|
| 游戏引擎 | Unity `2022.3.61f1` LTS | 项目主引擎与场景编辑环境 |
| 开发语言 | C# | 游戏逻辑、UI 控制、数据模型、编辑器工具 |
| C# 版本 | C# 9.0 / .NET Framework 4.7.1 兼容项目 | Unity 生成的脚本工程配置 |
| 渲染管线 | Universal Render Pipeline `14.0.12` | 2D 项目渲染基础，使用 URP 2D 模板与 Renderer2D |
| 目标平台 | WebGL / Standalone | 当前工程定义包含 `UNITY_WEBGL`，PlayerSettings 中配置 WebGL 构建参数 |
| IDE 支持 | Visual Studio / Rider 插件 | Unity C# 工程开发与调试 |

## Unity 包与能力

| Unity Package | 版本 | 作用 |
|---|---:|---|
| Addressables | `1.22.3` | 场景与资源分组、异步加载、按地址管理资源 |
| UGUI | `1.0.0` | 按钮、面板、滚动列表等传统 Unity UI |
| TextMeshPro | `3.0.7` | 游戏文本、按钮文字、详情面板、中文字体显示 |
| Universal RP | `14.0.12` | URP 2D 渲染配置 |
| Timeline | `1.7.7` | 可用于剧情/演出时间轴能力 |
| Test Framework | `1.1.33` | Unity 测试框架基础依赖 |
| Visual Scripting | `1.9.4` | 项目依赖中存在，可支持可视化逻辑扩展 |

## 资源与场景管理

- 场景组织：`Initialization`、`StartMenu`、`MainScene`、`ShopMainScene`、`Dialogue`、`TradeScene`、`Warehouse`、`GuideBook`、`shichang`
- 启动入口：Build Settings 当前启用 `Assets/Scenes/Initialization.unity`
- Addressables 分组：`Group_Scenes`、`Group_Common_UI`、`Group_Common_Fonts`、`Group_BGM`、`Group_Portraits`、`Group_Shop_Items`
- 运行时加载：`AddressableSceneLoader` 封装 Addressables 场景加载与卸载
- 传统资源加载：部分商品、剧情和图鉴数据通过 `Resources.Load` / `Resources.LoadAll` 获取

## 数据驱动设计

- 商品数据：`ShopItemDefinition : ScriptableObject` 管理商品基础属性
- NPC 数据：`NPCDefinition : ScriptableObject` 管理 NPC 展示与交互信息
- 剧情 / 事件数据：`Assets/Text/*.json` 保存商品、NPC 事件、初始状态、章节剧情等内容
- JSON 解析：使用 Unity `JsonUtility` 解析剧情、NPC 事件和编辑器导入数据
- 编辑器工具：`ImportShopItemsFromJson` 支持从 JSON 批量生成 / 更新商品 ScriptableObject

## 主要业务模块

| 模块 | 代表脚本 | 技术职责 |
|---|---|---|
| 主界面与经营 | `MainSceneShopController`、`MainSceneHudController` | 回合、金币、经营状态、主界面 UI |
| 市场 / 商品 | `ShopController`、`ShopGenerationUtility`、`ShopItemSlotUI` | 商品生成、稀有度、折扣、购买交互 |
| 仓库 | `WarehouseInventory`、`WarehouseController` | 玩家物品持有、仓库展示、详情查看 |
| 交易 | `TradeSceneController`、`TradeOfferStack` | NPC 交易、报价栏、库存与交易结果 |
| NPC 事件 | `NPCEventScheduler`、`NPCEventDatabase` | NPC 事件调度、条件判定、结局记录 |
| 对话系统 | `DialogueSceneController`、`DialogueBoxController`、`DialogueJsonStoryPlayer` | 剧情对话、选项、打字机文本、场景叠加加载 |
| 后端对话接入 | `NPCDialogueBackendConnector` | 使用 `UnityWebRequest` 访问外部 NPC 对话接口 |
| 存档 | `GameSaveService`、`GameSaveData` | 多存档位、运行数据、仓库/NPC/结局保存 |
| 图鉴 | `GuideBookController` | 商品图鉴、已解锁内容、详情面板 |
| 音频 | `BgmManager`、`GlobalButtonClickSound` | BGM 播放、全局按钮音效 |

## UI 与交互实现

- UI 框架：Unity UGUI + TextMeshPro
- 交互方式：Button 事件、`IPointerClickHandler`、面板显隐、场景切换
- 文本表现：TMP 文本组件，包含对话打字机、按钮文本、商品属性展示
- 视觉资源：PNG UI 背景、商品图标、NPC 头像、预制体 Prefab
- 音频资源：MP3 BGM 与点击音效，通过 `AudioSource` 播放

## 存档与平台适配

- 存档模型：`GameSaveRoot`、`RunSaveSlotData`、`RunSaveData` 等可序列化数据类
- 存储方式：使用 Unity 持久化路径与 JSON 文件保存游戏进度
- WebGL 适配：项目当前包含 WebGL 构建配置，存档系统有针对 WebGL 的方案历史记录
- 构建参数：WebGL 启用数据缓存、WASM 链接目标、内存增长配置

## 技术亮点

- 资源异步化：用 Addressables 管理多场景与公共资源，降低场景耦合
- 数据驱动内容：商品、NPC 事件、剧情文本以 ScriptableObject + JSON 组合维护
- 模块化脚本结构：按 Dialogue、Save、Trade、Warehouse、GuideBook、ShopMainScene 等目录拆分业务
- WebGL 友好：当前工程目标面向 WebGL，资源加载和存档均考虑浏览器运行环境
- 编辑器辅助生产：提供 JSON 导入工具，减少商品数据手工录入成本

## PPT 建议页结构

- 第 1 页：项目定位与整体架构
- 第 2 页：Unity / C# / URP / WebGL 基础技术栈
- 第 3 页：Addressables 资源与场景加载体系
- 第 4 页：ScriptableObject + JSON 的数据驱动方案
- 第 5 页：核心业务模块分层
- 第 6 页：UI、对话、存档与后端对话接入亮点
