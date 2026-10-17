using UnityEngine;

namespace NetworkingLab.Combat
{
    /// <summary>Loads combat prefabs that live under a Resources folder.</summary>
    public static class CombatAssets
    {
        public const string ProjectileResourcePath = "Combat/Projectile";

        private static GameObject projectilePrefab;

        public static GameObject ProjectilePrefab
        {
            get
            {
                if (projectilePrefab == null)
                {
                    projectilePrefab = Resources.Load<GameObject>(ProjectileResourcePath);
                }

                return projectilePrefab != null ? projectilePrefab : projectilePrefab = CreateRuntimeProjectile();
            }
        }

        private static GameObject CreateRuntimeProjectile()
        {
            GameObject root = new("Projectile (runtime)");
            root.SetActive(false);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.25f;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            Object.Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.5f;
            visual.GetComponent<Renderer>().sharedMaterial = CombatFx.ProjectileMaterial;

            root.AddComponent<Projectile>();
            return root;
        }
    }
}
