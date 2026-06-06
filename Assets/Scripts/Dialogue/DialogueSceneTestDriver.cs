using UnityEngine;

public class DialogueSceneTestDriver : MonoBehaviour
{
    [SerializeField] private DialogueSceneController dialogueController;
    [SerializeField] private Sprite testPortrait;

    private void Awake()
    {
        if (dialogueController == null)
        {
            dialogueController = FindObjectOfType<DialogueSceneController>(true);
        }
    }

    [ContextMenu("Run Dialogue UI Test")]
    public void RunDialogueUiTest()
    {
        if (dialogueController == null)
        {
            Debug.LogWarning("[DialogueSceneTestDriver] DialogueSceneController is missing.");
            return;
        }

        DialogueBody body = new DialogueBody
        {
            npcName = "费仁",
            portrait = testPortrait,
            text = "老板……我、我想看看护身的东西。城南遗迹那边，总觉得有些不对劲。",
            choices = new[]
            {
                new DialogueChoice { id = "rational", text = "你先说说，你想防什么。" },
                new DialogueChoice { id = "empathy", text = "听起来，你是攒了很久才走进我这家店。" },
                new DialogueChoice { id = "playful", text = "护身的有，护胆的暂时没卖，要不要一起看看？" }
            }
        };

        dialogueController.ShowDialogue(body, HandleFirstChoice);
    }

    private void HandleFirstChoice(DialogueChoiceResult result)
    {
        Debug.Log($"[DialogueSceneTestDriver] Player selected: {result.Id} / {result.Text}");

        DialogueBody nextBody = new DialogueBody
        {
            npcName = "费仁",
            portrait = testPortrait,
            text = "你这么一说，我倒像是能喘口气了。可我还是怕，怕进去了，就再也回不来。",
            choices = new[]
            {
                new DialogueChoice { id = "close", text = "我明白了，先到这里。" }
            }
        };

        dialogueController.ShowDialogue(nextBody, _ => dialogueController.UnloadDialogue());
    }
}
