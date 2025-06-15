using UnityEngine;
using UnityEngine.UI;

public class ButtonAssigner : MonoBehaviour
{
    public Button upButton;
    public Button downButton;
    public Button leftButton;
    public Button rightButton;
    public Button undoButton;
    public GameObject snakeHead;

    void Start()
    {

        SnakeController snakeController = snakeHead.GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("Không tìm thấy SnakeController trên snakeHead!");
            return;
        }


        snakeController.AssignButtons(upButton, downButton, leftButton, rightButton, undoButton);
    }
}
