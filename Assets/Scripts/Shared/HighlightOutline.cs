using UnityEngine;
using UnityEngine.Rendering;

namespace Shift.Shared
{
    /// <summary>
    /// Draws a border around a mesh by rendering a slightly inflated, front-face-culled copy
    /// behind it. Built lazily on first use and added at runtime by whatever wants to
    /// highlight something, so props do not need to be authored with it.
    /// </summary>
    [DisallowMultipleComponent]
    public class HighlightOutline : MonoBehaviour
    {
        [SerializeField] private Color _color = new Color(1f, 0.79f, 0.28f, 1f);
        [SerializeField] private float _thickness = 0.025f;

        private GameObject _outline;
        private Material _material;
        private bool _buildFailed;

        public void SetVisible(bool visible)
        {
            if (visible && _outline == null && !_buildFailed) Build();
            if (_outline != null) _outline.SetActive(visible);
        }

        private void Build()
        {
            MeshFilter sourceFilter = GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                _buildFailed = true;
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null)
            {
                _buildFailed = true;
                return;
            }

            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _material.color = _color;
            if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", _color);
            // Render only backfaces of the inflated copy, so it reads as a rim around the
            // original rather than covering it.
            if (_material.HasProperty("_Cull")) _material.SetFloat("_Cull", (float)CullMode.Front);

            _outline = new GameObject("HighlightOutline");
            _outline.transform.SetParent(transform, false);
            _outline.layer = gameObject.layer;

            _outline.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer renderer = _outline.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            _outline.transform.localScale = InflatedScale(sourceFilter.sharedMesh.bounds.size);
        }

        /// <summary>Scales per-axis so the border is a constant world-space width on any prop size.</summary>
        private Vector3 InflatedScale(Vector3 meshSize)
        {
            return new Vector3(
                1f + _thickness * 2f / Mathf.Max(meshSize.x, 0.001f),
                1f + _thickness * 2f / Mathf.Max(meshSize.y, 0.001f),
                1f + _thickness * 2f / Mathf.Max(meshSize.z, 0.001f));
        }

        private void OnDestroy()
        {
            if (_material == null) return;

            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
        }
    }
}
