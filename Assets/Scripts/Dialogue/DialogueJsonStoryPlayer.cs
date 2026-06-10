using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DialogueJsonStoryPlayer : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueSceneController dialogueController;
    [SerializeField] private List<NPCDefinition> npcDefinitions = new List<NPCDefinition>();

    [Header("Playback")]
    [SerializeField] private string defaultJsonPath;
    [SerializeField] private string fallbackSpeakerName = "旁白";
    [SerializeField] private string continueChoiceText = "继续";
    [SerializeField] private string finishChoiceText = "结束";
    [SerializeField] private bool unloadDialogueWhenFinished = true;

    private readonly Dictionary<string, NPCDefinition> npcById = new Dictionary<string, NPCDefinition>();
    private StoryDialogueLine[] activeLines;
    private int activeLineIndex;
    private bool isPlaying;

    public event Action StoryCompleted;
    public event Action<string> StoryFailed;

    private void Awake()
    {
        AutoBind();
        BuildNpcLookup();
    }

    [ContextMenu("Play Default JSON Dialogue")]
    public void PlayDefaultJsonDialogue()
    {
        StartDialogueFromJsonPath(defaultJsonPath);
    }

    public void StartDialogueFromJsonPath(string jsonPath)
    {
        if (isPlaying)
        {
            Debug.LogWarning("[DialogueJsonStoryPlayer] A JSON dialogue is already playing.");
            return;
        }

        AutoBind();
        BuildNpcLookup();

        if (dialogueController == null)
        {
            Fail("DialogueSceneController is missing.");
            return;
        }

        if (!TryLoadJson(jsonPath, out string jsonText))
        {
            Fail($"Cannot load dialogue JSON from path: {jsonPath}");
            return;
        }

        StoryDialogueJson story = JsonUtility.FromJson<StoryDialogueJson>(jsonText);
        activeLines = GetLines(story);
        if (activeLines == null || activeLines.Length == 0)
        {
            Fail($"Dialogue JSON has no lines: {jsonPath}");
            return;
        }

        isPlaying = true;
        activeLineIndex = 0;
        ShowCurrentLine();
    }

    public void StopJsonDialogue()
    {
        activeLines = null;
        activeLineIndex = 0;
        isPlaying = false;

        if (dialogueController != null && unloadDialogueWhenFinished)
        {
            dialogueController.UnloadDialogue();
        }
    }

    private void ShowCurrentLine()
    {
        if (!isPlaying || activeLines == null || activeLineIndex >= activeLines.Length)
        {
            CompleteStory();
            return;
        }

        StoryDialogueLine line = activeLines[activeLineIndex];
        NPCDefinition speaker = FindSpeaker(GetLineNpcId(line));
        bool isLastLine = activeLineIndex >= activeLines.Length - 1;
        string choiceText = string.IsNullOrWhiteSpace(line.choiceText)
            ? (isLastLine ? finishChoiceText : continueChoiceText)
            : line.choiceText;

        DialogueBody body = new DialogueBody
        {
            npcName = GetSpeakerName(line, speaker),
            portrait = speaker == null ? null : speaker.Portrait,
            text = GetLineText(line),
            choices = new[]
            {
                new DialogueChoice
                {
                    id = isLastLine ? "finish" : "next",
                    text = choiceText
                }
            }
        };

        dialogueController.ShowDialogue(body, HandleChoiceSelected);
    }

    private void HandleChoiceSelected(DialogueChoiceResult result)
    {
        if (!isPlaying)
        {
            return;
        }

        activeLineIndex++;
        ShowCurrentLine();
    }

    private void CompleteStory()
    {
        activeLines = null;
        activeLineIndex = 0;
        isPlaying = false;

        if (dialogueController != null && unloadDialogueWhenFinished)
        {
            dialogueController.UnloadDialogue();
        }

        StoryCompleted?.Invoke();
    }

    private void BuildNpcLookup()
    {
        npcById.Clear();
        for (int i = 0; i < npcDefinitions.Count; i++)
        {
            NPCDefinition definition = npcDefinitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.NpcId))
            {
                continue;
            }

            if (npcById.ContainsKey(definition.NpcId))
            {
                Debug.LogWarning($"[DialogueJsonStoryPlayer] Duplicate NPC id skipped: {definition.NpcId}");
                continue;
            }

            npcById.Add(definition.NpcId, definition);
        }
    }

    private NPCDefinition FindSpeaker(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return null;
        }

        npcById.TryGetValue(npcId, out NPCDefinition definition);
        return definition;
    }

    private string GetSpeakerName(StoryDialogueLine line, NPCDefinition speaker)
    {
        if (speaker != null)
        {
            return speaker.DisplayName;
        }

        string npcId = GetLineNpcId(line);
        return string.IsNullOrWhiteSpace(npcId) ? fallbackSpeakerName : npcId;
    }

    private static StoryDialogueLine[] GetLines(StoryDialogueJson story)
    {
        if (story == null)
        {
            return null;
        }

        if (story.lines != null && story.lines.Length > 0)
        {
            return story.lines;
        }

        if (story.dialogues != null && story.dialogues.Length > 0)
        {
            return story.dialogues;
        }

        return story.entries;
    }

    private static string GetLineNpcId(StoryDialogueLine line)
    {
        if (line == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(line.npcId))
        {
            return line.npcId;
        }

        if (!string.IsNullOrWhiteSpace(line.npcid))
        {
            return line.npcid;
        }

        return line.npcID ?? string.Empty;
    }

    private static string GetLineText(StoryDialogueLine line)
    {
        if (line == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(line.text))
        {
            return line.text;
        }

        return line.dialogue ?? string.Empty;
    }

    private bool TryLoadJson(string jsonPath, out string jsonText)
    {
        jsonText = string.Empty;
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            return false;
        }

        string normalizedPath = jsonPath.Trim().Replace('\\', '/');
        if (TryReadFile(normalizedPath, out jsonText))
        {
            return true;
        }

        if (!Path.IsPathRooted(normalizedPath))
        {
            string streamingPath = Path.Combine(Application.streamingAssetsPath, normalizedPath);
            if (TryReadFile(streamingPath, out jsonText))
            {
                return true;
            }
        }

        string resourcePath = ToResourcesPath(normalizedPath);
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            return false;
        }

        jsonText = textAsset.text;
        return !string.IsNullOrWhiteSpace(jsonText);
    }

    private static bool TryReadFile(string path, out string text)
    {
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        text = File.ReadAllText(path);
        return !string.IsNullOrWhiteSpace(text);
    }

    private static string ToResourcesPath(string path)
    {
        string resourcePath = Path.ChangeExtension(path.Replace('\\', '/'), null);
        const string resourcesMarker = "/Resources/";
        int resourcesIndex = resourcePath.IndexOf(resourcesMarker, StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex >= 0)
        {
            return resourcePath.Substring(resourcesIndex + resourcesMarker.Length);
        }

        if (resourcePath.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
        {
            return resourcePath.Substring("Resources/".Length);
        }

        if (resourcePath.StartsWith("Assets/Resources/", StringComparison.OrdinalIgnoreCase))
        {
            return resourcePath.Substring("Assets/Resources/".Length);
        }

        return resourcePath;
    }

    private void AutoBind()
    {
        if (dialogueController == null)
        {
            dialogueController = DialogueSceneController.Instance != null
                ? DialogueSceneController.Instance
                : FindObjectOfType<DialogueSceneController>(true);
        }
    }

    private void Fail(string message)
    {
        Debug.LogError($"[DialogueJsonStoryPlayer] {message}");
        isPlaying = false;
        activeLines = null;
        activeLineIndex = 0;
        StoryFailed?.Invoke(message);
    }
}

[Serializable]
public class StoryDialogueJson
{
    public StoryDialogueLine[] lines;
    public StoryDialogueLine[] dialogues;
    public StoryDialogueLine[] entries;
}

[Serializable]
public class StoryDialogueLine
{
    public string npcId;
    public string npcid;
    public string npcID;
    [TextArea] public string text;
    [TextArea] public string dialogue;
    public string choiceText;
}
