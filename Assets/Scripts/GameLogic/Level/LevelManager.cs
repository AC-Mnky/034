using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour, IButtonReceiver
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Config")]
    public int LevelIndex;

    [Header("Scene References")]
    public List<Transform> BuildAreaVertices = new List<Transform>();
    public Transform CameraAnchor;

    [Header("Inventory (offset relative to CameraAnchor)")]
    public Rect InventoryAreaOffset = new Rect(-5f, -8f, 10f, 3f);

    [Header("Initial Blueprint")]
    public BlueprintData InitialBlueprint = new BlueprintData();

    [Header("UI Settings")]
    public float UIButtonSize = 0.8f;

    public LevelState CurrentState { get; private set; } = LevelState.Build;

    private BlueprintData _memoryBlueprint;
    private Camera _mainCamera;
    private ConnectionManager _connMgr;

    private GameButton _actionButton;
    private GameButton _exitButton;
    private GameObject _inventoryBg;
    private GameObject _buildAreaBg;
    private Canvas _uiCanvas;
    private bool _startButtonInteractable = true;

    private void Awake()
    {
        Instance = this;
        _mainCamera = Camera.main;

        if (ConnectionManager.Instance == null)
        {
            var go = new GameObject("ConnectionManager");
            go.AddComponent<ConnectionManager>();
        }
        _connMgr = ConnectionManager.Instance;

        GenerateUI();
        SnapCameraToBuildPosition();
        LoadOrInitBlueprint();
        EnterBuildMode();
    }

    private void Update()
    {
        if (CurrentState == LevelState.Build)
        {
            if (Input.GetKeyDown(KeyCode.R))
                ResetToInitialBlueprint();

            UpdateStartButtonState();
        }
    }

    private void UpdateStartButtonState()
    {
        var buildPoly = GetBuildAreaPolygon();
        bool allInBuildArea = true;
        List<(Node, Node)> previewConns = null;

        foreach (var node in _connMgr.AllNodes)
        {
            if (!node.gameObject.activeSelf) continue;

            var drag = node.GetComponent<NodeDragHandler>();
            if (drag != null && drag.IsDragging)
                previewConns = drag.PreviewConnections;

            if (node.IsInInventory || !PointInPolygon(node.transform.position, buildPoly))
                allInBuildArea = false;
        }

        bool canStart = allInBuildArea && _connMgr.AreAllNodesConnected(previewConns);
        if (canStart == _startButtonInteractable) return;

        _startButtonInteractable = canStart;
        if (_actionButton == null) return;
        _actionButton.ButtonColor = canStart
            ? ColorConfig.Instance.StartButtonColor
            : ColorConfig.Instance.DisabledButtonColor;
        _actionButton.ApplyVisual();
    }

    private void LoadOrInitBlueprint()
    {
        var loaded = BlueprintData.LoadBlueprint(LevelIndex);
        if (loaded != null)
        {
            ResolvePrefabsFromInitial(loaded);
            _memoryBlueprint = loaded;
        }
        else
        {
            _memoryBlueprint = InitialBlueprint.DeepCopy();
        }
    }

    private void ResetToInitialBlueprint()
    {
        BlueprintData.DeleteBlueprint(LevelIndex);
        _memoryBlueprint = InitialBlueprint.DeepCopy();
        RestoreFromBlueprint(_memoryBlueprint);
        SetupAllNodesForBuild();
    }

    private void ResolvePrefabsFromInitial(BlueprintData loaded)
    {
        foreach (var nd in loaded.nodes)
        {
            if (nd.prefab != null) continue;
            nd.prefab = FindPrefabByType(nd.nodeType);
        }
    }

    private void GenerateUI()
    {
        CreateInventoryBackground();
        CreateBuildAreaBackground();
        CreateCanvas();

        _exitButton = CreateUIButton(
            "ExitButton", "Exit", ButtonShape.TriangleLeft,
            ColorConfig.Instance.ExitButtonColor, new Vector2(0f, 1f), new Vector2(60f, -60f));

        _actionButton = CreateUIButton(
            "ActionButton", "Start", ButtonShape.TriangleRight,
            ColorConfig.Instance.StartButtonColor, new Vector2(1f, 0f), new Vector2(-60f, 60f));
    }

    private void CreateCanvas()
    {
        var canvasGo = new GameObject("UICanvas");
        _uiCanvas = canvasGo.AddComponent<Canvas>();
        _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _uiCanvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }
    }

    private void CreateInventoryBackground()
    {
        _inventoryBg = CreateAreaBackground("InventoryBackground",
            GetInventoryArea(), ColorConfig.Instance.InventoryBackgroundColor);
    }

    private void CreateBuildAreaBackground()
    {
        var poly = GetBuildAreaPolygon();
        if (poly.Length < 3) return;

        _buildAreaBg = new GameObject("BuildAreaBackground");
        _buildAreaBg.transform.position = new Vector3(0f, 0f, 1f);

        var mf = _buildAreaBg.AddComponent<MeshFilter>();
        var mr = _buildAreaBg.AddComponent<MeshRenderer>();

        mr.material = new Material(Shader.Find("Sprites/Default"));
        mr.material.color = ColorConfig.Instance.BuildAreaBackgroundColor;
        mr.sortingOrder = -100;

        mf.mesh = CreatePolygonMesh(poly);
    }

    private static Mesh CreatePolygonMesh(Vector2[] poly)
    {
        var mesh = new Mesh();

        var verts = new Vector3[poly.Length];
        for (int i = 0; i < poly.Length; i++)
            verts[i] = new Vector3(poly[i].x, poly[i].y, 0f);

        var tris = new int[(poly.Length - 2) * 3];
        for (int i = 0; i < poly.Length - 2; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    private GameObject CreateAreaBackground(string name, Rect area, Color color)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(
            area.x + area.width * 0.5f,
            area.y + area.height * 0.5f,
            1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = color;
        sr.sortingOrder = -100;

        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex,
            new UnityEngine.Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f), 1f);

        go.transform.localScale = new Vector3(area.width, area.height, 1f);
        return go;
    }

    private void SetBuildUIVisible(bool visible)
    {
        if (_inventoryBg != null)
            _inventoryBg.SetActive(visible);
        if (_buildAreaBg != null)
            _buildAreaBg.SetActive(visible);
    }

    private GameButton CreateUIButton(string name, string buttonName, ButtonShape shape,
        Color color, Vector2 anchor, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(_uiCanvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        float pixelSize = UIButtonSize * 100f;
        rt.sizeDelta = new Vector2(pixelSize, pixelSize);

        var image = go.AddComponent<Image>();
        image.raycastTarget = true;

        var btn = go.AddComponent<GameButton>();
        btn.ButtonName = buttonName;
        btn.Shape = shape;
        btn.ButtonColor = color;
        btn.ButtonSize = Vector2.one * UIButtonSize;
        btn.SetReceiver(this);
        btn.ApplyVisual();

        return btn;
    }

    public bool OnButtonDown(string buttonName)
    {
        switch (buttonName)
        {
            case "Exit":
                if (CurrentState == LevelState.Build)
                    _memoryBlueprint = CaptureBlueprint();
                SaveBlueprintToDisk();
                SceneManager.LoadScene(GameConstants.LevelSelectSceneName);
                return true;

            case "Start":
                if (CurrentState == LevelState.Build && _startButtonInteractable)
                    EnterRunMode();
                return true;

            case "Stop":
                if (CurrentState == LevelState.Run)
                    EnterBuildMode();
                return true;

            case "Next":
                if (CurrentState == LevelState.Victory)
                {
                    SaveBlueprintToDisk();
                    int next = LevelIndex + 1;
                    if (next >= GameConstants.TotalLevelNum)
                        SceneManager.LoadScene(GameConstants.LevelSelectSceneName);
                    else
                        SceneManager.LoadScene(GameConstants.GetLevelSceneName(next));
                }
                return true;
        }
        return false;
    }

    public void EnterBuildMode()
    {
        CurrentState = LevelState.Build;
        _connMgr.CurrentState = LevelState.Build;

        if (_memoryBlueprint != null)
        {
            RestoreFromBlueprint(_memoryBlueprint);
        }

        SetupAllNodesForBuild();
        SetActionButton("Start", ButtonShape.TriangleRight, ColorConfig.Instance.StartButtonColor);
        SetBuildUIVisible(true);

        var camCtrl = _mainCamera.GetComponent<RuntimeCameraController>();
        if (camCtrl != null && CameraAnchor != null)
        {
            camCtrl.ReturnToDefault(CameraAnchor.position);
        }
        else if (camCtrl != null)
        {
            camCtrl.enabled = false;
        }
    }

    private void SnapCameraToBuildPosition()
    {
        var camCtrl = _mainCamera.GetComponent<RuntimeCameraController>();
        if (camCtrl != null && CameraAnchor != null)
        {
            camCtrl.SnapToDefault(CameraAnchor.position);
        }
        else if (CameraAnchor != null && _mainCamera != null)
        {
            _mainCamera.transform.position = new Vector3(
                CameraAnchor.position.x, CameraAnchor.position.y,
                _mainCamera.transform.position.z);
        }
    }

    private void SetupAllNodesForBuild()
    {
        var buildPoly = GetBuildAreaPolygon();
        foreach (var node in _connMgr.AllNodes)
        {
            node.EnterBuildMode();
            var drag = node.GetComponent<NodeDragHandler>();
            if (drag != null)
            {
                drag.BuildAreaPolygon = buildPoly;
                drag.InventoryArea = GetInventoryArea();
            }
        }
    }

    public void EnterRunMode()
    {
        _memoryBlueprint = CaptureBlueprint();
        SaveBlueprintToDisk();

        CurrentState = LevelState.Run;
        _connMgr.CurrentState = LevelState.Run;
        _connMgr.InitializeAllForRun();

        foreach (var node in _connMgr.AllNodes)
        {
            node.EnterRunMode();
        }

        SetActionButton("Stop", ButtonShape.Square, ColorConfig.Instance.StopButtonColor);
        SetBuildUIVisible(false);

        var camCtrl = _mainCamera.GetComponent<RuntimeCameraController>();
        if (camCtrl != null) camCtrl.StartFollowing();
    }

    public void EnterVictoryMode()
    {
        if (CurrentState == LevelState.Victory) return;

        CurrentState = LevelState.Victory;
        _connMgr.CurrentState = LevelState.Victory;

        SaveManager.Instance.CompleteLevel(LevelIndex);

        SetActionButton("Next", ButtonShape.TriangleRight, ColorConfig.Instance.NextButtonColor);
    }

    private void SetActionButton(string buttonName, ButtonShape shape, Color color)
    {
        if (_actionButton == null) return;
        _actionButton.ButtonName = buttonName;
        _actionButton.Shape = shape;
        _actionButton.ButtonColor = color;
        _actionButton.ApplyVisual();
    }

    public Vector2[] GetBuildAreaPolygon()
    {
        if (BuildAreaVertices == null || BuildAreaVertices.Count < 3)
        {
            Debug.LogWarning("Build area vertices are not set, using default polygon");
            return new[]
            {
                new Vector2(-5, -4), new Vector2(5, -4),
                new Vector2(5, 4), new Vector2(-5, 4)
            };
        }

        var poly = new Vector2[BuildAreaVertices.Count];
        for (int i = 0; i < BuildAreaVertices.Count; i++)
        {
            poly[i] = BuildAreaVertices[i] != null
                ? (Vector2)BuildAreaVertices[i].position
                : Vector2.zero;
        }
        return poly;
    }

    public Rect GetInventoryArea()
    {
        Vector2 anchor = CameraAnchor != null ? (Vector2)CameraAnchor.position : Vector2.zero;
        return new Rect(
            InventoryAreaOffset.x + anchor.x,
            InventoryAreaOffset.y + anchor.y,
            InventoryAreaOffset.width,
            InventoryAreaOffset.height);
    }

    public static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
                    / (polygon[j].y - polygon[i].y) + polygon[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private BlueprintData CaptureBlueprint()
    {
        var data = new BlueprintData();
        var nodeIndexMap = new Dictionary<Node, int>();
        int idx = 0;

        foreach (var node in _connMgr.AllNodes)
        {
            nodeIndexMap[node] = idx++;
            data.nodes.Add(new NodeData
            {
                prefab = FindPrefabByType(node.NodeType),
                nodeType = node.NodeType,
                isInInventory = node.IsInInventory,
                posX = node.IsInInventory ? 0f : node.transform.position.x,
                posY = node.IsInInventory ? 0f : node.transform.position.y
            });
        }

        foreach (var conn in _connMgr.AllConnections)
        {
            if (nodeIndexMap.ContainsKey(conn.NodeA) && nodeIndexMap.ContainsKey(conn.NodeB))
            {
                data.connections.Add(new ConnectionData
                {
                    nodeIndexA = nodeIndexMap[conn.NodeA],
                    nodeIndexB = nodeIndexMap[conn.NodeB]
                });
            }
        }

        return data;
    }

    private void RestoreFromBlueprint(BlueprintData bp)
    {
        _connMgr.ClearAllNodes();

        if (bp == null || bp.nodes.Count == 0) return;

        var spawnedNodes = new List<Node>();
        int inventoryIndex = 0;

        foreach (var nd in bp.nodes)
        {
            GameObject prefab = nd.prefab != null ? nd.prefab : FindPrefabByType(nd.nodeType);
            if (prefab == null) continue;

            Vector3 spawnPos;
            if (nd.isInInventory)
            {
                spawnPos = GetInventorySlotPosition(inventoryIndex);
                inventoryIndex++;
            }
            else
            {
                spawnPos = new Vector3(nd.posX, nd.posY, 0f);
            }

            var go = Instantiate(prefab, spawnPos, Quaternion.identity);
            var node = go.GetComponent<Node>();
            if (node == null) continue;

            node.IsInInventory = nd.isInInventory;
            _connMgr.RegisterNode(node);
            spawnedNodes.Add(node);
        }

        foreach (var cd in bp.connections)
        {
            if (cd.nodeIndexA < spawnedNodes.Count && cd.nodeIndexB < spawnedNodes.Count)
            {
                _connMgr.AddConnection(spawnedNodes[cd.nodeIndexA], spawnedNodes[cd.nodeIndexB]);
            }
        }
    }

    private Vector3 GetInventorySlotPosition(int index)
    {
        Rect inv = GetInventoryArea();
        float spacing = 1.2f;
        float startX = inv.x + inv.width * 0.5f;
        float y = inv.y + inv.height * 0.5f;

        int totalInventory = CountInventoryNodes();
        float totalWidth = (totalInventory - 1) * spacing;
        float x = startX - totalWidth * 0.5f + index * spacing;

        return new Vector3(x, y, 0f);
    }

    private int CountInventoryNodes()
    {
        if (_memoryBlueprint == null) return 0;
        int count = 0;
        foreach (var nd in _memoryBlueprint.nodes)
        {
            if (nd.isInInventory) count++;
        }
        return count;
    }

    private GameObject FindPrefabByType(string typeName)
    {
        foreach (var nd in InitialBlueprint.nodes)
        {
            if (nd.prefab != null)
            {
                var nodeComp = nd.prefab.GetComponent<Node>();
                if (nodeComp != null && nodeComp.NodeType == typeName)
                    return nd.prefab;
            }
        }
        if (InitialBlueprint.nodes.Count > 0 && InitialBlueprint.nodes[0].prefab != null)
            return InitialBlueprint.nodes[0].prefab;
        return null;
    }

    private void SaveBlueprintToDisk()
    {
        var bp = _memoryBlueprint ?? CaptureBlueprint();
        BlueprintData.SaveBlueprint(LevelIndex, bp);
    }

    private void OnDrawGizmos()
    {
        if (BuildAreaVertices == null || BuildAreaVertices.Count < 3) return;

        Gizmos.color = new Color(ColorConfig.Instance.BuildAreaBackgroundColor.r,
            ColorConfig.Instance.BuildAreaBackgroundColor.g,
            ColorConfig.Instance.BuildAreaBackgroundColor.b, 0.8f);

        for (int i = 0; i < BuildAreaVertices.Count; i++)
        {
            if (BuildAreaVertices[i] == null) continue;
            int next = (i + 1) % BuildAreaVertices.Count;
            if (BuildAreaVertices[next] == null) continue;

            Gizmos.DrawLine(BuildAreaVertices[i].position, BuildAreaVertices[next].position);
        }

        Gizmos.color = Color.white;
        foreach (var v in BuildAreaVertices)
        {
            if (v != null)
                Gizmos.DrawWireSphere(v.position, 0.15f);
        }

        Gizmos.color = new Color(ColorConfig.Instance.InventoryBackgroundColor.r,
            ColorConfig.Instance.InventoryBackgroundColor.g,
            ColorConfig.Instance.InventoryBackgroundColor.b, 0.8f);
        Rect inv = GetInventoryArea();
        Vector3 invCenter = new Vector3(
            inv.x + inv.width * 0.5f,
            inv.y + inv.height * 0.5f, 0f);
        Vector3 invSize = new Vector3(inv.width, inv.height, 0f);
        Gizmos.DrawWireCube(invCenter, invSize);
    }
}
