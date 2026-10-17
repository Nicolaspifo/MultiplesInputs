using UnityEngine;

namespace NetworkingLab.NGO
{
    /// <summary>Scene markers that define where each player spawns in the NGO scene.</summary>
    public sealed class NgoSpawnPoints : MonoBehaviour
    {
        [SerializeField] private Transform[] points;
        [SerializeField] private bool random;

        public int Count => points != null ? points.Length : 0;

        public Vector3 GetPosition(int index)
        {
            if (points == null || points.Length == 0)
            {
                return Vector3.zero;
            }

            int slot = random ? Random.Range(0, points.Length) : Mathf.Abs(index) % points.Length;
            Transform point = points[slot];
            return point != null ? point.position : Vector3.zero;
        }

        private void OnDrawGizmos()
        {
            if (points == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.8f, 0.1f);
            foreach (Transform point in points)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.8f);
                    Gizmos.DrawLine(point.position, point.position + Vector3.up * 2f);
                }
            }
        }
    }
}
