using UnityEngine;

public class WinCondition : MonoBehaviour
{
    public GameObject winPoint;
    public AudioClip winSound;
    private bool isWinPointActive = false;
    private AudioSource audioSource;

    void Start()
    {
        if (winPoint != null)
        {
            Animator animator = winPoint.GetComponent<Animator>();
            if (animator == null)
            {
                animator = winPoint.AddComponent<Animator>();
            }
            animator.enabled = false;
#if UNITY_EDITOR
            if (animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath("Assets/WinAnimatorController.controller");
                UnityEditor.Animations.AnimatorController controller = (UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;
                UnityEditor.Animations.AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
                UnityEditor.Animations.AnimatorState state = stateMachine.AddState("WinAnimation");
            }
#endif
        }
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void OnEnable()
    {
        SnakeController.OnBananaEaten += CheckWinCondition;
    }

    void OnDisable()
    {
        SnakeController.OnBananaEaten -= CheckWinCondition;
    }

    void CheckWinCondition(int bananaCount)
    {
        if (bananaCount == 0 && !isWinPointActive && winPoint != null)
        {
            isWinPointActive = true;
            Animator animator = winPoint.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.enabled = true;
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (animator.HasState(0, Animator.StringToHash("WinAnimation")))
                {
                    animator.Play("WinAnimation");
                }
                else
                {
                    Debug.LogWarning("State 'WinAnimation' not found in Animator! Please create and assign it in the Animator Controller.");
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("SnakeHead") && isWinPointActive)
        {
            Debug.Log("win");
            if (winSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(winSound);
            }
            DisableMovement();
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                levelManager.WinLevel();
            }
        }
    }

    void DisableMovement()
    {
        SnakeController snakeController = FindFirstObjectByType<SnakeController>();
        if (snakeController != null)
        {
            snakeController.upButton.interactable = false;
            snakeController.downButton.interactable = false;
            snakeController.leftButton.interactable = false;
            snakeController.rightButton.interactable = false;
            snakeController.undoButton.interactable = false;
        }
    }
}
