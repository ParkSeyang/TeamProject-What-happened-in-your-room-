using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARGameManager : MonoBehaviour
{
    public static ARGameManager Instance { get; private set; }

    [Header("Core Components")]
    [SerializeField] private ARSpaceManager spaceManager;

    [Header("Score Management")]
    [SerializeField] private int sessionCatchCount = 0;
    public int SessionCatchCount => sessionCatchCount;

    [Header("Spawner Settings")]
    [SerializeField] private Monster[] monsterPrefabs;
    [SerializeField] private float spawnInterval = 5.0f; 
    [SerializeField] private float spawnHeightOffset = 0.1f;
    [SerializeField] private int maxMonsterCount = 3;

    private bool isSpawning = false;
    private Coroutine spawnCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (spaceManager == null) spaceManager = FindFirstObjectByType<ARSpaceManager>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnGUI()
    {
        float buttonWidth = 200f;
        float buttonHeight = 60f;
        float margin = 20f;
        
        Rect buttonRect = new Rect(Screen.width - buttonWidth - margin, margin, buttonWidth, buttonHeight);

        GUI.skin.button.fontSize = 16;
        string btnText = isSpawning ? "■ Stop Spawning" : "▶ Start Spawning";
        
        if (GUI.Button(buttonRect, btnText))
        {
            ToggleSpawning();
        }

        Rect labelRect = new Rect(Screen.width - buttonWidth - margin, margin + buttonHeight + 5, buttonWidth, 30);
        int currentCount = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None).Length;
        string statusText = isSpawning 
            ? $"<color=lime>Spawning Active ({currentCount}/{maxMonsterCount})</color>" 
            : "<color=red>Spawning Paused</color>";
            
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.UpperRight };
        GUI.Label(labelRect, statusText, labelStyle);
    }

    #region Catch Logic

    public void AddCatch(string monsterId)
    {
        sessionCatchCount++;
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.AddMonsterToInventory(monsterId);
        }
    }

    #endregion

    #region Spawner Logic

    public void ToggleSpawning()
    {
        isSpawning = !isSpawning;

        if (spaceManager != null)
        {
            spaceManager.SetOcclusion(isSpawning);
        }

        if (isSpawning)
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
        else
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (spaceManager == null || monsterPrefabs == null || monsterPrefabs.Length == 0)
                continue;

            int currentMonsterCount = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None).Length;
            if (currentMonsterCount >= maxMonsterCount) continue;

            var cubes = spaceManager.GetGeneratedCubes();
            if (cubes == null || cubes.Count == 0) continue;

            GameObject targetCube = cubes[Random.Range(0, cubes.Count)];
            Vector3 spawnPos = CalculateRandomPointOnCube(targetCube);

            Monster prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }

    private Vector3 CalculateRandomPointOnCube(GameObject cube)
    {
        Vector3 size = cube.transform.localScale;
        Vector3 center = cube.transform.position;

        float randomX = Random.Range(-size.x * 0.5f, size.x * 0.5f);
        float randomZ = Random.Range(-size.z * 0.5f, size.z * 0.5f);
        float topY = center.y + (size.y * 0.5f) + spawnHeightOffset;

        return new Vector3(center.x + randomX, topY, center.z + randomZ);
    }

    #endregion
}
