using System.Collections.Generic;
using PlayerSystem;
using UnityEngine;

/// <summary>
/// Spawns one Collectible at a time at this spot.
///  - When the spot is empty (level start, picked up, or cleared), waits spawnDelay seconds, then spawns.
///  - The type is picked randomly by weight, e.g. Arrow 3, Heart 1 => 75% Arrow, 25% Heart.
///  - On Player spawn (start and every respawn): removes its item and restarts the timer.
/// </summary>
public class CollectibleSpot : MonoBehaviour
{
    [System.Serializable]
    private struct Entry
    {
        public Collectible prefab;

        [Min(0f)]
        public float weight;
    }

    [SerializeField]
    private float spawnDelay = 10f; // seconds an empty spot waits before spawning

    [SerializeField]
    private Vector3 spawnOffset = Vector3.zero; // added to this spot's position

    [SerializeField]
    private List<Entry> entries = new();

    private Collectible current;
    private bool hasItem; // true while an item spawned here hasn't been taken yet
    private float timer;

    private void OnEnable() => PlayerEvents.Spawned += Clear;

    private void OnDisable() => PlayerEvents.Spawned -= Clear;

    private void Start()
    {
        timer = spawnDelay;
    }

    private void Update()
    {
        if (hasItem)
        {
            if (current != null)
                return; // still waiting to be picked up

            // Picked up (Collectible destroyed itself): the spot is empty now.
            hasItem = false;
            timer = spawnDelay;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
            Spawn();
    }

    // Remove the current item and restart the timer.
    public void Clear()
    {
        if (current != null)
            Destroy(current.gameObject);
        current = null;
        hasItem = false;
        timer = spawnDelay;
    }

    private void Spawn()
    {
        Collectible prefab = PickByWeight();
        if (prefab == null)
        {
            timer = spawnDelay; // nothing to spawn, try again later
            return;
        }

        current = Instantiate(prefab, transform.position + spawnOffset, transform.rotation);
        hasItem = true;
    }

    private Collectible PickByWeight()
    {
        float total = 0f;
        foreach (Entry e in entries)
            if (e.prefab != null && e.weight > 0f)
                total += e.weight;

        if (total <= 0f)
            return null;

        float roll = Random.Range(0f, total);
        Collectible last = null;
        foreach (Entry e in entries)
        {
            if (e.prefab == null || e.weight <= 0f)
                continue;
            if (roll < e.weight)
                return e.prefab;
            roll -= e.weight;
            last = e.prefab;
        }
        return last; // float rounding fallback
    }
}
