using UnityEngine;

namespace Shift.EditorTools
{
    /// <summary>Greybox primitives shared by the Zone builders.</summary>
    internal static class ZoneParts
    {
        public static readonly Color Neutral = new Color(0.62f, 0.62f, 0.64f);
        public static readonly Color Accent = new Color(0.9f, 0.85f, 0.55f);
        public static readonly Color Hazard = new Color(0.75f, 0.3f, 0.3f);
        public static readonly Color Ghostly = new Color(0.55f, 0.65f, 0.9f);

        /// <summary>An axis-aligned box of solid geometry, positioned by its centre.</summary>
        public static GameObject Slab(string name, Transform parent, Vector3 centre, Vector3 size, Color color)
        {
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = name;
            if (parent != null) slab.transform.SetParent(parent);
            slab.transform.position = centre;
            slab.transform.localScale = size;

            Tint(slab, color);
            return slab;
        }

        /// <summary>
        /// A trigger volume. Never place one on the Prop layer: PHASE sets excludeLayers, which
        /// suppresses trigger callbacks too, so a ghost would pass through it unnoticed.
        /// </summary>
        public static GameObject TriggerBox(string name, Transform parent, Vector3 centre, Vector3 size)
        {
            GameObject trigger = new GameObject(name);
            if (parent != null) trigger.transform.SetParent(parent);
            trigger.transform.position = centre;

            BoxCollider box = trigger.AddComponent<BoxCollider>();
            box.size = size;
            box.isTrigger = true;

            return trigger;
        }

        /// <summary>A bare transform, for spawn and respawn poses.</summary>
        public static Transform Marker(string name, Transform parent, Vector3 position, float yaw = 0f)
        {
            GameObject marker = new GameObject(name);
            if (parent != null) marker.transform.SetParent(parent);
            marker.transform.position = position;
            marker.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            return marker.transform;
        }

        public static GameObject Group(string name, Transform parent = null)
        {
            GameObject group = new GameObject(name);
            if (parent != null) group.transform.SetParent(parent);
            return group;
        }

        public static void Tint(GameObject target, Color color)
        {
            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer == null) return;

            Material material = new Material(renderer.sharedMaterial) { color = color };
            renderer.sharedMaterial = material;
        }
    }
}
