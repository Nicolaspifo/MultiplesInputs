using UnityEngine;

namespace NetworkingLab.Combat
{
    /// <summary>Creates simple materials and effects without needing authored assets.</summary>
    public static class CombatFx
    {
        public static readonly Color TankColor = new(1f, 0.6f, 0.1f);
        public static readonly Color ProjectileColor = new(1f, 0.85f, 0.25f);
        private static readonly Color WallColor = new(0.35f, 0.37f, 0.42f);

        private static Material explosionMaterial;
        private static Material projectileMaterial;
        private static Material wallMaterial;

        public static Material ExplosionMaterial => explosionMaterial != null
            ? explosionMaterial
            : explosionMaterial = CreateMaterial(new Color(1f, 0.45f, 0.05f), true);

        public static Material ProjectileMaterial => projectileMaterial != null
            ? projectileMaterial
            : projectileMaterial = CreateMaterial(ProjectileColor, true);

        public static Material WallMaterial => wallMaterial != null
            ? wallMaterial
            : wallMaterial = CreateMaterial(WallColor, false);

        public static Material CreateMaterial(Color color, bool emissive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = $"Combat-{color}" };
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color * 2f);
                }
            }

            return material;
        }

        public static void SpawnExplosion(Vector3 position, Color color)
        {
            GameObject root = new("Explosion");
            root.transform.position = position;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.5f;
            Material material = CreateMaterial(color, true);
            visual.GetComponent<Renderer>().sharedMaterial = material;

            root.AddComponent<ExplosionEffect>();
            Object.Destroy(material, 1f);
            Object.Destroy(root, 1f);
        }
    }
}
