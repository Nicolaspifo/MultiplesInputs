using UnityEngine;

namespace NetworkingLab.Player
{
    /// <summary>Axis-separated XZ movement resolution against scene colliders.</summary>
    public static class MovementCollision
    {
        private const float FeetClearance = 0.1f;

        public static Vector3 Resolve(Transform self, Vector3 from, Vector3 to, LayerMask obstacleLayers, float radius, float height)
        {
            if (!Blocked(self, to, obstacleLayers, radius, height))
            {
                return to;
            }

            Vector3 xOnly = new(to.x, from.y, from.z);
            if (!Blocked(self, xOnly, obstacleLayers, radius, height))
            {
                return xOnly;
            }

            Vector3 zOnly = new(from.x, from.y, to.z);
            if (!Blocked(self, zOnly, obstacleLayers, radius, height))
            {
                return zOnly;
            }

            return from;
        }

        private static bool Blocked(Transform self, Vector3 position, LayerMask obstacleLayers, float radius, float height)
        {
            Vector3 halfExtents = new(radius, height * 0.5f, radius);
            Vector3 center = position + Vector3.up * (height * 0.5f);
            Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, obstacleLayers, QueryTriggerInteraction.Ignore);
            float feet = position.y + FeetClearance;
            foreach (Collider hit in hits)
            {
                if (hit.transform == self || hit.transform.IsChildOf(self))
                {
                    continue;
                }

                if (hit.bounds.max.y <= feet)
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
