using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    [Header("Danh sách Prefab Mây (kéo vào trong Unity)")]
    public List<GameObject> cloudPrefabs;

    [Header("Khoảng random trục Y")]
    public float minY = -2f;
    public float maxY = 2f;

    [Header("Vị trí sinh mây")]
    public float spawnX = -10f;

    [Header("Tốc độ bay (trục X)")]
    public float speedX = 1f;

    [Header("Khoảng thời gian mây tồn tại (giây)")]
    public float lifeTime = 10f;

    [Header("Tốc độ sinh mây (random giữa 2 giá trị)")]
    public float minSpawnInterval = 1f;
    public float maxSpawnInterval = 3f;

    void Start()
    {
        StartCoroutine(SpawnCloudRoutine());
    }

    IEnumerator SpawnCloudRoutine()
    {
        while (true)
        {
            SpawnCloud();
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void SpawnCloud()
    {
        if (cloudPrefabs.Count == 0) return;

        int index = Random.Range(0, cloudPrefabs.Count);
        GameObject cloudPrefab = cloudPrefabs[index];

        float y = Random.Range(minY, maxY);
        Vector3 spawnPos = new Vector3(spawnX, y, 0f);

        GameObject cloud = Instantiate(cloudPrefab, spawnPos, Quaternion.identity);
        cloud.AddComponent<CloudMover>().Init(speedX, lifeTime);
    }
}

