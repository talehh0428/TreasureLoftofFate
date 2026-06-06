# NPC 后端对话连接脚本验收指南

## 目标

`NPCDialogueBackendConnector` 负责把本地 `NPCDefinition` 和本地历史摘要发送给后端 `POST /api/npc/dialogue`，再把后端返回的 NPC 台词和玩家选项交给 `DialogueSceneController` 显示。

脚本不会修改 `NPCDefinition.Prompt`。对话结束后，本地历史摘要会被清空。

## 场景准备

主场景中保留：

```text
DialogueSystem
├─ DialogueSceneController
├─ NPCDialogueBackendConnector
└─ 可选: DialogueSceneTestDriver
```

`Dialogue` 场景中只保留对话 UI：

```text
Canvas_DialogueSystem
├─ CanvasGroup
├─ DialogueBoxController
├─ PortraitArea / PortraitImage
├─ ChoiceGroup / ChoiceButton_1~3
└─ DialoguePanel / NpcNameText / DialogueText
```

确保 `Dialogue` 场景已经加入 `File > Build Settings > Scenes In Build`。

## NPCDefinition 设置

`NPCDefinition` 现在有两个图片字段：

- `Avatar`: NPC 头像，用于列表、卡片等小图。
- `Portrait`: NPC 立绘，用于对话框左侧大图。

`npcId` 使用已配置好的 P 开头 ID，例如 `P01`。

`Prompt` 用作 NPC 角色设定主体。连接脚本会将它放入 `eventSummary`，但不会修改它。

## NPCDialogueBackendConnector 挂载

在主场景 `DialogueSystem` 上添加 `NPCDialogueBackendConnector`，然后设置：

- `Dialogue Controller`: 同物体上的 `DialogueSceneController`
- `Npc`: 目标 `NPCDefinition`
- `Max Rounds`: 最大后端请求轮次，例如 `3`
- `Closing Choice Text`: 达到轮次上限后的关闭选项，默认 `我了解了，先聊到这里吧`
- `Base Url`: 后端根地址，例如 `http://127.0.0.1:3000`
- `Model`: 可空，空则使用服务端默认模型
- `Temperature`: 默认 `0.7`
- `Max Tokens`: 默认 `512`
- `Debug`: 调试时可勾选

`DialogueSceneController` 设置：

- `Dialogue Scene Name`: `Dialogue`
- `Dont Destroy On Load`: 勾选
- `Dialogue Box`: 主场景中可留空，加载 `Dialogue` 场景后会自动查找

## 闭环测试步骤

1. 启动后端服务，确认接口地址可访问。
2. 在 Unity 中运行主场景。
3. 选中 `DialogueSystem`。
4. 在 `NPCDialogueBackendConnector` 组件菜单中执行 `Start Backend Dialogue`。
5. 预期表现：
   - 自动加载 `Dialogue` 场景。
   - 后端返回的 `npcDialogue` 逐字显示。
   - 文字播放完成后显示后端返回的 3 个选项。
   - 点击任意选项后，脚本把 NPC 台词和玩家选择写入本地历史摘要，再请求下一轮。
   - 达到 `Max Rounds` 后，不再显示后端选项，选项变成 `Closing Choice Text`。
   - 点击关闭选项后，清空本地历史摘要，并调用 `DialogueSceneController.UnloadDialogue()` 关闭对话 UI。

## eventSummary 格式

首轮大致格式：

```text
（费仁）
这里是 NPCDefinition.Prompt 的内容
```

后续轮次会追加最近历史：

```text
最近对话摘要：
NPC刚才说“...”。玩家回应“...”。请生成NPC此刻的下一句回应。
```

脚本只保留最近 3 条本地历史用于摘要，避免无限增长。

## 常见问题

### DialogueBoxController 找不到

检查：

- `Dialogue` 场景是否加入 Build Settings。
- `Dialogue Scene Name` 是否等于 `Dialogue`。
- `Dialogue` 场景中的 Canvas 是否挂了 `DialogueBoxController`。

### HTTP 被 Unity 拦截

如果后端是 `http://`，到：

```text
Edit > Project Settings > Player > Other Settings > Allow downloads over HTTP
```

测试阶段可设为 `Allowed in Development Builds` 或 `Always Allowed`。

### 后端返回 502

通常是后端调用模型失败、API Key 错误、模型服务异常，或后端生成失败。看 Unity Console 打印的响应 Body。

### 最后一轮为什么只有一个选项

达到 `Max Rounds` 后，客户端主动结束对话，不再继续请求后端，所以选项会替换为 `Closing Choice Text`。
