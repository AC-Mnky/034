using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class IntroCameraController : MonoBehaviour
{
    private enum TransitionState
    {
        Idle = 0,
        Hold = 1,
        Transition = 2
    }

    private Camera _cam;
    private TransitionState _state = TransitionState.Idle;
    private Vector3 _holdPos;
    private float _holdOrthoSize;
    private float _holdRemaining;
    private Vector3 _fromPos;
    private Vector3 _toPos;
    private float _fromOrthoSize;
    private float _toOrthoSize;
    private float _duration;
    private float _elapsed;
    private Action _onComplete;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        enabled = false;
    }

    private void OnEnable()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
    }

    public void PlayIntroSequence(
        Vector3 introPos, Vector3 buildPos,
        float holdSeconds, float moveSeconds,
        float introOrthoSize, float buildOrthoSize,
        Action onComplete)
    {
        StopCurrentTransition(invokeCallback: false);

        float z = transform.position.z;
        _holdPos = new Vector3(introPos.x, introPos.y, z);
        _holdOrthoSize = Mathf.Max(0.01f, introOrthoSize);
        _holdRemaining = Mathf.Max(0f, holdSeconds);
        _onComplete = onComplete;

        transform.position = _holdPos;
        _cam.orthographicSize = _holdOrthoSize;
        _state = _holdRemaining > 0f ? TransitionState.Hold : TransitionState.Transition;

        BeginTransition(new Vector3(buildPos.x, buildPos.y, z), buildOrthoSize, moveSeconds);
        enabled = true;
    }

    public void TransitionTo(Vector3 targetPos, float targetOrthoSize, float durationSeconds, Action onComplete = null)
    {
        StopCurrentTransition(invokeCallback: false);
        _onComplete = onComplete;
        BeginTransition(new Vector3(targetPos.x, targetPos.y, transform.position.z), targetOrthoSize, durationSeconds);
        _state = TransitionState.Transition;
        enabled = true;
    }

    public void StopCurrentTransition(bool invokeCallback)
    {
        if (_state == TransitionState.Idle)
            return;

        _state = TransitionState.Idle;
        enabled = false;
        var callback = _onComplete;
        _onComplete = null;
        if (invokeCallback)
            callback?.Invoke();
    }

    private void BeginTransition(Vector3 targetPos, float targetOrthoSize, float durationSeconds)
    {
        _fromPos = transform.position;
        _fromOrthoSize = Mathf.Max(0.01f, _cam.orthographicSize);
        _toPos = targetPos;
        _toOrthoSize = Mathf.Max(0.01f, targetOrthoSize);
        _duration = Mathf.Max(0.0001f, durationSeconds);
        _elapsed = 0f;
    }

    private void LateUpdate()
    {
        if (_state == TransitionState.Hold)
        {
            transform.position = _holdPos;
            _cam.orthographicSize = _holdOrthoSize;
            _holdRemaining -= Time.deltaTime;
            if (_holdRemaining <= 0f)
                _state = TransitionState.Transition;
            return;
        }

        if (_state == TransitionState.Transition)
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float eased = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(_fromPos, _toPos, eased);
            _cam.orthographicSize = Mathf.Lerp(_fromOrthoSize, _toOrthoSize, eased);

            if (t >= 1f)
            {
                transform.position = _toPos;
                _cam.orthographicSize = _toOrthoSize;
                _state = TransitionState.Idle;
                enabled = false;
                var callback = _onComplete;
                _onComplete = null;
                callback?.Invoke();
            }
        }
    }
}
