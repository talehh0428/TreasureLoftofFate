using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingEntryUI : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text endingText;

    public void Setup(NPCEventEndingGroup group)
    {
        AutoBind();

        if (avatarImage != null)
        {
            avatarImage.sprite = group.Avatar;
            avatarImage.enabled = group.Avatar != null;
        }

        if (npcNameText != null)
        {
            npcNameText.text = group.NpcName;
        }

        if (endingText != null)
        {
            endingText.text = BuildEndingText(group);
        }
    }

    private static string BuildEndingText(NPCEventEndingGroup group)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < group.Endings.Count; i++)
        {
            NPCEventEndingRecord ending = group.Endings[i];
            if (i > 0)
            {
                builder.AppendLine();
            }

            builder.Append(ending.Title);
            builder.Append("——");
            builder.Append(ending.Text);
        }

        return builder.ToString();
    }

    private void AutoBind()
    {
        if (avatarImage == null)
        {
            avatarImage = FindChildComponent<Image>("Avatar", "NpcAvatar", "NPCAvatar");
        }

        if (npcNameText == null)
        {
            npcNameText = FindChildComponent<TMP_Text>("NpcName", "NPCName", "NameText");
        }

        if (endingText == null)
        {
            endingText = FindChildComponent<TMP_Text>("EndingText", "ContentText");
        }
    }

    private T FindChildComponent<T>(params string[] names) where T : Component
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = transform.Find(names[i]);
            if (child == null)
            {
                continue;
            }

            T component = child.GetComponent<T>();
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }
}
