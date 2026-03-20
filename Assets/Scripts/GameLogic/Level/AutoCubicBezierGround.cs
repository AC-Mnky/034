using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class AutoCubicBezierGround : MonoBehaviour
{
    [Header("Bezier Points")]
    public Transform PointA;
    public Transform PointMiddleA;
    public Transform PointMiddleB;
    public Transform PointB;

    [Header("Ground Hierarchy")]
    public Transform GroundRoot;
    public Transform GroundInstance;

    [Header("Bezier Mesh")]
    [Min(0.01f)] public float CurveThickness = 0.6f;
    [Range(4, 1024)] public int SegmentCount = 128;
    [Min(0f)] public float DoubleSidedGap = 0.02f;
    public Material GroundMaterial;

    private const float RuntimeEpsilon = 1e-8f;
    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private PolygonCollider2D _polygonCollider;
    private bool _hasRuntimeState;
    private bool _runtimeForceRefresh = true;
    private Transform _runtimePointARef;
    private Transform _runtimePointMiddleARef;
    private Transform _runtimePointMiddleBRef;
    private Transform _runtimePointBRef;
    private Transform _runtimeGroundRef;
    private Material _runtimeMaterial;
    private Vector2 _runtimePointAPos;
    private Vector2 _runtimePointMiddleAPos;
    private Vector2 _runtimePointMiddleBPos;
    private Vector2 _runtimePointBPos;
    private float _runtimeMiddleZ;
    private float _runtimeCurveThickness;
    private int _runtimeSegmentCount;
    private float _runtimeDoubleSidedGap;

    private void Reset()
    {
        EnsureStructure();
        EnsureGroundInstance();
        RebuildGround();
    }

    private void Awake()
    {
        EnsureStructure();
        EnsureGroundInstance();
        RebuildGround();
    }

    private void OnValidate()
    {
        EnsureStructure();
        EnsureGroundInstance();
        RebuildGround();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            RebuildGround();
            return;
        }

        if (!ShouldRefreshInPlayMode())
            return;

        RebuildGround();
    }

    private bool ShouldRefreshInPlayMode()
    {
        if (_runtimeForceRefresh)
        {
            CacheRuntimeState();
            _runtimeForceRefresh = false;
            return true;
        }

        bool refsChanged =
            _runtimePointARef != PointA ||
            _runtimePointMiddleARef != PointMiddleA ||
            _runtimePointMiddleBRef != PointMiddleB ||
            _runtimePointBRef != PointB ||
            _runtimeGroundRef != GroundInstance;
        if (refsChanged)
        {
            CacheRuntimeState();
            return true;
        }

        if (PointA == null || PointMiddleA == null || PointMiddleB == null || PointB == null || GroundInstance == null)
            return false;

        Vector2 pointA = PointA.position;
        Vector2 pointMiddleA = PointMiddleA.position;
        Vector2 pointMiddleB = PointMiddleB.position;
        Vector2 pointB = PointB.position;
        float middleZ = GetMiddleZ(PointMiddleA, PointMiddleB);
        bool changed =
            !_hasRuntimeState ||
            (pointA - _runtimePointAPos).sqrMagnitude > RuntimeEpsilon ||
            (pointMiddleA - _runtimePointMiddleAPos).sqrMagnitude > RuntimeEpsilon ||
            (pointMiddleB - _runtimePointMiddleBPos).sqrMagnitude > RuntimeEpsilon ||
            (pointB - _runtimePointBPos).sqrMagnitude > RuntimeEpsilon ||
            Mathf.Abs(middleZ - _runtimeMiddleZ) > RuntimeEpsilon ||
            Mathf.Abs(CurveThickness - _runtimeCurveThickness) > RuntimeEpsilon ||
            SegmentCount != _runtimeSegmentCount ||
            Mathf.Abs(DoubleSidedGap - _runtimeDoubleSidedGap) > RuntimeEpsilon ||
            GroundMaterial != _runtimeMaterial;

        if (changed)
            CacheRuntimeState();

        return changed;
    }

    private void CacheRuntimeState()
    {
        _runtimePointARef = PointA;
        _runtimePointMiddleARef = PointMiddleA;
        _runtimePointMiddleBRef = PointMiddleB;
        _runtimePointBRef = PointB;
        _runtimeGroundRef = GroundInstance;
        _runtimeMaterial = GroundMaterial;
        _runtimePointAPos = PointA != null ? (Vector2)PointA.position : Vector2.zero;
        _runtimePointMiddleAPos = PointMiddleA != null ? (Vector2)PointMiddleA.position : Vector2.zero;
        _runtimePointMiddleBPos = PointMiddleB != null ? (Vector2)PointMiddleB.position : Vector2.zero;
        _runtimePointBPos = PointB != null ? (Vector2)PointB.position : Vector2.zero;
        _runtimeMiddleZ = GetMiddleZ(PointMiddleA, PointMiddleB);
        _runtimeCurveThickness = CurveThickness;
        _runtimeSegmentCount = SegmentCount;
        _runtimeDoubleSidedGap = DoubleSidedGap;
        _hasRuntimeState = true;
    }

    private void EnsureStructure()
    {
        if (GroundRoot == null)
        {
            Transform existing = transform.Find("GroundRoot");
            if (existing != null)
            {
                GroundRoot = existing;
            }
            else
            {
                var rootGo = new GameObject("GroundRoot");
                GroundRoot = rootGo.transform;
                GroundRoot.SetParent(transform, false);
            }
        }

        if (PointA == null)
        {
            Transform existing = transform.Find("PointA");
            if (existing != null)
            {
                PointA = existing;
            }
            else
            {
                var pointGo = new GameObject("PointA");
                PointA = pointGo.transform;
                PointA.SetParent(transform, false);
                PointA.localPosition = new Vector3(-2f, 0f, 0f);
            }
        }

        if (PointMiddleA == null)
        {
            Transform existing = transform.Find("PointMiddleA");
            if (existing != null)
            {
                PointMiddleA = existing;
            }
            else
            {
                var pointGo = new GameObject("PointMiddleA");
                PointMiddleA = pointGo.transform;
                PointMiddleA.SetParent(transform, false);
                PointMiddleA.localPosition = new Vector3(-0.7f, 1f, 0f);
            }
        }

        if (PointMiddleB == null)
        {
            Transform existing = transform.Find("PointMiddleB");
            if (existing != null)
            {
                PointMiddleB = existing;
            }
            else
            {
                var pointGo = new GameObject("PointMiddleB");
                PointMiddleB = pointGo.transform;
                PointMiddleB.SetParent(transform, false);
                PointMiddleB.localPosition = new Vector3(0.7f, 1f, 0f);
            }
        }

        if (PointB == null)
        {
            Transform existing = transform.Find("PointB");
            if (existing != null)
            {
                PointB = existing;
            }
            else
            {
                var pointGo = new GameObject("PointB");
                PointB = pointGo.transform;
                PointB.SetParent(transform, false);
                PointB.localPosition = new Vector3(2f, 0f, 0f);
            }
        }
    }

    private void EnsureGroundInstance()
    {
        if (GroundRoot == null) return;

        if (GroundInstance == null)
        {
            Transform existing = GroundRoot.Find("CubicBezierGround");
            if (existing != null)
                GroundInstance = existing;
        }

        if (GroundInstance == null)
        {
            var go = new GameObject("CubicBezierGround");
            GroundInstance = go.transform;
            GroundInstance.SetParent(GroundRoot, false);
        }

        _meshFilter = GroundInstance.GetComponent<MeshFilter>();
        if (_meshFilter == null) _meshFilter = GroundInstance.gameObject.AddComponent<MeshFilter>();

        _meshRenderer = GroundInstance.GetComponent<MeshRenderer>();
        if (_meshRenderer == null) _meshRenderer = GroundInstance.gameObject.AddComponent<MeshRenderer>();

        _polygonCollider = GroundInstance.GetComponent<PolygonCollider2D>();
        if (_polygonCollider == null) _polygonCollider = GroundInstance.gameObject.AddComponent<PolygonCollider2D>();

        var rb = GroundInstance.GetComponent<Rigidbody2D>();
        if (rb == null) rb = GroundInstance.gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        if (GroundMaterial != null)
            _meshRenderer.sharedMaterial = GroundMaterial;
        else if (_meshRenderer.sharedMaterial == null)
            _meshRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "AutoCubicBezierGroundMesh" };
            _meshFilter.sharedMesh = _mesh;
        }
        else if (_meshFilter.sharedMesh != _mesh)
        {
            _meshFilter.sharedMesh = _mesh;
        }
    }

    private void RebuildGround()
    {
        if (PointA == null || PointMiddleA == null || PointMiddleB == null || PointB == null) return;
        if (GroundInstance == null || _meshFilter == null || _meshRenderer == null || _polygonCollider == null) return;

        if (GroundMaterial != null && _meshRenderer.sharedMaterial != GroundMaterial)
            _meshRenderer.sharedMaterial = GroundMaterial;

        Vector2 p0 = PointA.position;
        Vector2 p1 = PointMiddleA.position;
        Vector2 p2 = PointMiddleB.position;
        Vector2 p3 = PointB.position;
        float middleZ = GetMiddleZ(PointMiddleA, PointMiddleB);

        int segments = Mathf.Max(4, SegmentCount);
        int stripVertCount = (segments + 1) * 2;
        int frontOffset = 0;
        int backOffset = stripVertCount;
        int vertCount = stripVertCount * 2;
        var verts = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        var tris = new int[segments * 12];
        var colliderPath = new Vector2[(segments + 1) * 2];

        float halfThickness = CurveThickness * 0.5f;
        float halfGap = DoubleSidedGap * 0.5f;
        Vector2 lastTangent = p3 - p0;
        if (lastTangent.sqrMagnitude < 1e-8f)
            lastTangent = Vector2.right;

        Transform inst = GroundInstance;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 point = EvaluateCubic(p0, p1, p2, p3, t);
            Vector2 tangent = EvaluateCubicDerivative(p0, p1, p2, p3, t);
            if (tangent.sqrMagnitude < 1e-8f)
                tangent = lastTangent;
            else
                lastTangent = tangent;

            Vector2 normal = new Vector2(-tangent.y, tangent.x).normalized;
            if (normal.sqrMagnitude < 1e-8f)
                normal = Vector2.up;

            Vector2 outer = point + normal * halfThickness;
            Vector2 inner = point - normal * halfThickness;

            int outerIdx = i * 2;
            int innerIdx = outerIdx + 1;
            Vector3 outerFront = inst.InverseTransformPoint(new Vector3(outer.x, outer.y, middleZ + halfGap));
            Vector3 innerFront = inst.InverseTransformPoint(new Vector3(inner.x, inner.y, middleZ + halfGap));
            Vector3 outerBack = inst.InverseTransformPoint(new Vector3(outer.x, outer.y, middleZ - halfGap));
            Vector3 innerBack = inst.InverseTransformPoint(new Vector3(inner.x, inner.y, middleZ - halfGap));

            verts[frontOffset + outerIdx] = outerFront;
            verts[frontOffset + innerIdx] = innerFront;
            verts[backOffset + outerIdx] = outerBack;
            verts[backOffset + innerIdx] = innerBack;

            Vector2 uvOuter = new Vector2(t, 1f);
            Vector2 uvInner = new Vector2(t, 0f);
            uvs[frontOffset + outerIdx] = uvOuter;
            uvs[frontOffset + innerIdx] = uvInner;
            uvs[backOffset + outerIdx] = uvOuter;
            uvs[backOffset + innerIdx] = uvInner;
        }

        FillStripTriangles(tris, segments, frontOffset, backOffset);

        int pathIdx = 0;
        for (int i = 0; i <= segments; i++)
        {
            int idx = i * 2;
            colliderPath[pathIdx++] = new Vector2(verts[frontOffset + idx].x, verts[frontOffset + idx].y);
        }
        for (int i = segments; i >= 0; i--)
        {
            int idx = i * 2 + 1;
            colliderPath[pathIdx++] = new Vector2(verts[frontOffset + idx].x, verts[frontOffset + idx].y);
        }

        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.uv = uvs;
        _mesh.triangles = tris;
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();

        _polygonCollider.pathCount = 1;
        _polygonCollider.SetPath(0, colliderPath);
        _meshRenderer.enabled = true;
        _polygonCollider.enabled = true;
    }

    private static float GetMiddleZ(Transform middleA, Transform middleB)
    {
        if (middleA == null && middleB == null) return 0f;
        if (middleA == null) return middleB.position.z;
        if (middleB == null) return middleA.position.z;
        return (middleA.position.z + middleB.position.z) * 0.5f;
    }

    private static Vector2 EvaluateCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float tt = t * t;
        return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
    }

    private static Vector2 EvaluateCubicDerivative(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float tt = t * t;
        return 3f * uu * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * tt * (p3 - p2);
    }

    private static void FillStripTriangles(int[] tris, int segments, int frontOffset, int backOffset)
    {
        int tri = 0;
        for (int i = 0; i < segments; i++)
        {
            int i0 = i * 2;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;

            tris[tri++] = frontOffset + i0;
            tris[tri++] = frontOffset + i2;
            tris[tri++] = frontOffset + i1;
            tris[tri++] = frontOffset + i2;
            tris[tri++] = frontOffset + i3;
            tris[tri++] = frontOffset + i1;

            tris[tri++] = backOffset + i1;
            tris[tri++] = backOffset + i2;
            tris[tri++] = backOffset + i0;
            tris[tri++] = backOffset + i1;
            tris[tri++] = backOffset + i3;
            tris[tri++] = backOffset + i2;
        }
    }
}
