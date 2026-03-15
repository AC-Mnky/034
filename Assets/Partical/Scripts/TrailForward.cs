using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class VelocityDirectionalTrail : MonoBehaviour
{
    [Header("节点引用")]
    [Tooltip("拖入带有Rigidbody2D的节点物体")]
    public GameObject targetNode;

    [Header("方向控制")]
    [Tooltip("方向夹角阈值（度），小于此值时视为正向")]
    [Range(0f, 180f)]
    public float angleThreshold = 90f;

    [Tooltip("最小速度阈值，低于此速度不显示拖尾")]
    public float minSpeed = 0.5f;

    [Header("拖尾控制")]
    [Tooltip("最大发射率（当完全正向时）")]
    public float maxEmissionRate = 30f;

    [Tooltip("是否使用渐变效果")]
    public bool useLerp = true;

    [Tooltip("渐变速度")]
    public float lerpSpeed = 5f;

    [Header("调试")]
    [Tooltip("在Scene视图中显示方向")]
    public bool showDebugLines = true;

    private ParticleSystem _trailParticles;
    private ParticleSystem.EmissionModule _emission;
    private Rigidbody2D _rb;
    private float _currentEmissionRate = 0f;

    void Start()
    {
        // 获取ParticleSystem组件
        _trailParticles = GetComponent<ParticleSystem>();
        if (_trailParticles == null)
        {
            Debug.LogError("VelocityDirectionalTrail: 未找到ParticleSystem组件！");
            return;
        }

        _emission = _trailParticles.emission;

        // 如果没有手动指定节点，尝试自动查找
        if (targetNode == null)
        {
            targetNode = transform.parent?.gameObject ?? gameObject;
            Debug.LogWarning($"VelocityDirectionalTrail: 未指定节点，自动使用{targetNode.name}");
        }

        // 获取Rigidbody2D
        _rb = targetNode.GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            Debug.LogError($"VelocityDirectionalTrail: 节点{targetNode.name}上没有Rigidbody2D组件！");
        }

        // 初始时关闭发射
        SetEmissionRate(0f);
    }

    void Update()
    {
        if (_trailParticles == null || _rb == null) return;

        // 获取速度和旋转
        Vector2 velocity = _rb.velocity;
        float speed = velocity.magnitude;

        // 如果速度太小，直接关闭拖尾
        if (speed < minSpeed)
        {
            SetEmissionRate(0f);
            if (showDebugLines)
            {
                Debug.DrawRay(targetNode.transform.position, velocity.normalized * 2, Color.gray);
            }
            return;
        }

        // 计算速度方向（弧度）
        float velocityAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg; // 转换为度

        // 获取节点旋转（假设rotation是度）
        float nodeRotation = _rb.rotation; // 如果是弧度，需要转换：_rb.rotation * Mathf.Rad2Deg

        // 计算角度差（0-180度）
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(nodeRotation, velocityAngle));

        // 计算正向系数（0-1）：1表示完全正向，0表示垂直或反向
        float forwardFactor = 0f;

        if (angleDiff <= angleThreshold)
        {
            // 在阈值内，计算正向系数
            forwardFactor = 1f - (angleDiff / angleThreshold);
        }

        // 根据正向系数计算发射率
        float targetEmissionRate = forwardFactor * maxEmissionRate;

        // 应用发射率
        if (useLerp && lerpSpeed > 0)
        {
            _currentEmissionRate = Mathf.Lerp(_currentEmissionRate, targetEmissionRate, lerpSpeed * Time.deltaTime);
        }
        else
        {
            _currentEmissionRate = targetEmissionRate;
        }

        SetEmissionRate(_currentEmissionRate);

        // 调试显示
        if (showDebugLines)
        {
            // 节点方向
            Vector2 nodeDirection = new Vector2(Mathf.Cos(nodeRotation * Mathf.Deg2Rad), Mathf.Sin(nodeRotation * Mathf.Deg2Rad));
            Debug.DrawRay(targetNode.transform.position, nodeDirection * 2, Color.blue);

            // 速度方向
            Debug.DrawRay(targetNode.transform.position, velocity.normalized * 2, Color.red);

            // 阈值范围
            float halfThreshold = angleThreshold * 0.5f;
            for (int i = -1; i <= 1; i += 2)
            {
                float angle = (nodeRotation + i * halfThreshold) * Mathf.Deg2Rad;
                Vector2 thresholdDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Debug.DrawRay(targetNode.transform.position, thresholdDir * 1.5f, Color.green);
            }

            // 显示角度差
            Debug.Log($"速度角度: {velocityAngle:F1}°, 节点角度: {nodeRotation:F1}°, 角度差: {angleDiff:F1}°, 正向系数: {forwardFactor:F2}, 发射率: {_currentEmissionRate:F1}");
        }
    }

    private void SetEmissionRate(float rate)
    {
        if (_emission.enabled != (rate > 0))
        {
            _emission.enabled = rate > 0;
        }

        if (rate > 0)
        {
            var rateOverTime = _emission.rateOverTime;
            rateOverTime.constant = rate;
            _emission.rateOverTime = rateOverTime;
        }
    }

    // 外部调用：立即停止所有拖尾粒子
    public void ClearTrail()
    {
        if (_trailParticles != null)
        {
            _trailParticles.Clear();
        }
    }

    // 外部调用：获取当前状态
    public float GetCurrentAngleDiff()
    {
        if (_rb == null) return 0f;

        Vector2 velocity = _rb.velocity;
        if (velocity.magnitude < 0.1f) return 180f;

        float velocityAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        float nodeRotation = _rb.rotation;
        return Mathf.Abs(Mathf.DeltaAngle(nodeRotation, velocityAngle));
    }

    // 外部调用：获取正向系数
    public float GetForwardFactor()
    {
        float angleDiff = GetCurrentAngleDiff();
        if (angleDiff > angleThreshold) return 0f;
        return 1f - (angleDiff / angleThreshold);
    }

    // 在编辑器中显示阈值范围
    void OnDrawGizmosSelected()
    {
        if (!showDebugLines || targetNode == null || _rb == null) return;

        Gizmos.color = Color.green;
        float nodeRotation = _rb.rotation;
        Vector3 center = targetNode.transform.position;

        for (int i = 0; i <= 20; i++)
        {
            float angle1 = (nodeRotation - angleThreshold * 0.5f + i * angleThreshold / 20) * Mathf.Deg2Rad;
            float angle2 = (nodeRotation - angleThreshold * 0.5f + (i + 1) * angleThreshold / 20) * Mathf.Deg2Rad;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * 1.5f;
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * 1.5f;

            Gizmos.DrawLine(point1, point2);
        }
    }
}