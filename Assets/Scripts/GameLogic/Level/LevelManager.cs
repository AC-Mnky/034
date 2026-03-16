using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour, IButtonReceiver, IButtonHoverReceiver
{
    public static LevelManager Instance { get; private set; }
    public static bool IsInputLocked { get; private set; }

    [Header("Scene References")]
    public List<Transform> BuildAreaVertices = new List<Transform>();
    public Transform IntroCameraAnchor;
    public Transform CameraAnchor;
    [Tooltip("Optional roots used to compute intro camera bounds. If empty, auto-detect map renderers/colliders in scene.")]
    public List<Transform> IntroBoundsRoots = new List<Transform>();

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
    private LevelAreaVisualController _areaVisualController;
    private LevelIntroSequenceController _introSequenceController;
    private LevelPartAppearController _partAppearController;

    private GameButton _actionButton;
    private GameButton _exitButton;
    private GameButton _previewButton;
    private Canvas _uiCanvas;
    private bool _startButtonInteractable = true;
    private string CurrentSceneName => SceneManager.GetActiveScene().name;
    private Coroutine _buildIntroRoutine;
    private Coroutine _hoverRecoverRoutine;
    private bool _buildIntroFinished;
    private bool _isHoverPreviewActive;
    private readonly List<GoalTrigger> _goalTriggers = new List<GoalTrigger>();

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
        _areaVisualController = GetComponent<LevelAreaVisualController>();
        if (_areaVisualController == null) _areaVisualController = gameObject.AddComponent<LevelAreaVisualController>();
        _introSequenceController = GetComponent<LevelIntroSequenceController>();
        if (_introSequenceController == null) _introSequenceController = gameObject.AddComponent<LevelIntroSequenceController>();
        _partAppearController = GetComponent<LevelPartAppearController>();
        if (_partAppearController == null) _partAppearController = gameObject.AddComponent<LevelPartAppearController>();
        RefreshGoalTriggers();

        GenerateUI();
        RefreshAreaVisuals();
        SnapCameraToBuildPosition();
        LoadOrInitBlueprint();
        EnterBuildMode();
        BeginBuildIntroSequence();
    }

    private void Update()
    {
        if (CurrentState == LevelState.Build)
        {
            if (IsInputLocked) return;

            if (Input.GetKeyDown(KeyCode.R))
                ResetToInitialBlueprint();

            UpdateStartButtonState();
        }
    }

    private void UpdateStartButtonState(bool forceRefresh = false)
    {
        var buildPoly = GetBuildAreaPolygon();
        bool allPlacedInBuildArea = true;
        List<(Node, Node)> previewConns = null;

        foreach (var node in _connMgr.AllNodes)
        {
            if (!node.gameObject.activeSelf) continue;

            var drag = node.GetComponent<NodeDragHandler>();
            if (drag != null && drag.IsDragging)
                previewConns = drag.PreviewConnections;

            if (node.IsInInventory) continue;
            if (!PointInPolygon(node.transform.position, buildPoly))
                allPlacedInBuildArea = false;
        }

        bool canStart = allPlacedInBuildArea && _connMgr.AreAllNodesConnected(previewConns);
        if (!forceRefresh && canStart == _startButtonInteractable) return;

        _startButtonInteractable = canStart;
        if (_actionButton == null) return;
        _actionButton.ButtonColor = canStart
            ? ColorConfig.Instance.StartButtonColor
            : ColorConfig.Instance.DisabledButtonColor;
        _actionButton.ApplyVisual();
    }

    private void LoadOrInitBlueprint()
    {
        var loaded = BlueprintData.LoadBlueprint(CurrentSceneName);
        if (loaded != null)
        {
            _memoryBlueprint = loaded;
        }
        else
        {
            _memoryBlueprint = InitialBlueprint.DeepCopy();
        }
    }

    private void ResetToInitialBlueprint()
    {
        BlueprintData.DeleteBlueprint(CurrentSceneName);
        _memoryBlueprint = InitialBlueprint.DeepCopy();
        RestoreFromBlueprint(_memoryBlueprint);
        SetupAllNodesForBuild();
        BeginBuildIntroSequence();
    }

    private void GenerateUI()
    {
        CreateCanvas();

        _exitButton = CreateUIButton(
            "ExitButton", "Exit", ButtonShape.TriangleLeft,
            ColorConfig.Instance.ExitButtonColor, new Vector2(0f, 1f), new Vector2(60f, -60f));

        _actionButton = CreateUIButton(
            "ActionButton", "Start", ButtonShape.TriangleRight,
            ColorConfig.Instance.StartButtonColor, new Vector2(1f, 0f), new Vector2(-60f, 60f));

        _previewButton = CreateUIButton(
            "PreviewButton", "Preview", ButtonShape.Square,
            Color.white, new Vector2(1f, 1f), new Vector2(-60f, -60f));
        _previewButton.gameObject.SetActive(false);
    }

    private void RefreshAreaVisuals()
    {
        if (_areaVisualController == null) return;
        _areaVisualController.BuildVisuals(GetBuildAreaPolygon(), GetInventoryArea());
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

    private void SetBuildUIVisible(bool visible)
    {
        if (_areaVisualController != null)
            _areaVisualController.SetVisible(visible);
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
        bool allowActionButtonDuringPreview =
            _isHoverPreviewActive &&
            _actionButton != null &&
            buttonName == _actionButton.ButtonName;
        bool allowStartDuringBuildIntro =
            IsInputLocked &&
            CurrentState == LevelState.Build &&
            buttonName == "Start";

        if (IsInputLocked && buttonName != "Exit" && !allowActionButtonDuringPreview && !allowStartDuringBuildIntro)
            return true;

        switch (buttonName)
        {
            case "Exit":
                if (CurrentState == LevelState.Build)
                    _memoryBlueprint = CaptureBlueprint();
                SaveBlueprintToDisk();
                SceneManager.LoadScene(GameConfig.Instance.LevelSelectSceneName);
                return true;

            case "Start":
                UpdateStartButtonState(true);
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
                    string next = LevelTopologyRuntime.GetNextScene(CurrentSceneName);
                    if (string.IsNullOrWhiteSpace(next))
                        SceneManager.LoadScene(GameConfig.Instance.LevelSelectSceneName);
                    else
                        SceneManager.LoadScene(next);
                }
                return true;

            case "Preview":
                return true;
        }
        return false;
    }

    public void OnButtonHoverEnter(string buttonName)
    {
        if (buttonName != "Preview")
            return;
        StartHoverPreview();
    }

    public void OnButtonHoverExit(string buttonName)
    {
        if (buttonName != "Preview")
            return;
        StopHoverPreview();
    }

    public void EnterBuildMode()
    {
        CurrentState = LevelState.Build;
        _connMgr.CurrentState = LevelState.Build;
        IsInputLocked = false;
        _isHoverPreviewActive = false;
        ResetGoalStates();

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

        UpdatePreviewButtonVisibility();
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
        if (_buildIntroRoutine != null)
        {
            StopCoroutine(_buildIntroRoutine);
            _buildIntroRoutine = null;
            _buildIntroFinished = true;
        }

        StopHoverPreviewImmediate();
        IsInputLocked = false;
        _memoryBlueprint = CaptureBlueprint();
        SaveBlueprintToDisk();

        CurrentState = LevelState.Run;
        _connMgr.CurrentState = LevelState.Run;
        ResetGoalStates();
        _connMgr.InitializeAllForRun();

        foreach (var node in _connMgr.AllNodes)
        {
            node.EnterRunMode();
        }

        SetActionButton("Stop", ButtonShape.Square, ColorConfig.Instance.StopButtonColor);
        SetBuildUIVisible(false);
        UpdatePreviewButtonVisibility();

        var camCtrl = _mainCamera.GetComponent<RuntimeCameraController>();
        if (camCtrl != null) camCtrl.StartFollowing(RuntimeCameraController.ZoomMode.OnlyZoomOut);
    }

    public void EnterVictoryMode()
    {
        if (CurrentState == LevelState.Victory) return;

        if (!_isHoverPreviewActive)
            StopHoverPreviewImmediate();
        CurrentState = LevelState.Victory;
        _connMgr.CurrentState = LevelState.Victory;

        SaveManager.Instance.CompleteLevel(SceneManager.GetActiveScene().name);

        SetActionButton("Next", ButtonShape.TriangleRight, ColorConfig.Instance.NextButtonColor);
        UpdatePreviewButtonVisibility();
    }

    public void NotifyGoalTriggered(GoalTrigger goal)
    {
        if (CurrentState != LevelState.Run) return;
        if (goal != null) goal.SetReached(true);
        TryEnterVictoryMode();
    }

    private void BeginBuildIntroSequence()
    {
        if (!gameObject.activeInHierarchy) return;
        StopHoverPreviewImmediate();
        _buildIntroFinished = false;
        UpdatePreviewButtonVisibility();
        if (_buildIntroRoutine != null) StopCoroutine(_buildIntroRoutine);
        _buildIntroRoutine = StartCoroutine(BuildIntroSequenceRoutine());
    }

    private IEnumerator BuildIntroSequenceRoutine()
    {
        IsInputLocked = true;
        SetBuildUIVisible(false);
        SetActionButtonDisabledVisual();
        if (_partAppearController != null)
            _partAppearController.HideInventoryParts(_connMgr.AllNodes);
        var camCtrl = _mainCamera != null ? _mainCamera.GetComponent<RuntimeCameraController>() : null;
        if (_introSequenceController != null)
        {
            yield return StartCoroutine(_introSequenceController.PlayIntro(
                _mainCamera,
                camCtrl,
                IntroCameraAnchor,
                CameraAnchor,
                IntroBoundsRoots,
                _uiCanvas != null ? _uiCanvas.transform : null,
                _areaVisualController != null ? _areaVisualController.InventoryVisualRoot : null,
                _areaVisualController != null ? _areaVisualController.BuildAreaVisualRoot : null));
        }

        float appearSeconds = CameraConfig.Instance != null ? CameraConfig.Instance.PartAppearSeconds : 0.5f;
        Coroutine areaFadeRoutine = null;
        Coroutine partFadeRoutine = null;
        if (_areaVisualController != null)
            areaFadeRoutine = StartCoroutine(_areaVisualController.FadeIn(appearSeconds));
        if (_partAppearController != null)
            partFadeRoutine = StartCoroutine(_partAppearController.PlayInventoryAppearance(_connMgr.AllNodes, CameraConfig.Instance));
        if (areaFadeRoutine != null) yield return areaFadeRoutine;
        if (partFadeRoutine != null) yield return partFadeRoutine;
        if (_partAppearController != null)
            _partAppearController.ForceShowInventoryParts(_connMgr.AllNodes);

        _buildIntroFinished = true;
        IsInputLocked = false;
        UpdateStartButtonState(true);
        _buildIntroRoutine = null;
        UpdatePreviewButtonVisibility();
    }

    private void SetActionButton(string buttonName, ButtonShape shape, Color color)
    {
        if (_actionButton == null) return;
        _actionButton.ButtonName = buttonName;
        _actionButton.Shape = shape;
        _actionButton.ButtonColor = color;
        _actionButton.ApplyVisual();
    }

    private void SetActionButtonDisabledVisual()
    {
        if (_actionButton == null) return;
        _actionButton.ButtonColor = ColorConfig.Instance.DisabledButtonColor;
        _actionButton.ApplyVisual();
    }

    private void StartHoverPreview()
    {
        if (!CanPreviewHover()) return;
        CancelHoverRecoverRoutine();

        if (_mainCamera == null || _introSequenceController == null) return;
        var runtimeCtrl = _mainCamera.GetComponent<RuntimeCameraController>();
        if (!_introSequenceController.TryGetIntroCameraTargets(
            _mainCamera,
            runtimeCtrl,
            IntroCameraAnchor,
            CameraAnchor,
            IntroBoundsRoots,
            _uiCanvas != null ? _uiCanvas.transform : null,
            _areaVisualController != null ? _areaVisualController.InventoryVisualRoot : null,
            _areaVisualController != null ? _areaVisualController.BuildAreaVisualRoot : null,
            out var introPos,
            out _,
            out var introOrthoSize,
            out _))
        {
            return;
        }

        if (runtimeCtrl != null)
            runtimeCtrl.enabled = false;

        _isHoverPreviewActive = true;
        IsInputLocked = true;
        if (_areaVisualController != null)
            _areaVisualController.CancelFadeAndRestoreColors();
        SetBuildUIVisible(false);
        if (_partAppearController != null)
        {
            _partAppearController.CancelHiddenFadeAndRestore();
            _partAppearController.HideInventoryParts(_connMgr.AllNodes);
        }

        float hoverToIntroSeconds;
        if (CurrentState == LevelState.Run || CurrentState == LevelState.Victory)
            hoverToIntroSeconds = CameraConfig.Instance != null ? CameraConfig.Instance.RuntimeHoverToIntroSeconds : 0.35f;
        else
            hoverToIntroSeconds = CameraConfig.Instance != null ? CameraConfig.Instance.HoverToIntroSeconds : 0.35f;
        GetOrCreateIntroCameraController().TransitionTo(introPos, introOrthoSize, hoverToIntroSeconds);
    }

    private void StopHoverPreview()
    {
        if (!_isHoverPreviewActive) return;
        _isHoverPreviewActive = false;

        CancelHoverRecoverRoutine();
        if (_mainCamera == null || _introSequenceController == null) return;
        var runtimeCtrl = _mainCamera.GetComponent<RuntimeCameraController>();

        if (CurrentState == LevelState.Run || CurrentState == LevelState.Victory)
        {
            TransitionBackToRuntimeCamera(runtimeCtrl);
            return;
        }

        if (!_introSequenceController.TryGetIntroCameraTargets(
            _mainCamera,
            runtimeCtrl,
            IntroCameraAnchor,
            CameraAnchor,
            IntroBoundsRoots,
            _uiCanvas != null ? _uiCanvas.transform : null,
            _areaVisualController != null ? _areaVisualController.InventoryVisualRoot : null,
            _areaVisualController != null ? _areaVisualController.BuildAreaVisualRoot : null,
            out _,
            out var buildPos,
            out _,
            out var buildOrthoSize))
        {
            StartHoverRecoverRoutine();
            return;
        }

        float hoverReturnSeconds = CameraConfig.Instance != null ? CameraConfig.Instance.HoverReturnSeconds : 0.2f;
        var introCtrl = GetOrCreateIntroCameraController();
        introCtrl.TransitionTo(buildPos, buildOrthoSize, hoverReturnSeconds, () =>
        {
            if (_isHoverPreviewActive) return;
            StartHoverRecoverRoutine();
        });
    }

    private void StopHoverPreviewImmediate()
    {
        CancelHoverRecoverRoutine();

        _isHoverPreviewActive = false;
        var introCtrl = _mainCamera != null ? _mainCamera.GetComponent<IntroCameraController>() : null;
        if (introCtrl != null)
            introCtrl.StopCurrentTransition(false);
        if (_areaVisualController != null)
            _areaVisualController.CancelFadeAndRestoreColors();
        if (_partAppearController != null)
            _partAppearController.CancelHiddenFadeAndRestore();
        if (_partAppearController != null)
            _partAppearController.ShowHiddenInventoryParts();
        if (_partAppearController != null)
            _partAppearController.ForceShowInventoryParts(_connMgr.AllNodes);
        if (_areaVisualController != null)
            _areaVisualController.SetVisible(CurrentState == LevelState.Build && _buildIntroFinished);

        if (CurrentState == LevelState.Run || CurrentState == LevelState.Victory)
        {
            if (_partAppearController != null)
                _partAppearController.HideInventoryParts(_connMgr.AllNodes);
            if (_mainCamera != null)
            {
                var runtimeCtrl = _mainCamera.GetComponent<RuntimeCameraController>();
                if (runtimeCtrl != null)
                {
                    runtimeCtrl.StartFollowing(RuntimeCameraController.ZoomMode.OnlyZoomOut);
                    runtimeCtrl.SuspendOnlyZoomOutForSeconds(runtimeCtrl.SmoothTime * 5f);
                }
            }
            SetBuildUIVisible(false);
            RestoreActionButtonVisualForCurrentState();
            IsInputLocked = false;
        }
    }

    private bool CanPreviewHover()
    {
        bool isPreviewState = CurrentState == LevelState.Build
                              || CurrentState == LevelState.Run
                              || CurrentState == LevelState.Victory;
        return isPreviewState
               && _buildIntroRoutine == null
               && _buildIntroFinished
               && !_isHoverPreviewActive;
    }

    private IntroCameraController GetOrCreateIntroCameraController()
    {
        var ctrl = _mainCamera.GetComponent<IntroCameraController>();
        if (ctrl == null) ctrl = _mainCamera.gameObject.AddComponent<IntroCameraController>();
        return ctrl;
    }

    private IEnumerator HoverRecoverRoutine()
    {
        if (CurrentState != LevelState.Build || _buildIntroRoutine != null || !_buildIntroFinished)
        {
            _hoverRecoverRoutine = null;
            yield break;
        }

        float appearSeconds = CameraConfig.Instance != null ? CameraConfig.Instance.PartAppearSeconds : 0.5f;
        Coroutine areaFadeRoutine = null;
        Coroutine partFadeRoutine = null;
        if (_areaVisualController != null)
            areaFadeRoutine = StartCoroutine(_areaVisualController.FadeIn(appearSeconds));
        if (_partAppearController != null)
            partFadeRoutine = StartCoroutine(_partAppearController.FadeInHiddenInventoryParts(appearSeconds));
        if (areaFadeRoutine != null) yield return areaFadeRoutine;
        if (partFadeRoutine != null) yield return partFadeRoutine;

        if (CurrentState == LevelState.Build && _buildIntroRoutine == null && _buildIntroFinished)
        {
            if (_partAppearController != null)
                _partAppearController.ForceShowInventoryParts(_connMgr.AllNodes);
            IsInputLocked = false;
            UpdateStartButtonState(true);
        }

        _hoverRecoverRoutine = null;
    }

    private void StartHoverRecoverRoutine()
    {
        if (_hoverRecoverRoutine != null)
            StopCoroutine(_hoverRecoverRoutine);
        _hoverRecoverRoutine = StartCoroutine(HoverRecoverRoutine());
    }

    private void CancelHoverRecoverRoutine()
    {
        if (_hoverRecoverRoutine != null)
        {
            StopCoroutine(_hoverRecoverRoutine);
            _hoverRecoverRoutine = null;
        }
        if (_areaVisualController != null)
            _areaVisualController.CancelFadeAndRestoreColors();
        if (_partAppearController != null)
            _partAppearController.CancelHiddenFadeAndRestore();
    }

    private void UpdatePreviewButtonVisibility()
    {
        if (_previewButton == null) return;
        bool previewState = CurrentState == LevelState.Build
                            || CurrentState == LevelState.Run
                            || CurrentState == LevelState.Victory;
        bool visible = previewState && _buildIntroFinished && _buildIntroRoutine == null;
        if (_previewButton.gameObject.activeSelf != visible)
            _previewButton.gameObject.SetActive(visible);
    }

    private void TransitionBackToRuntimeCamera(RuntimeCameraController runtimeCtrl)
    {
        var introCtrl = _mainCamera != null ? _mainCamera.GetComponent<IntroCameraController>() : null;
        if (introCtrl != null)
            introCtrl.StopCurrentTransition(false);

        // Restore runtime camera follow directly to avoid returning to a stale cached position first.
        RestoreRuntimeHoverState(runtimeCtrl);
    }

    private void RestoreRuntimeHoverState(RuntimeCameraController runtimeCtrl)
    {
        if (_partAppearController != null)
        {
            _partAppearController.CancelHiddenFadeAndRestore();
            _partAppearController.HideInventoryParts(_connMgr.AllNodes);
        }
        if (_areaVisualController != null)
            _areaVisualController.CancelFadeAndRestoreColors();

        SetBuildUIVisible(false);
        RestoreActionButtonVisualForCurrentState();

        if (runtimeCtrl != null)
        {
            runtimeCtrl.StartFollowing(RuntimeCameraController.ZoomMode.OnlyZoomOut);
            float cooldown = runtimeCtrl.SmoothTime * 5f;
            runtimeCtrl.SuspendOnlyZoomOutForSeconds(cooldown);
            runtimeCtrl.SuspendCameraDistanceZoomForSeconds(cooldown);
        }
        IsInputLocked = false;
    }

    private void RestoreActionButtonVisualForCurrentState()
    {
        if (CurrentState == LevelState.Run)
            SetActionButton("Stop", ButtonShape.Square, ColorConfig.Instance.StopButtonColor);
        else if (CurrentState == LevelState.Victory)
            SetActionButton("Next", ButtonShape.TriangleRight, ColorConfig.Instance.NextButtonColor);
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
            GameObject sourcePrefab = node.SourcePrefab != null ? node.SourcePrefab : FindPrefabByType(node.NodeType);
            if (sourcePrefab == null)
                Debug.LogError($"Failed to resolve source prefab for node '{node.name}' ({node.NodeType}) in scene '{CurrentSceneName}'.");

            nodeIndexMap[node] = idx++;
            data.nodes.Add(new NodeData
            {
                prefab = sourcePrefab,
                isInInventory = node.IsInInventory,
                posX = node.IsInInventory ? 0f : node.transform.position.x,
                posY = node.IsInInventory ? 0f : node.transform.position.y,
                rotZ = node.transform.eulerAngles.z
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

        for (int i = 0; i < bp.nodes.Count; i++)
        {
            var nd = bp.nodes[i];
            if (nd.prefab == null)
            {
                Debug.LogError($"Blueprint node prefab is null at index {i} in scene '{CurrentSceneName}'.");
                continue;
            }
            GameObject prefab = nd.prefab;

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

            var spawnRot = Quaternion.Euler(prefab.transform.eulerAngles.x, prefab.transform.eulerAngles.y, nd.rotZ);
            var go = Instantiate(prefab, spawnPos, spawnRot);
            var node = go.GetComponent<Node>();
            if (node == null) continue;

            node.SourcePrefab = prefab;
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
        BlueprintData.SaveBlueprint(CurrentSceneName, bp);
    }

    private void RefreshGoalTriggers()
    {
        _goalTriggers.Clear();
        _goalTriggers.AddRange(FindObjectsOfType<GoalTrigger>(true));
    }

    private void ResetGoalStates()
    {
        RefreshGoalTriggers();
        for (int i = 0; i < _goalTriggers.Count; i++)
        {
            var goal = _goalTriggers[i];
            if (goal == null) continue;
            goal.SetReached(false);
        }
    }

    private bool AreAllGoalsReached()
    {
        bool hasGoal = false;
        for (int i = 0; i < _goalTriggers.Count; i++)
        {
            var goal = _goalTriggers[i];
            if (goal == null) continue;
            hasGoal = true;
            if (!goal.IsReached) return false;
        }
        return hasGoal;
    }

    private void TryEnterVictoryMode()
    {
        if (CurrentState != LevelState.Run) return;
        if (!AreAllGoalsReached()) return;
        EnterVictoryMode();
    }

    private void OnDrawGizmos()
    {
        LevelAreaVisualController.DrawAreaGizmos(GetBuildAreaPolygon(), GetInventoryArea());
        var introCtrl = _introSequenceController != null
            ? _introSequenceController
            : GetComponent<LevelIntroSequenceController>();
        if (introCtrl != null)
        {
            introCtrl.DrawIntroGizmo(
                IntroCameraAnchor,
                IntroBoundsRoots,
                _uiCanvas != null ? _uiCanvas.transform : null,
                _areaVisualController != null ? _areaVisualController.InventoryVisualRoot : null,
                _areaVisualController != null ? _areaVisualController.BuildAreaVisualRoot : null);
        }
    }
}
