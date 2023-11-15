using System.Collections.Generic;
using UnityEngine;

namespace RadGames.Scripts
{
    public struct SpawnData
    {
        public Vector2 PointInDisc;
        public float RandAngleDeg;
        public GameObject Prefab;

        public void SetRandomValues(List<GameObject> prefabs)
        {
            PointInDisc = Random.insideUnitCircle;
            RandAngleDeg = Random.value * 360;
            Prefab = prefabs.Count == 0 ? null : prefabs[Random.Range(0, prefabs.Count)];
        }
    }
}