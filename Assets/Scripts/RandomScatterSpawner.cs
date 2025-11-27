using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RandomScatterSpawner : MonoBehaviour
{
    [Header("Area (X/Z size around this object)")]
    [SerializeField]
    private Vector2 areaSize = new Vector2(100f, 100f); // width/length

    [SerializeField]
    private float spawnHeight = 50f; // raycast height above ground

    [SerializeField]
    private LayerMask groundMask = ~0; // set to Ground layers

    [SerializeField]
    private LayerMask blockedMask; // e.g. Building, Obstacles

    [SerializeField]
    private float agentRadius = 0.5f;

    [SerializeField]
    private float agentHeight = 2f;

    [Header("Enemies")]
    [SerializeField]
    private MechMutantEnemy enemyPrefab;

    [SerializeField]
    private int enemyCount = 10;

    [Header("Loot")]
    [SerializeField]
    private GameObject[] lootPrefabs;

    [SerializeField]
    private int lootCount = 30;

    [Header("Spacing")]
    [SerializeField]
    private float minDistanceBetween = 3f;

    [SerializeField]
    private int maxAttemptsPerSpawn = 20;

    private readonly List<Vector3> usedPositions = new List<Vector3>();

    private void Start()
    {
        SpawnEnemies();
        SpawnLoot();
    }

    private void SpawnEnemies()
    {
        if (!enemyPrefab || enemyCount <= 0)
            return;

        for (int i = 0; i < enemyCount; i++)
        {
            if (!TryGetValidPosition(out Vector3 pos))
                break;

            // snap to NavMesh so NavMeshAgent works
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                Instantiate(enemyPrefab, hit.position, Quaternion.identity);
                usedPositions.Add(hit.position);
            }
        }
    }

    private void SpawnLoot()
    {
        if (lootPrefabs == null || lootPrefabs.Length == 0 || lootCount <= 0)
            return;

        for (int i = 0; i < lootCount; i++)
        {
            if (!TryGetValidPosition(out Vector3 pos))
                break;

            GameObject prefab = lootPrefabs[Random.Range(0, lootPrefabs.Length)];
            if (!prefab)
                continue;

            Instantiate(prefab, pos, Quaternion.identity);
            usedPositions.Add(pos);
        }
    }

    private bool TryGetValidPosition(out Vector3 result)
    {
        Vector3 center = transform.position;

        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            float x = Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
            float z = Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f);

            Vector3 origin = new Vector3(center.x + x, center.y + spawnHeight, center.z + z);

            // 1) Hit only ground
            if (
                !Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    spawnHeight * 2f,
                    groundMask,
                    QueryTriggerInteraction.Ignore
                )
            )
                continue;

            Vector3 candidate = hit.point;

            // 2) Enough distance from other spawns
            if (!IsFarEnough(candidate))
                continue;

            // 3) No buildings/obstacles overlapping the agent capsule
            Vector3 capsuleBottom = candidate + Vector3.up * agentRadius;
            Vector3 capsuleTop = candidate + Vector3.up * (agentHeight - agentRadius);

            if (
                Physics.CheckCapsule(
                    capsuleBottom,
                    capsuleTop,
                    agentRadius,
                    blockedMask,
                    QueryTriggerInteraction.Ignore
                )
            )
                continue;

            result = candidate;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private bool IsFarEnough(Vector3 position)
    {
        float minSqr = minDistanceBetween * minDistanceBetween;
        for (int i = 0; i < usedPositions.Count; i++)
        {
            if ((usedPositions[i] - position).sqrMagnitude < minSqr)
                return false;
        }
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position;
        Gizmos.DrawWireCube(center, new Vector3(areaSize.x, 0.1f, areaSize.y));
    }
}
