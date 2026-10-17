using UnityEngine;

namespace NetworkingLab.Combat
{
    /// <summary>Builds the closed arena with perimeter walls and cover blocks.</summary>
    public static class ArenaBuilder
    {
        public const float HalfWidth = 11f;
        public const float HalfDepth = 8f;
        public const string RootName = "Arena";

        private const float WallHeight = 1.5f;
        private const float WallThickness = 0.6f;

        public static GameObject Build(Transform parent = null, Material wallMaterial = null)
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(existing);
                }
                else
                {
                    Object.DestroyImmediate(existing);
                }
            }

            GameObject root = new(RootName);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            float y = WallHeight * 0.5f;
            AddWall(root.transform, "Perimeter North", new Vector3(0f, y, HalfDepth), new Vector3(HalfWidth * 2f + WallThickness, WallHeight, WallThickness), wallMaterial);
            AddWall(root.transform, "Perimeter South", new Vector3(0f, y, -HalfDepth), new Vector3(HalfWidth * 2f + WallThickness, WallHeight, WallThickness), wallMaterial);
            AddWall(root.transform, "Perimeter East", new Vector3(HalfWidth, y, 0f), new Vector3(WallThickness, WallHeight, HalfDepth * 2f + WallThickness), wallMaterial);
            AddWall(root.transform, "Perimeter West", new Vector3(-HalfWidth, y, 0f), new Vector3(WallThickness, WallHeight, HalfDepth * 2f + WallThickness), wallMaterial);
            AddWall(root.transform, "Cover North", new Vector3(0f, y, 2.4f), new Vector3(2.4f, WallHeight, WallThickness), wallMaterial);
            AddWall(root.transform, "Cover South", new Vector3(0f, y, -2.4f), new Vector3(2.4f, WallHeight, WallThickness), wallMaterial);
            AddWall(root.transform, "Cover East", new Vector3(3.6f, y, 0f), new Vector3(WallThickness, WallHeight, 2.4f), wallMaterial);
            AddWall(root.transform, "Cover West", new Vector3(-3.6f, y, 0f), new Vector3(WallThickness, WallHeight, 2.4f), wallMaterial);
            AddWall(root.transform, "Cover North East", new Vector3(7.5f, y, 4.8f), new Vector3(1.8f, WallHeight, 1.8f), wallMaterial);
            AddWall(root.transform, "Cover North West", new Vector3(-7.5f, y, 4.8f), new Vector3(1.8f, WallHeight, 1.8f), wallMaterial);
            AddWall(root.transform, "Cover South East", new Vector3(7.5f, y, -4.8f), new Vector3(1.8f, WallHeight, 1.8f), wallMaterial);
            AddWall(root.transform, "Cover South West", new Vector3(-7.5f, y, -4.8f), new Vector3(1.8f, WallHeight, 1.8f), wallMaterial);
            return root;
        }

        private static void AddWall(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = center;
            wall.transform.localScale = size;
            wall.AddComponent<ArenaWall>();
            wall.GetComponent<Renderer>().sharedMaterial = material != null ? material : CombatFx.WallMaterial;
        }
    }
}
