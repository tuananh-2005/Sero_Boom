using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SnakeController : MonoBehaviour
{
    public GameObject headPrefab;
    public GameObject bodyStraightPrefab;
    public GameObject bodyCornerPrefab;
    public GameObject tailPrefab;

    [HideInInspector] public Button upButton, downButton, leftButton, rightButton, undoButton;

    public AudioClip moveSound;
    public AudioClip eatSound;
    public AudioClip fallSound;
    private AudioSource audioSource;

    [SerializeField] private int initialBodySegments = 2;
    private Vector2 direction = Vector2.down;
    private List<Transform> segments = new List<Transform>();
    private List<Vector2> positions = new List<Vector2>();
    private List<Vector2> directions = new List<Vector2>();
    private List<(List<Vector2> positions, List<Vector2> directions, Dictionary<GameObject, Vector2> pushBlockPositions, Dictionary<GameObject, Vector2> bananaPositions)> history = new List<(List<Vector2>, List<Vector2>, Dictionary<GameObject, Vector2>, Dictionary<GameObject, Vector2>)>();
    private float moveDistance = 2f;
    private bool canMove = true;
    private bool isMapReady = false;

    private List<GameObject> pushBlocks = new List<GameObject>();
    private List<GameObject> bananas = new List<GameObject>();

    public delegate void BananaEatenHandler(int bananaCount);
    public static event BananaEatenHandler OnBananaEaten;

    void Start()
    {
        segments.Add(transform);
        positions.Add(transform.position);
        directions.Add(direction);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        for (int i = 0; i < initialBodySegments; i++)
            AddSegment();
        AddTail();

        pushBlocks.AddRange(GameObject.FindGameObjectsWithTag("PushBlock"));
        bananas.AddRange(GameObject.FindGameObjectsWithTag("Banana"));

        SaveState();
        CheckMapReadiness();
    }

    void CheckMapReadiness()
    {
        MapSpawner mapSpawner = FindFirstObjectByType<MapSpawner>();
        if (mapSpawner != null)
        {
            StartCoroutine(WaitForMapReady());
        }
        else
        {
            isMapReady = true;
        }
    }

    System.Collections.IEnumerator WaitForMapReady()
    {
        while (true)
        {
            MapSpawner mapSpawner = FindFirstObjectByType<MapSpawner>();
            if (mapSpawner != null && mapSpawner.IsMapReady())
            {
                isMapReady = true;
                break;
            }
            yield return null;
        }
    }

    public void AssignButtons(Button up, Button down, Button left, Button right, Button undo)
    {
        upButton = up;
        downButton = down;
        leftButton = left;
        rightButton = right;
        undoButton = undo;

        upButton.onClick.AddListener(() => Move(Vector2.up));
        downButton.onClick.AddListener(() => Move(Vector2.down));
        leftButton.onClick.AddListener(() => Move(Vector2.left));
        rightButton.onClick.AddListener(() => Move(Vector2.right));
        undoButton.onClick.AddListener(Undo);
    }

    void Update()
    {
        if (isMapReady)
        {
            CheckFallCondition();
        }
    }

    void CheckFallCondition()
    {
        bool isTouchingGround = false;
        foreach (var segment in segments)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(segment.position, 0.1f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Ground"))
                {
                    isTouchingGround = true;
                    break;
                }
            }
            if (isTouchingGround) break;
        }

        if (!isTouchingGround)
        {
            Debug.Log("rơi");
            if (fallSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(fallSound);
            }
            DisableMovement();
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                levelManager.LoseLevel();
            }
        }
    }

    void DisableMovement()
    {
        upButton.interactable = false;
        downButton.interactable = false;
        leftButton.interactable = false;
        rightButton.interactable = false;
        undoButton.interactable = false;
    }

    void Move(Vector2 newDirection)
    {
        if (!canMove || (newDirection == -direction && segments.Count > 1) || !isMapReady) return;

        Vector2 newPos = (Vector2)transform.position + newDirection * moveDistance;

        if (positions.Contains(newPos))
        {
            Debug.Log("Không thể di chuyển: Va chạm với thân!");
            canMove = true;
            return;
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(newPos, new Vector2(1.8f, 1.8f), 0);
        Collider2D pushBlock = null;
        Collider2D banana = null;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Block"))
            {
                Debug.Log("Không thể di chuyển: Va chạm với khối tĩnh!");
                canMove = true;
                return;
            }
            else if (hit.CompareTag("PushBlock"))
            {
                pushBlock = hit;
            }
            else if (hit.CompareTag("Banana"))
            {
                banana = hit;
            }
        }

        if (pushBlock != null)
        {
            Vector2 pushPos = newPos + newDirection * moveDistance;
            if (positions.Contains(pushPos))
            {
                Debug.Log("Không thể đẩy: Vị trí sau khối có thân rắn!");
                canMove = true;
                return;
            }

            Collider2D[] pushHits = Physics2D.OverlapBoxAll(pushPos, new Vector2(1.8f, 1.8f), 0);
            foreach (var hit in pushHits)
            {
                if (hit.CompareTag("Block") || hit.CompareTag("SnakeBody"))
                {
                    Debug.Log("Không thể đẩy: Vị trí sau khối bị chặn bởi Block hoặc SnakeBody!");
                    canMove = true;
                    return;
                }
            }

            pushBlock.transform.position = new Vector3(pushPos.x, pushPos.y, pushBlock.transform.position.z);
        }
        else if (banana != null)
        {
            Vector2 pushPos = newPos + newDirection * moveDistance;
            bool isBlocked = false;

            if (positions.Contains(pushPos))
            {
                isBlocked = true;
            }
            else
            {
                Collider2D[] pushHits = Physics2D.OverlapBoxAll(pushPos, new Vector2(1.8f, 1.8f), 0);
                foreach (var hit in pushHits)
                {
                    if (hit.CompareTag("Block") || hit.CompareTag("SnakeBody"))
                    {
                        isBlocked = true;
                        break;
                    }
                }
            }

            if (isBlocked)
            {
                if (eatSound != null)
                    audioSource.PlayOneShot(eatSound);
                bananas.Remove(banana.gameObject);
                Destroy(banana.gameObject);
                AddSegment();
                OnBananaEaten?.Invoke(bananas.Count);
            }
            else
            {
                banana.transform.position = new Vector3(pushPos.x, pushPos.y, banana.transform.position.z);
            }
        }

        canMove = false;
        SaveState();

        if (moveSound != null)
            audioSource.PlayOneShot(moveSound);

        direction = newDirection;
        UpdateHeadRotation();
        transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);

        positions.Insert(0, (Vector2)transform.position);
        directions.Insert(0, direction);

        if (positions.Count > segments.Count)
        {
            positions.RemoveAt(positions.Count - 1);
            directions.RemoveAt(directions.Count - 1);
        }

        UpdateBody();
        canMove = true;
    }

    void SaveState()
    {
        Dictionary<GameObject, Vector2> pushBlockPositions = new Dictionary<GameObject, Vector2>();
        foreach (var block in pushBlocks)
        {
            if (block != null)
                pushBlockPositions[block] = (Vector2)block.transform.position;
        }

        Dictionary<GameObject, Vector2> bananaPositions = new Dictionary<GameObject, Vector2>();
        foreach (var banana in bananas)
        {
            if (banana != null)
                bananaPositions[banana] = (Vector2)banana.transform.position;
        }

        history.Add((new List<Vector2>(positions), new List<Vector2>(directions), pushBlockPositions, bananaPositions));
    }

    void Undo()
    {
        if (!canMove || history.Count <= 1 || !isMapReady) return;

        canMove = false;
        history.RemoveAt(history.Count - 1);

        var (prevPositions, prevDirections, prevPushBlockPositions, prevBananaPositions) = history[history.Count - 1];
        positions = new List<Vector2>(prevPositions);
        directions = new List<Vector2>(prevDirections);

        transform.position = positions[0];
        direction = directions[0];
        UpdateHeadRotation();
        UpdateBody();

        foreach (var block in prevPushBlockPositions)
        {
            if (block.Key != null)
                block.Key.transform.position = new Vector3(block.Value.x, block.Value.y, block.Key.transform.position.z);
        }

        bananas.Clear();
        bananas.AddRange(GameObject.FindGameObjectsWithTag("Banana"));

        canMove = true;
    }

    void UpdateHeadRotation()
    {
        float z = direction == Vector2.down ? 0 :
                  direction == Vector2.right ? 90 :
                  direction == Vector2.left ? -90 : 180;
        transform.rotation = Quaternion.Euler(0, 0, z);
    }

    void UpdateBody()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            segments[i].position = positions[i];

            Vector2 prevDir = directions[i - 1];
            Vector2 currDir = directions[i];
            bool isCorner = Vector2.Dot(prevDir, currDir) == 0;

            if (i == segments.Count - 1 && i > 1) // Đuôi
            {
                if (i >= 2 && Vector2.Dot(directions[i - 2], directions[i - 1]) == 0)
                {
                    UpdateCornerRotation(segments[i], directions[i - 2], directions[i - 1]);
                }
                else
                {
                    float z = currDir == Vector2.down ? 90 :
                              currDir == Vector2.right ? 180 :
                              currDir == Vector2.left ? 0 : -90;
                    segments[i].rotation = Quaternion.Euler(0, 0, z);
                }
            }
            else if (isCorner) // Thân gập
            {
                ReplaceSegment(i, bodyCornerPrefab);
                UpdateCornerRotation(segments[i], prevDir, currDir);
            }
            else // Thân thẳng
            {
                ReplaceSegment(i, bodyStraightPrefab);
                float z = currDir == Vector2.down ? 0 :
                          currDir == Vector2.right ? 90 :
                          currDir == Vector2.left ? -90 : 180;
                segments[i].rotation = Quaternion.Euler(0, 0, z);
            }
        }
    }

    void UpdateCornerRotation(Transform segment, Vector2 prevDir, Vector2 currDir)
    {
        float z = (prevDir == Vector2.up && currDir == Vector2.right) || (prevDir == Vector2.left && currDir == Vector2.down) ? 90 :
                  (prevDir == Vector2.up && currDir == Vector2.left) || (prevDir == Vector2.right && currDir == Vector2.down) ? 0 :
                  (prevDir == Vector2.down && currDir == Vector2.right) || (prevDir == Vector2.left && currDir == Vector2.up) ? 180 : -90;
        segment.rotation = Quaternion.Euler(0, 0, z);
    }

    void ReplaceSegment(int index, GameObject prefab)
    {
        Vector3 pos = segments[index].position;
        Quaternion rot = segments[index].rotation;
        Destroy(segments[index].gameObject);
        GameObject newSegment = Instantiate(prefab, pos, rot);
        newSegment.tag = "SnakeBody";
        segments[index] = newSegment.transform;
        Rigidbody2D rb = newSegment.GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 0;
    }

    void AddSegment()
    {
        Vector2 lastPos = segments[segments.Count - 1].position;
        Vector2 newPos = lastPos - directions[directions.Count - 1] * moveDistance;
        GameObject newSegment = Instantiate(bodyStraightPrefab, newPos, Quaternion.identity);
        newSegment.tag = "SnakeBody";
        Rigidbody2D rb = newSegment.GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 0;
        segments.Add(newSegment.transform);
        positions.Add(newPos);
        directions.Add(directions[directions.Count - 1]);
    }

    void AddTail()
    {
        Vector2 lastPos = segments[segments.Count - 1].position;
        Vector2 newPos = lastPos - directions[directions.Count - 1] * moveDistance;
        GameObject newTail = Instantiate(tailPrefab, newPos, Quaternion.identity);
        newTail.tag = "SnakeBody";
        Rigidbody2D rb = newTail.GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 0;
        segments.Add(newTail.transform);
        positions.Add(newPos);
        directions.Add(directions[directions.Count - 1]);
    }
}