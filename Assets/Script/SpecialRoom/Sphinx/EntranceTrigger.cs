using UnityEngine;

public class EntranceTrigger : MonoBehaviour
{
    [SerializeField] private SphinxRoomController roomController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (roomController != null)
            {
                roomController.StartQuiz();
            }
        }
    }
}