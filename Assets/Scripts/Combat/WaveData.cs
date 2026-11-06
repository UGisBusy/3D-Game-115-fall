using System.Collections.Generic;
using UnityEngine;

// Wave data used by WaveSet (the Waves File) and EnemySpawner.
// Plain [Serializable] classes, so they also load from JSON with JsonUtility if ever needed.

public enum EnemyType
{
    Heavy, // higher life, move slow
    Light, // lower life, move fast
    Charge, // stand still and charge periodically
    Jump, // stand still and jump periodically
    Shield, // has a Dashable shield
}

public enum SpawnArea
{
    North,
    East,
    South,
    West,
    Random, // one of the four, picked when the group spawns
}

[System.Serializable]
public class Wave
{
    public List<EnemyGroup> groups = new();
}

[System.Serializable]
public class EnemyGroup
{
    public EnemyType type;
    public SpawnArea area;

    [Min(1)]
    public int count = 1;

    [Min(0f)]
    public float delay; // seconds after the Wave starts, 0 = right away
}
