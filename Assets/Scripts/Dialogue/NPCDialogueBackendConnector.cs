using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NPCDialogueBackendConnector : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueSceneController dialogueController;
    [SerializeField] private NPCDefinition npc;
    [SerializeField] [Min(1)] private int maxRounds = 3;
    [SerializeField] private string closingChoiceText = "我了解了，先聊到这里吧";

    [Header("Backend")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:3000";
    [SerializeField] private int timeoutSeconds = 30;

    [Header("Generation")]
    [SerializeField] private string model = string.Empty;
    [SerializeField] private float temperature = 0.7f;
    [SerializeField] private int maxTokens = 512;
    [SerializeField] private bool debug;

    private readonly List<DialogueHistoryEntry> history = new List<DialogueHistoryEntry>();
    private int currentRound;
    private bool isRequesting;
    private string lastNpcDialogue = string.Empty;
    private NPCDefinition activeNpc;

    public event Action<NPCDefinition> DialogueCompleted;
    public event Action<NPCDefinition> DialogueFailed;

    private void Awake()
    {
        if (dialogueController == null)
        {
            dialogueController = FindObjectOfType<DialogueSceneController>(true);
        }
    }

    [ContextMenu("Start Backend Dialogue")]
    public void StartBackendDialogue()
    {
        if (isRequesting)
        {
            Debug.LogWarning("[NPCDialogueBackendConnector] Dialogue request is already running.");
            return;
        }

        activeNpc = npc;

        if (!ValidateSetup())
        {
            DialogueFailed?.Invoke(activeNpc);
            activeNpc = null;
            return;
        }

        currentRound = 0;
        lastNpcDialogue = string.Empty;
        history.Clear();
        RequestNextDialogue();
    }

    public void StartBackendDialogue(NPCDefinition targetNpc)
    {
        npc = targetNpc;
        StartBackendDialogue();
    }

    public void EndBackendDialogue()
    {
        NPCDefinition completedNpc = activeNpc != null ? activeNpc : npc;

        isRequesting = false;
        currentRound = 0;
        lastNpcDialogue = string.Empty;
        activeNpc = null;
        history.Clear();

        if (dialogueController != null)
        {
            dialogueController.UnloadDialogue();
        }

        DialogueCompleted?.Invoke(completedNpc);
    }

    private void RequestNextDialogue()
    {
        if (currentRound >= maxRounds)
        {
            ShowClosingDialogue();
            return;
        }

        StartCoroutine(RequestDialogueRoutine());
    }

    private IEnumerator RequestDialogueRoutine()
    {
        isRequesting = true;

        if (dialogueController != null && npc != null)
        {
            dialogueController.ShowLoading(npc.DisplayName, npc.Portrait);
        }

        string url = CombineUrl(baseUrl, "/api/npc/dialogue");
        string json = CreateRequestJson();

        Debug.Log($"[NPCDialogueBackendConnector] POST {url}\n{json}");

        using (UnityWebRequest request = UnityWebRequest.Post(url, json, "application/json"))
        {
            request.timeout = Mathf.Max(1, timeoutSeconds);

            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = request.SendWebRequest();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError(
                    "[NPCDialogueBackendConnector] Unity blocked this request before sending it.\n" +
                    $"Url: {url}\nError: {exception.Message}");
                isRequesting = false;
                DialogueFailed?.Invoke(activeNpc);
                yield break;
            }

            yield return operation;

            string responseText = request.downloadHandler != null
                ? request.downloadHandler.text
                : string.Empty;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"[NPCDialogueBackendConnector] Request failed. " +
                    $"Status: {request.responseCode}, Error: {request.error}, Body:\n{responseText}");
                isRequesting = false;
                DialogueFailed?.Invoke(activeNpc);
                yield break;
            }

            NPCDialogueResponse response = JsonUtility.FromJson<NPCDialogueResponse>(responseText);
            if (response == null || !response.ok || response.data == null)
            {
                string message = response != null ? response.message : "Invalid response JSON.";
                Debug.LogError($"[NPCDialogueBackendConnector] Backend returned failure: {message}\n{responseText}");
                isRequesting = false;
                DialogueFailed?.Invoke(activeNpc);
                yield break;
            }

            currentRound++;
            ShowBackendDialogue(response.data);
        }

        isRequesting = false;
    }

    private string CreateRequestJson()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('{');
        AppendJsonString(builder, "npcId", npc.NpcId);
        builder.Append(',');
        AppendJsonString(builder, "eventSummary", BuildEventSummary());
        builder.Append(',');
        builder.Append("\"temperature\":");
        builder.Append(temperature.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append("\"max_tokens\":");
        builder.Append(maxTokens);
        builder.Append(',');
        builder.Append("\"debug\":");
        builder.Append(debug ? "true" : "false");

        if (!string.IsNullOrWhiteSpace(model))
        {
            builder.Append(',');
            AppendJsonString(builder, "model", model);
        }

        builder.Append('}');
        return builder.ToString();
    }

    private string BuildEventSummary()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('（');
        builder.Append(npc.DisplayName);
        builder.Append("）");
        builder.AppendLine(npc.Prompt ?? string.Empty);

        if (history.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("最近对话摘要：");

            int startIndex = Mathf.Max(0, history.Count - 3);
            for (int i = startIndex; i < history.Count; i++)
            {
                DialogueHistoryEntry entry = history[i];
                builder.Append("NPC刚才说“");
                builder.Append(entry.npcDialogue);
                builder.Append("”。玩家回应“");
                builder.Append(entry.playerChoice);
                builder.AppendLine("”。请生成NPC此刻的下一句回应。");
            }
        }

        return builder.ToString().Trim();
    }

    private void ShowBackendDialogue(NPCDialogueData data)
    {
        lastNpcDialogue = data.npcDialogue ?? string.Empty;
        bool shouldCloseAfterThisLine = currentRound >= maxRounds;

        DialogueBody body = new DialogueBody
        {
            npcName = npc.DisplayName,
            portrait = npc.Portrait,
            text = lastNpcDialogue,
            choices = shouldCloseAfterThisLine
                ? CreateClosingChoices()
                : ConvertOptions(data.playerOptions)
        };

        dialogueController.ShowDialogue(body, HandlePlayerChoice);
    }

    private DialogueChoice[] ConvertOptions(NPCDialogueOption[] options)
    {
        if (options == null || options.Length == 0)
        {
            return new[]
            {
                new DialogueChoice { id = "close", text = closingChoiceText }
            };
        }

        int count = Mathf.Min(3, options.Length);
        DialogueChoice[] choices = new DialogueChoice[count];
        for (int i = 0; i < count; i++)
        {
            NPCDialogueOption option = options[i];
            choices[i] = new DialogueChoice
            {
                id = option != null ? option.id : string.Empty,
                text = option != null ? option.text : string.Empty
            };
        }

        return choices;
    }

    private void HandlePlayerChoice(DialogueChoiceResult result)
    {
        if (currentRound >= maxRounds || string.Equals(result.Id, "close", StringComparison.OrdinalIgnoreCase))
        {
            EndBackendDialogue();
            return;
        }

        history.Add(new DialogueHistoryEntry(lastNpcDialogue, result.Text));

        RequestNextDialogue();
    }

    private void ShowClosingDialogue()
    {
        DialogueBody body = new DialogueBody
        {
            npcName = npc != null ? npc.DisplayName : string.Empty,
            portrait = npc != null ? npc.Portrait : null,
            text = lastNpcDialogue,
            choices = CreateClosingChoices()
        };

        dialogueController.ShowDialogue(body, _ => EndBackendDialogue());
    }

    private bool ValidateSetup()
    {
        if (dialogueController == null)
        {
            Debug.LogError("[NPCDialogueBackendConnector] DialogueSceneController is missing.");
            return false;
        }

        if (npc == null)
        {
            Debug.LogError("[NPCDialogueBackendConnector] NPCDefinition is missing.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(npc.NpcId))
        {
            Debug.LogError("[NPCDialogueBackendConnector] NPC id is empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Debug.LogError("[NPCDialogueBackendConnector] Base URL is empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(npc.Prompt))
        {
            Debug.LogWarning("[NPCDialogueBackendConnector] NPC prompt is empty. Backend may receive a weak eventSummary.");
        }

        return true;
    }

    private string CombineUrl(string root, string path)
    {
        string safeRoot = string.IsNullOrWhiteSpace(root)
            ? "http://127.0.0.1:3000"
            : root.TrimEnd('/');

        return safeRoot + path;
    }

    private void AppendJsonString(StringBuilder builder, string key, string value)
    {
        builder.Append('"');
        builder.Append(EscapeJson(key));
        builder.Append("\":\"");
        builder.Append(EscapeJson(value ?? string.Empty));
        builder.Append('"');
    }

    private string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            switch (character)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }

    private DialogueChoice[] CreateClosingChoices()
    {
        return new[]
        {
            new DialogueChoice { id = "close", text = closingChoiceText }
        };
    }
}

public readonly struct DialogueHistoryEntry
{
    public DialogueHistoryEntry(string npcDialogue, string playerChoice)
    {
        this.npcDialogue = npcDialogue;
        this.playerChoice = playerChoice;
    }

    public readonly string npcDialogue;
    public readonly string playerChoice;
}

[Serializable]
public class NPCDialogueResponse
{
    public bool ok;
    public string message;
    public string npcId;
    public NPCDialogueData data;
}

[Serializable]
public class NPCDialogueData
{
    public string npcDialogue;
    public NPCDialogueOption[] playerOptions;
}

[Serializable]
public class NPCDialogueOption
{
    public string id;
    public string text;
}
