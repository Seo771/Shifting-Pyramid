using UnityEngine;

public class SphinxInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private SphinxRoomController roomController;

    public void Interact()
    {
        roomController.StartQuiz();
    }
}
