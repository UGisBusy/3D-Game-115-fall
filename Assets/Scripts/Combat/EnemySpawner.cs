using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns Wave data into enemies. CombatLevelManager decides when a Wave starts; this decides where enemies go.
///  - Each group spawns `count` enemies of its type in its area, `delay` sec after the Wave starts.
///  - Positions are random on the bottom of the area's BoxCollider, at least minSpacing apart.
///    The BoxCollider can be disabled; only its size is used.
///  - Enemies face this object's position, so put it at the arena center.
///  - An enemy counts as dead when its GameObject is destroyed.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    private struct EnemyPrefab
    {
        public EnemyType type;
        public GameObject prefab; // TODO: change to Enemy once the Enemy class exists
    }

    private class PendingGroup
    {
        public EnemyGroup group;
        public float timeLeft;
    }

    [Header("Enemy Prefabs")]
    [SerializeField]
    private List<EnemyPrefab> prefabs = new();

    [Header("Spawn Areas")]
    [SerializeField]
    private BoxCollider north;

    [SerializeField]
    private BoxCollider east;

    [SerializeField]
    private BoxCollider south;

    [SerializeField]
    private BoxCollider west;

    [Header("Placement")]
    [SerializeField]
    private float minSpacing = 1.5f; // min distance between spawned enemies

    [SerializeField]
    private int maxTries = 10; // random tries to find a free spot before giving up on spacing

    private readonly List<GameObject> alive = new();
    private readonly List<PendingGroup> pending = new();
    private readonly List<BoxCollider> areaBuffer = new();

    public int AliveCount
    {
        get
        {
            alive.RemoveAll(e => e == null);
            return alive.Count;
        }
    }

    // True when nothing is alive and no delayed group is still waiting.
    public bool IsWaveDone => pending.Count == 0 && AliveCount == 0;

    // ---------------- API for CombatLevelManager ----------------

    public void SpawnWave(Wave wave)
    {
        if (wave == null || wave.groups == null)
            return;

        foreach (EnemyGroup group in wave.groups)
        {
            if (group.delay <= 0f)
                SpawnGroup(group);
            else
                pending.Add(new PendingGroup { group = group, timeLeft = group.delay });
        }
    }

    // Removes all enemies and cancels groups that haven't spawned yet (Player respawn).
    public void ClearAll()
    {
        pending.Clear();
        foreach (GameObject e in alive)
            if (e != null)
                Destroy(e);
        alive.Clear();
    }

    // ---------------- Spawning ----------------

    private void Update()
    {
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            pending[i].timeLeft -= Time.deltaTime; // paused time (timeScale 0) doesn't count
            if (pending[i].timeLeft > 0f)
                continue;

            EnemyGroup group = pending[i].group;
            pending.RemoveAt(i);
            SpawnGroup(group);
        }
    }

    private void SpawnGroup(EnemyGroup group)
    {
        GameObject prefab = GetPrefab(group.type);
        if (prefab == null)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)}: no prefab for {group.type}");
            return;
        }

        BoxCollider area = GetArea(group.area);
        if (area == null)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)}: spawn area {group.area} not set");
            return;
        }

        for (int i = 0; i < group.count; i++)
        {
            Vector3 position = FindFreePosition(area);
            alive.Add(Instantiate(prefab, position, FaceCenter(position)));
        }
    }

    private GameObject GetPrefab(EnemyType type)
    {
        foreach (EnemyPrefab p in prefabs)
            if (p.type == type)
                return p.prefab;
        return null;
    }

    private BoxCollider GetArea(SpawnArea area)
    {
        switch (area)
        {
            case SpawnArea.North:
                return north;
            case SpawnArea.East:
                return east;
            case SpawnArea.South:
                return south;
            case SpawnArea.West:
                return west;
        }

        // Random: any area that is set.
        areaBuffer.Clear();
        foreach (BoxCollider box in new[] { north, east, south, west })
            if (box != null)
                areaBuffer.Add(box);
        return areaBuffer.Count > 0 ? areaBuffer[Random.Range(0, areaBuffer.Count)] : null;
    }

    // Random point in the area, away from other enemies; after maxTries, takes the last try.
    private Vector3 FindFreePosition(BoxCollider area)
    {
        Vector3 point = RandomPointIn(area);
        for (int i = 1; i < maxTries && !IsFree(point); i++)
            point = RandomPointIn(area);
        return point;
    }

    // Random point on the bottom face of the box.
    private static Vector3 RandomPointIn(BoxCollider box)
    {
        Vector3 c = box.center;
        Vector3 s = box.size;
        Vector3 local = new(
            c.x + Random.Range(-0.5f, 0.5f) * s.x,
            c.y - s.y * 0.5f,
            c.z + Random.Range(-0.5f, 0.5f) * s.z
        );
        return box.transform.TransformPoint(local);
    }

    private bool IsFree(Vector3 point)
    {
        foreach (GameObject e in alive)
        {
            if (e == null)
                continue;
            Vector3 d = e.transform.position - point;
            d.y = 0f;
            if (d.sqrMagnitude < minSpacing * minSpacing)
                return false;
        }
        return true;
    }

    private Quaternion FaceCenter(Vector3 position)
    {
        Vector3 dir = transform.position - position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : Quaternion.identity;
    }
}
