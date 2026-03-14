using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class IntroCameraController : MonoBehaviour
{
    private enum IntroState
    {
        Idle = 0,
        Hold = 1,
        Move = 2
    }

    private Camera _cam;
    private IntroState _introState = IntroState.Idle;
    private Vector3 _introHoldPos;
    private Vector3 _introTargetPos;
    private float _introHoldRemaining;
    private float _introMoveDuration;
    private float _introMoveElapsed;
    private float _introHoldOrthoSize;
    private float _introTargetOrthoSize;
    private Action _onIntroComplete;

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
        _onIntroComplete = onComplete;

        float z = transform.position.z;
        _introHoldPos = new Vector3(introPos.x, introPos.y, z);
        _introTargetPos = new Vector3(buildPos.x, buildPos.y, z);
        _introHoldRemaining = Mathf.Max(0f, holdSeconds);
        _introMoveDuration = Mathf.Max(0.0001f, moveSeconds);
        _introMoveElapsed = 0f;
        _introHoldOrthoSize = Mathf.Max(0.01f, introOrthoSize);
        _introTargetOrthoSize = Mathf.Max(0.01f, buildOrthoSize);

        transform.position = _introHoldPos;
        _cam.orthographicSize = _introHoldOrthoSize;
        _introState = IntroState.Hold;
        enabled = true;
    }

    private void LateUpdate()
    {
        if (_introState == IntroState.Hold)
        {
            transform.position = _introHoldPos;
            _cam.orthographicSize = _introHoldOrthoSize;
            _introHoldRemaining -= Time.deltaTime;
            if (_introHoldRemaining <= 0f)
                _introState = IntroState.Move;
            return;
        }

        if (_introState == IntroState.Move)
        {
            _introMoveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_introMoveElapsed / _introMoveDuration);
            float eased = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(_introHoldPos, _introTargetPos, eased);
            _cam.orthographicSize = Mathf.Lerp(_introHoldOrthoSize, _introTargetOrthoSize, eased);

            if (t >= 1f)
            {
                transform.position = _introTargetPos;
                _introState = IntroState.Idle;
                var callback = _onIntroComplete;
                _onIntroComplete = null;
                enabled = false;
                callback?.Invoke();
            }
        }
    }
}
