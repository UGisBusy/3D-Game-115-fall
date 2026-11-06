using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Waves File: all Waves of a combat level, in order.
/// Create with: Project window > right-click > Create > 3DG > Wave Set.
/// </summary>
[CreateAssetMenu(fileName = "Waves", menuName = "3DG/Wave Set")]
public class WaveSet : ScriptableObject
{
    [SerializeField]
    private List<Wave> waves = new();

    public IReadOnlyList<Wave> Waves => waves;
}
