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
