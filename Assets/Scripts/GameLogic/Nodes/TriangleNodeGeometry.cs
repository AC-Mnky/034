using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class TriangleNodeGeometry : MonoBehaviour
{
    [Header("Isosceles Triangle Face (XY plane)")]
    [Min(0.01f)] public float Width = 0.8f;
    [Min(0.01f)] public float Height = 1.2f;

    [Header("Prism Half-Depth (extends from -Depth to +Depth)")]
    [Min(0.01f)] public float Depth = 0.6f;
    private Mesh _meshInstance;

    private void Awake()
    {
        RebuildGeometry();
    }

    private void Reset()
    {
        RebuildGeometry();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        // Avoid MeshFilter change messages during OnValidate lifecycle.
        EditorApplication.delayCall += DelayedRebuildInEditor;
#else
        RebuildGeometry();
#endif
    }

#if UNITY_EDITOR
    private void DelayedRebuildInEditor()
    {
        if (this == null) return;
        RebuildGeometry();
    }
#endif

    public void RebuildGeometry()
    {
        BuildCollider();
        BuildMesh();
    }

    private void BuildCollider()
    {
        var poly = GetComponent<PolygonCollider2D>();
        poly.pathCount = 1;
        poly.SetPath(0, GetTrianglePoints2D());
    }

    private void BuildMesh()
    {
        var mf = GetComponent<MeshFilter>();
        if (_meshInstance == null)
        {
            _meshInstance = new Mesh();
            _meshInstance.name = "TriangleNodeMesh_Instance";
        }
        _meshInstance.Clear();
        mf.sharedMesh = _meshInstance;

        Vector2[] tri2D = GetTrianglePoints2D();
        Vector3 f0 = new Vector3(tri2D[0].x, tri2D[0].y, -Depth);
        Vector3 f1 = new Vector3(tri2D[1].x, tri2D[1].y, -Depth);
        Vector3 f2 = new Vector3(tri2D[2].x, tri2D[2].y, -Depth);
        Vector3 b0 = new Vector3(tri2D[0].x, tri2D[0].y, Depth);
        Vector3 b1 = new Vector3(tri2D[1].x, tri2D[1].y, Depth);
        Vector3 b2 = new Vector3(tri2D[2].x, tri2D[2].y, Depth);

        // Use per-triangle vertices to keep face normals flat and color/shading uniform per face.
        _meshInstance.vertices = new[]
        {
            // Front / back
            f0, f2, f1,
            b0, b1, b2,

            // Side face (edge 0-1)
            f0, f1, b1,
            f0, b1, b0,

            // Side face (edge 1-2)
            f1, f2, b2,
            f1, b2, b1,

            // Side face (edge 2-0)
            f2, f0, b0,
            f2, b0, b2
        };

        _meshInstance.triangles = new[]
        {
            0, 1, 2,
            3, 4, 5,
            6, 7, 8,
            9, 10, 11,
            12, 13, 14,
            15, 16, 17,
            18, 19, 20,
            21, 22, 23
        };

        _meshInstance.RecalculateNormals();
        _meshInstance.RecalculateBounds();
    }

    private Vector2[] GetTrianglePoints2D()
    {
        // Isosceles triangle centered by centroid at origin.
        float halfW = 0.5f * Width;
        float h = Height;
        return new[]
        {
            new Vector2(-halfW, -h / 3f),
            new Vector2( halfW, -h / 3f),
            new Vector2(0f, 2f * h / 3f)
        };
    }
}
