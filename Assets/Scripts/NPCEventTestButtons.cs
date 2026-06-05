using UnityEngine;

public class NPCEventTestButtons : MonoBehaviour
{
    [SerializeField] private NPCEventScheduler scheduler;
    [SerializeField] private int round = 1;

    public void RunRound()
    {
        scheduler.ProcessTurn(round);
    }

    public void RunNextRound()
    {
        round++;
        scheduler.ProcessTurn(round);
    }
}