using System;
using UnityEngine;

[Serializable]
public class DialogueBody
{
    public string npcName;
    public Sprite portrait;
    [TextArea] public string text;
    public DialogueChoice[] choices;

    public bool HasChoices => choices != null && choices.Length > 0;
}

[Serializable]
public class DialogueChoice
{
    public string id;
    public string text;
}

public readonly struct DialogueChoiceResult
{
    public DialogueChoiceResult(string id, string text, int index)
    {
        Id = id;
        Text = text;
        Index = index;
    }

    public string Id { get; }

    public string Text { get; }

    public int Index { get; }
}
