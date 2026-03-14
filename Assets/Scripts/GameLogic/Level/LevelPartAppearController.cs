using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelPartAppearController : MonoBehaviour
{
    private HashSet<Renderer> _hiddenInventoryRenderers = new HashSet<Renderer>();
    private readonly List<PartMaterialState> _activeHiddenFadeMaterials = new List<PartMaterialState>();

    private class PartMaterialState
    {
        public Material Material;
        public Color OriginalColor;
        public float StartAlpha;
    }

    private class PartAppearState
    {
        public Transform Transform;
        public Vector3 OriginalScale;
        public Vector3 StartScale;
        public readonly List<PartMaterialState> Materials = new List<PartMaterialState>();
    }

    public void HideInventoryParts(List<Node> allNodes)
    {
        RestoreActiveHiddenFadeMaterials();
        var hidden = CollectAndHideInventoryParts(allNodes);
        foreach (var renderer in hidden)
            _hiddenInventoryRenderers.Add(renderer);
    }

    public void ShowHiddenInventoryParts()
    {
        RestoreActiveHiddenFadeMaterials();
        foreach (var renderer in _hiddenInventoryRenderers)
        {
            if (renderer != null)
                renderer.enabled = true;
        }
        _hiddenInventoryRenderers.Clear();
    }

    public void ForceShowInventoryParts(List<Node> allNodes)
    {
        RestoreActiveHiddenFadeMaterials();
        if (allNodes == null) return;
        for (int i = 0; i < allNodes.Count; i++)
        {
            var node = allNodes[i];
            if (node == null || !node.gameObject.activeSelf || !node.IsInInventory) continue;
            var renderers = node.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                var renderer = renderers[r];
                if (renderer != null) renderer.enabled = true;
            }
        }
        _hiddenInventoryRenderers.Clear();
    }

    public IEnumerator FadeInHiddenInventoryParts(float durationSeconds)
    {
        if (_hiddenInventoryRenderers == null || _hiddenInventoryRenderers.Count == 0) yield break;
        durationSeconds = Mathf.Max(0.01f, durationSeconds);

        _activeHiddenFadeMaterials.Clear();
        foreach (var renderer in _hiddenInventoryRenderers)
        {
            if (renderer == null) continue;
            renderer.enabled = true;
            var mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat == null || !mat.HasProperty("_Color")) continue;
                var original = mat.color;
                mat.color = new Color(original.r, original.g, original.b, 0f);
                _activeHiddenFadeMaterials.Add(new PartMaterialState
                {
                    Material = mat,
                    OriginalColor = original,
                    StartAlpha = 0f
                });
            }
        }

        float t = 0f;
        while (t < durationSeconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / durationSeconds);
            float eased = p * p * (3f - 2f * p);
            for (int i = 0; i < _activeHiddenFadeMaterials.Count; i++)
            {
                var ms = _activeHiddenFadeMaterials[i];
                if (ms.Material == null) continue;
                float alpha = Mathf.Lerp(0f, ms.OriginalColor.a, eased);
                ms.Material.color = new Color(ms.OriginalColor.r, ms.OriginalColor.g, ms.OriginalColor.b, alpha);
            }
            yield return null;
        }

        RestoreActiveHiddenFadeMaterials();
        _hiddenInventoryRenderers.Clear();
    }

    public void CancelHiddenFadeAndRestore()
    {
        RestoreActiveHiddenFadeMaterials();
    }

    private void RestoreActiveHiddenFadeMaterials()
    {
        for (int i = 0; i < _activeHiddenFadeMaterials.Count; i++)
        {
            var ms = _activeHiddenFadeMaterials[i];
            if (ms.Material != null)
                ms.Material.color = ms.OriginalColor;
        }
        _activeHiddenFadeMaterials.Clear();
    }

    public IEnumerator PlayInventoryAppearance(List<Node> allNodes, CameraConfig cfg)
    {
        var states = PrepareInventoryParts(allNodes, _hiddenInventoryRenderers, cfg);
        _hiddenInventoryRenderers.Clear();
        yield return PlayAppearance(states, cfg);
    }

    private static HashSet<Renderer> CollectAndHideInventoryParts(List<Node> allNodes)
    {
        var hidden = new HashSet<Renderer>();
        for (int i = 0; i < allNodes.Count; i++)
        {
            var node = allNodes[i];
            if (node == null || !node.gameObject.activeSelf || !node.IsInInventory) continue;
            var renderers = node.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                var renderer = renderers[r];
                if (renderer == null || !renderer.enabled) continue;
                renderer.enabled = false;
                hidden.Add(renderer);
            }
        }
        return hidden;
    }

    private static List<PartAppearState> PrepareInventoryParts(List<Node> allNodes, HashSet<Renderer> hidden, CameraConfig cfg)
    {
        var states = new List<PartAppearState>();
        float startScaleRate = cfg != null ? cfg.PartAppearStartScale : 0.65f;
        float startAlphaRate = cfg != null ? cfg.PartAppearStartAlpha : 0f;

        for (int i = 0; i < allNodes.Count; i++)
        {
            var node = allNodes[i];
            if (node == null || !node.gameObject.activeSelf || !node.IsInInventory) continue;

            var state = new PartAppearState
            {
                Transform = node.transform,
                OriginalScale = node.transform.localScale
            };
            state.StartScale = state.OriginalScale * Mathf.Max(0f, startScaleRate);
            state.Transform.localScale = state.StartScale;

            var renderers = node.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                var renderer = renderers[r];
                if (renderer == null) continue;
                if (!renderer.enabled)
                {
                    if (hidden != null && hidden.Contains(renderer)) renderer.enabled = true;
                    else continue;
                }

                var mats = renderer.materials;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null || !mat.HasProperty("_Color")) continue;
                    var original = mat.color;
                    float startAlpha = original.a * Mathf.Clamp01(startAlphaRate);
                    mat.color = new Color(original.r, original.g, original.b, startAlpha);
                    state.Materials.Add(new PartMaterialState
                    {
                        Material = mat,
                        OriginalColor = original,
                        StartAlpha = startAlpha
                    });
                }
            }

            states.Add(state);
        }

        return states;
    }

    private static IEnumerator PlayAppearance(List<PartAppearState> states, CameraConfig cfg)
    {
        if (states == null || states.Count == 0) yield break;

        float duration = cfg != null ? cfg.PartAppearSeconds : 0.5f;
        duration = Mathf.Max(0.01f, duration);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p);
            for (int i = 0; i < states.Count; i++)
            {
                var s = states[i];
                if (s.Transform == null) continue;
                s.Transform.localScale = Vector3.Lerp(s.StartScale, s.OriginalScale, eased);
                for (int j = 0; j < s.Materials.Count; j++)
                {
                    var ms = s.Materials[j];
                    if (ms.Material == null) continue;
                    float alpha = Mathf.Lerp(ms.StartAlpha, ms.OriginalColor.a, eased);
                    ms.Material.color = new Color(ms.OriginalColor.r, ms.OriginalColor.g, ms.OriginalColor.b, alpha);
                }
            }
            yield return null;
        }

        for (int i = 0; i < states.Count; i++)
        {
            var s = states[i];
            if (s.Transform != null) s.Transform.localScale = s.OriginalScale;
            for (int j = 0; j < s.Materials.Count; j++)
            {
                var ms = s.Materials[j];
                if (ms.Material == null) continue;
                ms.Material.color = ms.OriginalColor;
            }
        }
    }
}
