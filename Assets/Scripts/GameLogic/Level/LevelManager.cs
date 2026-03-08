using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour, IButtonReceiver
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Config")]
    public int LevelIndex;

    [Header("Scene References")]
    public Transform BuildAreaCenter;
    public Vector2 BuildAreaSize = new Vector2(10f, 8f);
    public Transform CameraAnchor;

    [Header("Inventory")]
    public Rect InventoryArea = new Rect(-5f, -8f, 10f, 3f);
    public List<GameObject> InventoryNodePrefabs = new List<GameObject>();

    [Header("UI Prefab Settings")]
    public Color ExitButtonColor = Color.white;
    public Color StartButtonColor = Color.green;
    public Color StopButtonColor = Color.red;
    public Color NextButtonColor = Color.green;
    public float UIButtonSize = 0.8f;

    public LevelState CurrentState { get; private set; } = LevelState.Build;

    private BlueprintData _memoryBlueprint;
    private Camera _mainCamera;
    private ConnectionManager _connMgr;

    private GameButton _actionButton;
    private GameButton _exitButton;

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
        LoadBlueprintFromDisk();
        EnterBuildMode();
    }

    private void GenerateUI()
    {
        _exitButton = CreateUIButton(
            "ExitButton", "Exit", ButtonShape.TriangleLeft,
            ExitButtonColor, GetScreenCornerWorld(ScreenCorner.TopLeft));

        _actionButton = CreateUIButton(
            "ActionButton", "Start", ButtonShape.Circle,
            StartButtonColor, GetScreenCornerWorld(ScreenCorner.BottomRight));
    }

    private GameButton CreateUIButton(string name, string buttonName, ButtonShape shape, Color color, Vector3 worldPos)
    {
        var go = new GameObject(name);
        go.transform.position = worldPos;

        var btn = go.AddComponent<GameButton>();
        btn.ButtonName = buttonName;
        btn.Shape = shape;
        btn.ButtonColor = color;
        btn.ButtonSize = Vector2.one * UIButtonSize;
        btn.SetReceiver(this);

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = UIButtonSize * 0.5f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = color;
        sr.sortingOrder = 100;

        return btn;
    }

    public bool OnButtonDown(string buttonName)
    {
        switch (buttonName)
        {
            case "Exit":
                SaveBlueprintToDisk();
                SceneManager.LoadScene(GameConstants.LevelSelectSceneName);
                return true;

            case "Start":
                if (CurrentState == LevelState.Build)
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

        foreach (var node in _connMgr.AllNodes)
        {
            node.EnterBuildMode();
            var drag = node.GetComponent<NodeDragHandler>();
            if (drag != null)
            {
                drag.BuildArea = GetBuildAreaRect();
                drag.InventoryArea = InventoryArea;
            }
        }

        SetActionButton("Start", ButtonShape.Circle, StartButtonColor);

        if (CameraAnchor != null && _mainCamera != null)
        {
            _mainCamera.transform.position = CameraAnchor.position;
        }

        var camCtrl = _mainCamera.GetComponent<RuntimeCameraController>();
        if (camCtrl != null) camCtrl.enabled = false;
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

        SetActionButton("Stop", ButtonShape.Square, StopButtonColor);

        var camCtrl = _mainCamera.GetComponent<RuntimeCameraController>();
        if (camCtrl != null) camCtrl.enabled = true;
    }

    public void EnterVictoryMode()
    {
        if (CurrentState == LevelState.Victory) return;

        CurrentState = LevelState.Victory;
        _connMgr.CurrentState = LevelState.Victory;

        SaveManager.Instance.CompleteLevel(LevelIndex);

        SetActionButton("Next", ButtonShape.TriangleRight, NextButtonColor);
    }

    private void SetActionButton(string buttonName, ButtonShape shape, Color color)
    {
        if (_actionButton == null) return;
        _actionButton.ButtonName = buttonName;
        _actionButton.Shape = shape;
        _actionButton.ButtonColor = color;

        var sr = _actionButton.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = color;
    }

    private Rect GetBuildAreaRect()
    {
        if (BuildAreaCenter == null) return new Rect(-5, -4, 10, 8);
        Vector2 center = BuildAreaCenter.position;
        return new Rect(
            center.x - BuildAreaSize.x * 0.5f,
            center.y - BuildAreaSize.y * 0.5f,
            BuildAreaSize.x,
            BuildAreaSize.y);
    }

    private BlueprintData CaptureBlueprint()
    {
        var data = new BlueprintData();
        var nodeIndexMap = new Dictionary<Node, int>();
        int idx = 0;

        foreach (var node in _connMgr.AllNodes)
        {
            if (node.IsInInventory) continue;
            nodeIndexMap[node] = idx++;
            data.nodes.Add(new NodeData
            {
                nodeType = node.NodeType,
                posX = node.transform.position.x,
                posY = node.transform.position.y
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
        foreach (var nd in bp.nodes)
        {
            GameObject prefab = FindPrefabByType(nd.nodeType);
            if (prefab == null) continue;

            var go = Instantiate(prefab, new Vector3(nd.posX, nd.posY, 0f), Quaternion.identity);
            var node = go.GetComponent<Node>();
            if (node == null) continue;

            node.IsInInventory = false;
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

    private GameObject FindPrefabByType(string typeName)
    {
        foreach (var prefab in InventoryNodePrefabs)
        {
            var node = prefab.GetComponent<Node>();
            if (node != null && node.NodeType == typeName)
                return prefab;
        }
        return InventoryNodePrefabs.Count > 0 ? InventoryNodePrefabs[0] : null;
    }

    private void SaveBlueprintToDisk()
    {
        var bp = _memoryBlueprint ?? CaptureBlueprint();
        BlueprintData.SaveBlueprint(LevelIndex, bp);
    }

    private void LoadBlueprintFromDisk()
    {
        _memoryBlueprint = BlueprintData.LoadBlueprint(LevelIndex);
    }

    private enum ScreenCorner { TopLeft, TopRight, BottomLeft, BottomRight }

    private Vector3 GetScreenCornerWorld(ScreenCorner corner)
    {
        if (_mainCamera == null) return Vector3.zero;
        float margin = UIButtonSize;
        switch (corner)
        {
            case ScreenCorner.TopLeft:
                return _mainCamera.ScreenToWorldPoint(new Vector3(margin * 50f, Screen.height - margin * 50f, 10f));
            case ScreenCorner.BottomRight:
                return _mainCamera.ScreenToWorldPoint(new Vector3(Screen.width - margin * 50f, margin * 50f, 10f));
            default:
                return Vector3.zero;
        }
    }
}
