using Shift.Player;
using Shift.UI;
using UnityEditor;
using UnityEngine;

namespace Shift.EditorTools
{
    /// <summary>
    /// Scene pieces shared by the sandbox builders. Extracted verbatim so the movement sandbox
    /// stays byte-for-byte the scene it always was and remains usable as a feel baseline.
    /// </summary>
    internal static class SandboxParts
    {
        public const float EyeHeight = 1.7f;
        public const float CapsuleHeight = 2f;
        public const float CapsuleRadius = 0.5f;

        public static GameObject CreateGround(float scale = 5f)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(scale, 1f, scale);
            return ground;
        }

        public static GameObject CreatePlayer()
        {
            // Origin sits at the feet — FirstPersonMotor's ground check assumes this.
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.1f, 0f);

            CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, CapsuleHeight * 0.5f, 0f);
            capsule.height = CapsuleHeight;
            capsule.radius = CapsuleRadius;

            Rigidbody body = player.AddComponent<Rigidbody>();
            body.mass = 70f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            player.AddComponent<FirstPersonMotor>();
            return player;
        }

        public static Transform AttachCamera(GameObject player, CrosshairView crosshair)
        {
            GameObject pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform);
            pivot.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            pivot.transform.localRotation = Quaternion.identity;

            Camera camera = Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.SetParent(pivot.transform);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;

            BindCameraPivot(player.AddComponent<FirstPersonLook>(), pivot.transform);
            BindCameraPivot(player.GetComponent<FirstPersonMotor>(), pivot.transform);

            ObjectCarrier carrier = player.AddComponent<ObjectCarrier>();
            BindCameraPivot(carrier, pivot.transform);
            Bind(carrier, "_crosshair", crosshair);

            return pivot.transform;
        }

        /// <summary>
        /// Pitch and crouch height both drive the pivot, so the camera must hang off an
        /// intermediate transform rather than the player root — otherwise looking up would
        /// tip the capsule and crouching would sink the whole body.
        /// </summary>
        public static Transform EnsureCameraPivot(GameObject player, Camera camera)
        {
            Transform parent = camera.transform.parent;
            if (parent != null && parent != player.transform) return parent;

            GameObject pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform);
            pivot.transform.localPosition = new Vector3(0f, camera.transform.localPosition.y, 0f);
            pivot.transform.localRotation = Quaternion.identity;

            camera.transform.SetParent(pivot.transform);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;

            return pivot.transform;
        }

        public static void BindCameraPivot(Component component, Transform pivot)
        {
            Bind(component, "_cameraPivot", pivot);
        }

        /// <summary>
        /// The target fields are private [SerializeField]; SerializedObject is the supported way
        /// to set them without widening their visibility just for the builder.
        /// </summary>
        public static void Bind(Component component, string fieldName, Object value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogWarning($"{component.GetType().Name} has no serialized field '{fieldName}'.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void BindArrayElement(Component component, string fieldName, int index, Object value)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty array = serialized.FindProperty(fieldName);
            if (array == null || !array.isArray) return;

            if (array.arraySize <= index) array.arraySize = index + 1;
            array.GetArrayElementAtIndex(index).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
