using UnityEngine;

[CreateAssetMenu(fileName = "NPC", menuName = "TreasureLoftOfFate/NPC")]
public class NPCDefinition : ScriptableObject
{
    [SerializeField] private string npcId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite avatar;
    [SerializeField] [TextArea] private string description;
    [SerializeField] private int attack;
    [SerializeField] private int defense;
    [SerializeField] private int movementSpeed;
    [SerializeField] [TextArea] private string prompt;
    [SerializeField] private string currentEventID;
    [SerializeField] private string nextEventID;

    public string NpcId
    {
        get
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                return name;
            }
            return npcId;
        }
    }

    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return name;
            }
            return displayName;
        }
    }

    public Sprite Avatar => avatar;

    public string Description => description;

    public int Attack => Mathf.Max(0, attack);

    public int Defense => Mathf.Max(0, defense);

    public int MovementSpeed => Mathf.Max(0, movementSpeed);

    public string Prompt => prompt;

    public string CurrentEventID
    {
        get => currentEventID;
        set => currentEventID = value;
    }

    public string NextEventID
    {
        get => nextEventID;
        set => nextEventID = value;
    }

    public void AppendPromptEntry(string entryText)
    {
        if (string.IsNullOrWhiteSpace(entryText))
        {
            return;
        }

        int nextIndex = GetNextPromptEntryIndex();
        string entry = $"{nextIndex}. {entryText.Trim()}";
        prompt = string.IsNullOrWhiteSpace(prompt)
            ? entry
            : $"{prompt.TrimEnd()}\n{entry}";
    }

    public void ClearNextEventID()
    {
        nextEventID = string.Empty;
    }

    private int GetNextPromptEntryIndex()
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return 1;
        }

        string[] lines = prompt.Split('\n');
        int count = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                count++;
            }
        }

        return count + 1;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            npcId = name;
        }

        attack = Mathf.Max(0, attack);
        defense = Mathf.Max(0, defense);
        movementSpeed = Mathf.Max(0, movementSpeed);
    }
}
