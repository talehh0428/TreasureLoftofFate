using UnityEngine;

public class NPCEventJsonLoader : MonoBehaviour
{
    [SerializeField] private TextAsset storyJson;

    public NPCEventDatabase Load()
    {
        NPCEventDatabase database = NPCEventDatabase.FromJson(storyJson != null ? storyJson.text : string.Empty);

        foreach (string warning in database.Warnings)
        {
            Debug.LogWarning($"NPCEventJsonLoader: {warning}");
        }

        return database;
    }
}
