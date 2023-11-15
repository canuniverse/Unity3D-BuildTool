using UnityEditor;
using UnityEngine;

namespace RadGames.Scripts
{
    public class SpawnablePrefab : MonoBehaviour
    {
        public float Height = 1f;

        private void OnDrawGizmosSelected()
        {
            var pointA = transform.position;
            var pointB = transform.position + transform.up * Height;

            Handles.DrawAAPolyLine(pointA, pointB);

            void DrawSphere(Vector3 point) => Gizmos.DrawSphere(point, HandleUtility.GetHandleSize(point) * 0.3f);

            DrawSphere(pointA);
            DrawSphere(pointB);
        }
    }
}