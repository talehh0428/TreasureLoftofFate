using System;
using UnityEngine;

[Serializable]
public class DialogueBody
{
    public string npcName;
    public Sprite portrait;
    public string portraitAddress;
    [TextArea] public string text;
    public DialogueChoice[] choices;
    public DialogueAdvanceMode advanceMode = DialogueAdvanceMode.Choices;

    public bool HasChoices => choices != null && choices.Length > 0;
}

public enum DialogueAdvanceMode
{
    Choices = 0,
    ScreenClick = 1,
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
