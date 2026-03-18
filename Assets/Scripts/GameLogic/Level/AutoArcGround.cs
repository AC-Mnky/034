using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class AutoArcGround : MonoBehaviour
{
    [Header("Arc Points")]
    public Transform PointA;
    public Transform PointMiddle;
    public Transform PointB;

    [Header("Ground Hierarchy")]
    public Transform GroundRoot;
    public Transform GroundInstance;

    [Header("Arc Mesh")]
    [Min(0.01f)] public float ArcThickness = 0.6f;
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
    private Transform _runtimePointMiddleRef;
    private Transform _runtimePointBRef;
    private Transform _runtimeGroundRef;
    private Material _runtimeMaterial;
    private Vector2 _runtimePointAPos;
    private Vector2 _runtimePointMiddlePos;
    private Vector2 _runtimePointBPos;
    private float _runtimeArcThickness;
    private int _runtimeSegmentCount;
    private float _runtimeDoubleSidedGap;

    private void Reset()
    {
        EnsureStructure();
        EnsureGroundInstance();
        RebuildArcGround();
    }

    private void Awake()
    {
        EnsureStructure();
        EnsureGroundInstance();
        RebuildArcGround();
    }

    private void OnValidate()
    {
        EnsureStructure();
        EnsureGroundInstance();
        RebuildArcGround();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            RebuildArcGround();
            return;
        }

        if (!ShouldRefreshInPlayMode())
            return;

        RebuildArcGround();
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
            _runtimePointMiddleRef != PointMiddle ||
            _runtimePointBRef != PointB ||
            _runtimeGroundRef != GroundInstance;
        if (refsChanged)
        {
            CacheRuntimeState();
            return true;
        }

        if (PointA == null || PointMiddle == null || PointB == null || GroundInstance == null)
            return false;

        Vector2 pointA = PointA.position;
        Vector2 pointMiddle = PointMiddle.position;
        Vector2 pointB = PointB.position;
        bool changed =
            !_hasRuntimeState ||
            (pointA - _runtimePointAPos).sqrMagnitude > RuntimeEpsilon ||
            (pointMiddle - _runtimePointMiddlePos).sqrMagnitude > RuntimeEpsilon ||
            (pointB - _runtimePointBPos).sqrMagnitude > RuntimeEpsilon ||
            Mathf.Abs(ArcThickness - _runtimeArcThickness) > RuntimeEpsilon ||
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
        _runtimePointMiddleRef = PointMiddle;
        _runtimePointBRef = PointB;
        _runtimeGroundRef = GroundInstance;
        _runtimeMaterial = GroundMaterial;
        _runtimePointAPos = PointA != null ? (Vector2)PointA.position : Vector2.zero;
        _runtimePointMiddlePos = PointMiddle != null ? (Vector2)PointMiddle.position : Vector2.zero;
        _runtimePointBPos = PointB != null ? (Vector2)PointB.position : Vector2.zero;
        _runtimeArcThickness = ArcThickness;
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

        if (PointMiddle == null)
        {
            Transform existing = transform.Find("PointMiddle");
            if (existing != null)
            {
                PointMiddle = existing;
            }
            else
            {
                var pointGo = new GameObject("PointMiddle");
                PointMiddle = pointGo.transform;
                PointMiddle.SetParent(transform, false);
                PointMiddle.localPosition = new Vector3(0f, 1f, 0f);
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
            Transform existing = GroundRoot.Find("ArcGround");
            if (existing != null)
                GroundInstance = existing;
        }

        if (GroundInstance == null)
        {
            var go = new GameObject("ArcGround");
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
            _mesh = new Mesh { name = "AutoArcGroundMesh" };
            _meshFilter.sharedMesh = _mesh;
        }
        else if (_meshFilter.sharedMesh != _mesh)
        {
            _meshFilter.sharedMesh = _mesh;
        }
    }

    private void RebuildArcGround()
    {
        if (PointA == null || PointMiddle == null || PointB == null) return;
        if (GroundInstance == null || _meshFilter == null || _meshRenderer == null || _polygonCollider == null) return;

        if (GroundMaterial != null && _meshRenderer.sharedMaterial != GroundMaterial)
            _meshRenderer.sharedMaterial = GroundMaterial;

        Vector2 a = PointA.position;
        Vector2 m = PointMiddle.position;
        Vector2 b = PointB.position;

        if (!TryGetCircumcenter(a, m, b, out Vector2 center))
        {
            _mesh.Clear();
            _polygonCollider.pathCount = 0;
            _meshRenderer.enabled = false;
            _polygonCollider.enabled = false;
            return;
        }

        float radius = Vector2.Distance(center, a);
        float halfThickness = ArcThickness * 0.5f;
        float innerRadius = Mathf.Max(0.01f, radius - halfThickness);
        float outerRadius = radius + halfThickness;

        float startAngle = Mathf.Atan2(a.y - center.y, a.x - center.x);
        float middleAngle = Mathf.Atan2(m.y - center.y, m.x - center.x);
        float endAngle = Mathf.Atan2(b.y - center.y, b.x - center.x);
        float span = GetSignedSpanThroughMiddle(startAngle, middleAngle, endAngle);

        int segments = Mathf.Max(4, SegmentCount);
        int stripVertCount = (segments + 1) * 2;
        int frontOffset = 0;
        int backOffset = stripVertCount;
        int vertCount = stripVertCount * 2;
        var verts = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        var tris = new int[segments * 12];
        var colliderPath = new Vector2[(segments + 1) * 2];
        Transform inst = GroundInstance;
        float halfGap = DoubleSidedGap * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = startAngle + span * t;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            Vector2 outer = center + dir * outerRadius;
            Vector2 inner = center + dir * innerRadius;

            int outerIdx = i * 2;
            int innerIdx = outerIdx + 1;
            Vector3 outerFront = inst.InverseTransformPoint(new Vector3(outer.x, outer.y, halfGap));
            Vector3 innerFront = inst.InverseTransformPoint(new Vector3(inner.x, inner.y, halfGap));
            Vector3 outerBack = inst.InverseTransformPoint(new Vector3(outer.x, outer.y, -halfGap));
            Vector3 innerBack = inst.InverseTransformPoint(new Vector3(inner.x, inner.y, -halfGap));

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

            // Back face (reversed winding) for double-sided rendering.
            tris[tri++] = backOffset + i1;
            tris[tri++] = backOffset + i2;
            tris[tri++] = backOffset + i0;

            tris[tri++] = backOffset + i1;
            tris[tri++] = backOffset + i3;
            tris[tri++] = backOffset + i2;
        }

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

    private static bool TryGetCircumcenter(Vector2 a, Vector2 m, Vector2 b, out Vector2 center)
    {
        float d = 2f * (a.x * (m.y - b.y) + m.x * (b.y - a.y) + b.x * (a.y - m.y));
        if (Mathf.Abs(d) < 1e-5f)
        {
            center = Vector2.zero;
            return false;
        }

        float aSq = a.sqrMagnitude;
        float mSq = m.sqrMagnitude;
        float bSq = b.sqrMagnitude;
        float ux = (aSq * (m.y - b.y) + mSq * (b.y - a.y) + bSq * (a.y - m.y)) / d;
        float uy = (aSq * (b.x - m.x) + mSq * (a.x - b.x) + bSq * (m.x - a.x)) / d;
        center = new Vector2(ux, uy);
        return true;
    }

    private static float GetSignedSpanThroughMiddle(float startAngle, float middleAngle, float endAngle)
    {
        float shortSpan = SignedDelta(startAngle, endAngle);
        float toMiddle = SignedDelta(startAngle, middleAngle);
        if (IsAlongSpan(toMiddle, shortSpan))
            return shortSpan;

        return shortSpan > 0f ? shortSpan - Mathf.PI * 2f : shortSpan + Mathf.PI * 2f;
    }

    private static float SignedDelta(float from, float to)
    {
        return Mathf.Atan2(Mathf.Sin(to - from), Mathf.Cos(to - from));
    }

    private static bool IsAlongSpan(float deltaToCheck, float span)
    {
        const float eps = 1e-4f;
        if (span >= 0f)
            return deltaToCheck >= -eps && deltaToCheck <= span + eps;
        return deltaToCheck <= eps && deltaToCheck >= span - eps;
    }
}
