using UnityEngine;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public GameObject[] levelPrefabs; // Mảng chứa các prefab level từ Project
    public Vector3[] levelPositions; // Mảng chứa vị trí tương ứng cho từng level
    private GameObject currentLevel; // Tham chiếu đến level hiện tại trong hierarchy
    private int currentLevelIndex = 0;
    private bool isGameOver = false;

    void Start()
    {
        if (levelPrefabs.Length > 0 && levelPositions.Length == levelPrefabs.Length)
        {
            LoadLevel(currentLevelIndex); // Tải Level 1 khi bắt đầu
        }
        else
        {
            Debug.LogError("Số lượng prefab và vị trí không khớp hoặc không có prefab!");
        }
    }

    public void LoadLevel(int index)
    {
        if (index >= 0 && index < levelPrefabs.Length)
        {
            // Xóa level cũ nếu có
            if (currentLevel != null)
            {
                Destroy(currentLevel);
            }
            // Xóa các đoạn thân rắn cũ (tag "SnakeBody")
            GameObject[] snakeBodies = GameObject.FindGameObjectsWithTag("SnakeBody");
            foreach (GameObject body in snakeBodies)
            {
                Destroy(body);
            }
            // Tạo level mới từ prefab với vị trí tương ứng
            currentLevel = Instantiate(levelPrefabs[index], levelPositions[index], Quaternion.identity);
            currentLevelIndex = index;
            isGameOver = false;
        }
        else
        {
            Debug.Log("All levels completed! Restarting from Level 1.");
            LoadLevel(0); // Quay về Level 1 nếu hết
        }
    }

    public void WinLevel()
    {
        if (!isGameOver && currentLevel != null)
        {
            int nextLevelIndex = currentLevelIndex + 1;
            Destroy(currentLevel); // Xóa level hiện tại
            LoadLevel(nextLevelIndex); // Tải level tiếp theo
        }
    }

    public void LoseLevel()
    {
        if (!isGameOver && currentLevel != null)
        {
            isGameOver = true;
            SnakeController snakeController = FindFirstObjectByType<SnakeController>();
            if (snakeController != null && snakeController.fallSound != null)
            {
                AudioSource audioSource = snakeController.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = snakeController.gameObject.AddComponent<AudioSource>();
                }
                audioSource.PlayOneShot(snakeController.fallSound);
                StartCoroutine(WaitForFallSound(snakeController.fallSound.length));
            }
            else
            {
                ReloadCurrentLevel(); // Nếu không có âm thanh, load ngay
            }
            Destroy(currentLevel); // Xóa level hiện tại
        }
    }

    IEnumerator WaitForFallSound(float clipLength)
    {
        yield return new WaitForSeconds(clipLength); // Chờ âm thanh kết thúc
        ReloadCurrentLevel(); // Load lại level sau khi âm thanh phát xong
    }

    void ReloadCurrentLevel()
    {
        LoadLevel(currentLevelIndex); // Tải lại level hiện tại
    }
}