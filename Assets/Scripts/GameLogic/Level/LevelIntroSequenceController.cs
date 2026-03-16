using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelIntroSequenceController : MonoBehaviour
{
    public bool TryGetIntroCameraTargets(
        Camera mainCamera,
        RuntimeCameraController runtimeCameraController,
        Transform introCameraAnchor,
        Transform cameraAnchor,
        float introOrthoSizeOverride,
        List<Transform> introBoundsRoots,
        Transform uiRoot,
        Transform inventoryVisualRoot,
        Transform buildVisualRoot,
        out Vector3 introPos,
        out Vector3 buildPos,
        out float introOrthoSize,
        out float buildOrthoSize)
    {
        introPos = Vector3.zero;
        buildPos = Vector3.zero;
        introOrthoSize = 0f;
        buildOrthoSize = 0f;
        if (mainCamera == null) return false;

        buildPos = cameraAnchor != null
            ? new Vector3(cameraAnchor.position.x, cameraAnchor.position.y, mainCamera.transform.position.z)
            : mainCamera.transform.position;
        introPos = ResolveIntroCameraPosition(mainCamera, introCameraAnchor, buildPos, introBoundsRoots, uiRoot, inventoryVisualRoot, buildVisualRoot);

        buildOrthoSize = runtimeCameraController != null
            ? runtimeCameraController.DefaultOrthoSize
            : mainCamera.orthographicSize;
        introOrthoSize = ResolveIntroCameraOrthoSize(
            mainCamera, introCameraAnchor, introOrthoSizeOverride, introBoundsRoots, uiRoot, inventoryVisualRoot, buildVisualRoot, buildOrthoSize);
        return true;
    }

    public IEnumerator PlayIntro(
        Camera mainCamera,
        RuntimeCameraController runtimeCameraController,
        Transform introCameraAnchor,
        Transform cameraAnchor,
        float introOrthoSizeOverride,
        List<Transform> introBoundsRoots,
        Transform uiRoot,
        Transform inventoryVisualRoot,
        Transform buildVisualRoot)
    {
        if (mainCamera == null) yield break;
        if (!TryGetIntroCameraTargets(
            mainCamera,
            runtimeCameraController,
            introCameraAnchor,
            cameraAnchor,
            introOrthoSizeOverride,
            introBoundsRoots,
            uiRoot,
            inventoryVisualRoot,
            buildVisualRoot,
            out var introPos,
            out var buildPos,
            out var introOrthoSize,
            out var buildOrthoSize))
        {
            yield break;
        }

        var cfg = CameraConfig.Instance;
        float holdSeconds = cfg != null ? cfg.IntroHoldSeconds : 1f;
        float moveSeconds = cfg != null ? cfg.IntroMoveSeconds : 3f;

        var introCtrl = mainCamera.GetComponent<IntroCameraController>();
        if (introCtrl == null) introCtrl = mainCamera.gameObject.AddComponent<IntroCameraController>();

        bool cameraDone = false;
        introCtrl.PlayIntroSequence(
            introPos, buildPos,
            holdSeconds, moveSeconds,
            introOrthoSize, buildOrthoSize,
            () => cameraDone = true);
        while (!cameraDone) yield return null;
    }

    public void DrawIntroGizmo(
        Transform introCameraAnchor,
        float introOrthoSizeOverride,
        List<Transform> introBoundsRoots,
        Transform uiRoot,
        Transform inventoryVisualRoot,
        Transform buildVisualRoot)
    {
        Camera cam = GetGameplayReferenceCamera();
        if (cam == null || !cam.orthographic) return;

        Vector3 c;
        bool fromAnchor = introCameraAnchor != null;
        float introOrthoSize = ResolveIntroCameraOrthoSize(
            cam, introCameraAnchor, introOrthoSizeOverride, introBoundsRoots, uiRoot, inventoryVisualRoot, buildVisualRoot, cam.orthographicSize);

        if (fromAnchor)
        {
            c = introCameraAnchor.position;
        }
        else if (TryGetIntroBounds(introBoundsRoots, uiRoot, inventoryVisualRoot, buildVisualRoot, out var autoBounds))
        {
            c = autoBounds.center;
            Gizmos.color = Color.yellow;
            Vector3 b0 = new Vector3(autoBounds.min.x, autoBounds.min.y, c.z);
            Vector3 b1 = new Vector3(autoBounds.max.x, autoBounds.min.y, c.z);
            Vector3 b2 = new Vector3(autoBounds.max.x, autoBounds.max.y, c.z);
            Vector3 b3 = new Vector3(autoBounds.min.x, autoBounds.max.y, c.z);
            Gizmos.DrawLine(b0, b1);
            Gizmos.DrawLine(b1, b2);
            Gizmos.DrawLine(b2, b3);
            Gizmos.DrawLine(b3, b0);
        }
        else
        {
            return;
        }

        float halfH = introOrthoSize;
        float halfW = halfH * cam.aspect;
        Vector3 p0 = new Vector3(c.x - halfW, c.y - halfH, c.z);
        Vector3 p1 = new Vector3(c.x + halfW, c.y - halfH, c.z);
        Vector3 p2 = new Vector3(c.x + halfW, c.y + halfH, c.z);
        Vector3 p3 = new Vector3(c.x - halfW, c.y + halfH, c.z);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p0);
        Gizmos.DrawWireSphere(c, fromAnchor ? 0.15f : 0.1f);
    }

    private static Vector3 ResolveIntroCameraPosition(
        Camera cam, Transform introCameraAnchor, Vector3 fallbackPos,
        List<Transform> introBoundsRoots, Transform uiRoot, Transform inventoryVisualRoot, Transform buildVisualRoot)
    {
        if (cam == null) return fallbackPos;
        if (introCameraAnchor != null)
            return new Vector3(introCameraAnchor.position.x, introCameraAnchor.position.y, cam.transform.position.z);
        if (TryGetIntroBounds(introBoundsRoots, uiRoot, inventoryVisualRoot, buildVisualRoot, out var bounds))
            return new Vector3(bounds.center.x, bounds.center.y, cam.transform.position.z);
        return fallbackPos;
    }

    private static float ResolveIntroCameraOrthoSize(
        Camera cam, Transform introCameraAnchor, float introOrthoSizeOverride, List<Transform> introBoundsRoots,
        Transform uiRoot, Transform inventoryVisualRoot, Transform buildVisualRoot, float fallbackOrthoSize)
    {
        if (cam == null) return fallbackOrthoSize;
        if (introOrthoSizeOverride > 0f) return introOrthoSizeOverride;
        if (introCameraAnchor != null) return fallbackOrthoSize;
        if (!TryGetIntroBounds(introBoundsRoots, uiRoot, inventoryVisualRoot, buildVisualRoot, out var bounds))
            return fallbackOrthoSize;

        var cfg = CameraConfig.Instance;
        float scaleMultiplier = cfg != null ? Mathf.Max(1f, cfg.IntroBoundsScaleMultiplier) : 1.2f;
        float required = ComputeOrthoSizeToContainBounds(bounds, cam.aspect) * scaleMultiplier;
        return Mathf.Max(fallbackOrthoSize, required);
    }

    private static float ComputeOrthoSizeToContainBounds(Bounds bounds, float aspect)
    {
        if (aspect <= 1e-4f) return bounds.extents.y;
        return Mathf.Max(bounds.extents.y, bounds.extents.x / aspect);
    }

    private static bool TryGetIntroBounds(
        List<Transform> introBoundsRoots, Transform uiRoot, Transform inventoryVisualRoot, Transform buildVisualRoot, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        if (TryCollectBoundsFromRoots(introBoundsRoots, ref bounds))
            return true;

        var renderers = FindObjectsOfType<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!ShouldIncludeForIntroBounds(r, uiRoot, inventoryVisualRoot, buildVisualRoot)) continue;
            AppendBounds(ref bounds, ref hasBounds, r.bounds);
        }

        var colliders2D = FindObjectsOfType<Collider2D>();
        for (int i = 0; i < colliders2D.Length; i++)
        {
            var c = colliders2D[i];
            if (!ShouldIncludeForIntroBounds(c, uiRoot, inventoryVisualRoot, buildVisualRoot)) continue;
            AppendBounds(ref bounds, ref hasBounds, c.bounds);
        }

        return hasBounds;
    }

    private static bool TryCollectBoundsFromRoots(List<Transform> introBoundsRoots, ref Bounds bounds)
    {
        if (introBoundsRoots == null || introBoundsRoots.Count == 0) return false;
        bool hasBounds = false;
        for (int i = 0; i < introBoundsRoots.Count; i++)
        {
            var root = introBoundsRoots[i];
            if (root == null) continue;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                var r = renderers[j];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                AppendBounds(ref bounds, ref hasBounds, r.bounds);
            }

            var colliders2D = root.GetComponentsInChildren<Collider2D>(true);
            for (int j = 0; j < colliders2D.Length; j++)
            {
                var c = colliders2D[j];
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                AppendBounds(ref bounds, ref hasBounds, c.bounds);
            }
        }
        return hasBounds;
    }

    private static bool ShouldIncludeForIntroBounds(Component comp, Transform uiRoot, Transform inventoryVisualRoot, Transform buildVisualRoot)
    {
        if (comp == null || !comp.gameObject.activeInHierarchy) return false;
        if (comp.GetComponentInParent<Node>() != null) return false;
        if (uiRoot != null && comp.transform.IsChildOf(uiRoot)) return false;
        if (inventoryVisualRoot != null && comp.transform.IsChildOf(inventoryVisualRoot)) return false;
        if (buildVisualRoot != null && comp.transform.IsChildOf(buildVisualRoot)) return false;
        if (comp.GetComponent<LineRenderer>() != null) return false;
        return true;
    }

    private static void AppendBounds(ref Bounds dst, ref bool hasBounds, Bounds src)
    {
        if (!hasBounds)
        {
            dst = src;
            hasBounds = true;
            return;
        }
        dst.Encapsulate(src);
    }

    private static Camera GetGameplayReferenceCamera()
    {
        var mainCam = Camera.main;
        if (mainCam != null) return mainCam;
        var camCtrl = FindObjectOfType<RuntimeCameraController>();
        if (camCtrl != null)
        {
            var cam = camCtrl.GetComponent<Camera>();
            if (cam != null) return cam;
        }
        return null;
    }
}
