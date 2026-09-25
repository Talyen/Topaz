using UnityEngine;

namespace Topaz.VisualStudy
{
    /// <summary>Soft local grounding where the outdoor sun cannot cast a useful shadow.</summary>
    public sealed class GroundContactShadow : MonoBehaviour
    {
        [SerializeField] Material material;
        [SerializeField] float radius = .48f;

        Transform _shadow;
        Mesh _mesh;

        void Awake()
        {
            if (material == null) return;
            var go = new GameObject("Ground Contact Shadow", typeof(MeshFilter), typeof(MeshRenderer));
            _shadow = go.transform;
            _mesh = CreateDisc();
            go.GetComponent<MeshFilter>().sharedMesh = _mesh;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void LateUpdate()
        {
            if (_shadow == null) return;
            float y = GroundSurface.Height(transform.position);
            _shadow.position = new Vector3(transform.position.x, y + .025f, transform.position.z);
            _shadow.localScale = new Vector3(radius, 1f, radius);
        }

        void OnEnable()
        {
            if (_shadow != null) _shadow.gameObject.SetActive(true);
        }

        void OnDisable()
        {
            if (_shadow != null) _shadow.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (_shadow != null) Destroy(_shadow.gameObject);
            if (_mesh != null) Destroy(_mesh);
        }

        static Mesh CreateDisc()
        {
            const int sides = 24;
            var vertices = new Vector3[sides + 1];
            var colors = new Color32[sides + 1];
            var triangles = new int[sides * 3];
            colors[0] = new Color32(0, 0, 0, 78);
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                colors[i + 1] = new Color32(0, 0, 0, 0);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % sides + 1;
                triangles[i * 3 + 2] = i + 1;
            }
            var mesh = new Mesh { name = "Contact Shadow Disc", vertices = vertices, colors32 = colors,
                triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
