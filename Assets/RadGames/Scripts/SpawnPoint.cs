using UnityEngine;

namespace RadGames.Scripts
{
    public class SpawnPoint
    {
        public SpawnData SpawnData;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsValid = false;

        public Vector3 Up => Rotation * Vector3.up;

        public SpawnPoint(Vector3 position, Quaternion rotation, SpawnData spawnData)
        {
            SpawnData = spawnData;
            Position = position;
            Rotation = rotation;

            if (spawnData.Prefab == null)
            {
                return;
            }

            var spawnablePrefab = spawnData.Prefab.GetComponent<SpawnablePrefab>();
            if (spawnablePrefab == null)
            {
                IsValid = true;
            }
            else
            {
                var height = spawnablePrefab.Height;
                var ray = new Ray(position, Up);
                IsValid = Physics.Raycast(ray, height) == false;
            }
        }
    }
}