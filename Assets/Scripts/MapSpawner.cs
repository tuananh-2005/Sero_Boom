using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;

[System.Serializable]
public class Region
{
    public string name;
    public int width;
    public int height;
    public Vector2 offset;
    public List<GameObject> prefabs;
    public int groupIndex;
    public bool isFixedRowBottom = false;
}

[System.Serializable]
public class ManualSpawn
{
    public GameObject prefab;
    public Vector2 position;
    public int groupIndex;
}

[System.Serializable]
public class GroupInfo
{
    public string groupName;
    public float flyInSpeed = 0.1f;
    public AudioClip flyInSound;
}

public class MapSpawner : MonoBehaviour
{
    public List<Region> regions = new();
    public List<ManualSpawn> manualSpawns = new();
    public List<GroupInfo> groupSettings = new();
    public float spacing = 2f;
    public Vector3 flyOutOffset = new Vector3(0, -20f, 0);
    private AudioSource audioSource;
    public float dominoDelay = 0.05f;

    private Dictionary<int, List<GameObject>> groupPrefabs = new();
    private Dictionary<GameObject, Vector3> originalPositions = new();
    private bool isMapReady = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    async void Start()
    {
        await SpawnAllRegions();
        await SpawnManualPoints();
        await FlyInAllGroups();
        isMapReady = true; // Đánh dấu map hoàn thành
    }

    async Task SpawnAllRegions()
    {
        foreach (var region in regions)
        {
            for (int y = 0; y < region.height; y++)
            {
                for (int x = 0; x < region.width; x++)
                {
                    GameObject prefabToUse = region.isFixedRowBottom && y == 0 && region.prefabs.Count > 0
                        ? region.prefabs[x % region.prefabs.Count]
                        : region.prefabs[(x + y) % region.prefabs.Count];

                    Vector3 pos = new Vector3(x * spacing, y * spacing, 0) + (Vector3)region.offset;
                    await SpawnPrefab(prefabToUse, pos, region.groupIndex);
                }
            }
        }
    }

    async Task SpawnManualPoints()
    {
        foreach (var spawn in manualSpawns)
        {
            Vector3 pos = (Vector3)spawn.position;
            await SpawnPrefab(spawn.prefab, pos, spawn.groupIndex);
        }
    }

    async Task SpawnPrefab(GameObject prefab, Vector3 pos, int group)
    {
        var obj = Instantiate(prefab, pos + flyOutOffset, Quaternion.identity, transform);
        originalPositions[obj] = pos;

        if (!groupPrefabs.ContainsKey(group))
            groupPrefabs[group] = new List<GameObject>();
        groupPrefabs[group].Add(obj);

        await Task.Yield();
    }

    async Task FlyInAllGroups()
    {
        for (int group = 0; group < groupSettings.Count; group++)
        {
            var setting = groupSettings[group];
            var prefabList = groupPrefabs.ContainsKey(group) ? groupPrefabs[group] : new List<GameObject>();

            if (setting.flyInSound && audioSource)
            {
                audioSource.clip = setting.flyInSound;
                audioSource.Play();
            }

            List<Task> flyTasks = new();
            foreach (var obj in prefabList)
            {
                if (!originalPositions.ContainsKey(obj)) continue;

                var tween = obj.transform.DOMove(originalPositions[obj], setting.flyInSpeed)
                    .SetEase(Ease.OutBounce);
                flyTasks.Add(tween.AsyncWaitForCompletion());

                await Task.Delay((int)(dominoDelay * 1000));
            }

            await Task.WhenAll(flyTasks);

            if (audioSource && audioSource.isPlaying)
                audioSource.Stop();

            if (group < groupSettings.Count - 1)
                await Task.Delay(500);
        }
    }

    public bool IsMapReady()
    {
        return isMapReady;
    }
}