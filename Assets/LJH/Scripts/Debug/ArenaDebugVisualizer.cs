using UnityEngine;
using LJH.BT;

/// <summary>
/// 아레나 경계 시스템의 디버그 시각화를 위한 컴포넌트
/// Scene 뷰에서 아레나 경계선과 안전 구역을 표시
/// </summary>
public class ArenaDebugVisualizer : MonoBehaviour
{
    [Header("아레나 설정")]
    public Vector3 arenaCenter = Vector3.zero;
    public float arenaRadius = 10f;
    
    [Header("경계 레벨 표시")]
    public bool showWarningBoundary = true;
    public bool showDangerBoundary = true;
    public bool showCriticalBoundary = true;
    public bool showSafeZone = true;
    
    [Header("경계 임계값")]
    public float warningThreshold = 0.8f;   // 80%
    public float dangerThreshold = 0.9f;    // 90%
    public float criticalThreshold = 0.95f; // 95%
    public float safeZoneRadius = 0.7f;     // 70%
    
    [Header("시각화 설정")]
    public bool showLabels = true;
    public float labelHeight = 5f;
    public int circleSegments = 64;
    
    [Header("BT 에이전트 참조 (선택사항)")]
    public AggressiveBTAgent aggressiveAgent;
    public DefensiveBTAgent defensiveAgent;

    void OnDrawGizmos()
    {
        DrawArenaBoundaries();
        DrawAgentStatus();
    }

    void DrawArenaBoundaries()
    {
        // 전체 아레나 경계 (흰색)
        Gizmos.color = Color.white;
        DrawCircle(arenaCenter, arenaRadius, 2f);
        
        // 경고 경계선 (노란색)
        if (showWarningBoundary)
        {
            Gizmos.color = Color.yellow;
            DrawCircle(arenaCenter, arenaRadius * warningThreshold, 1f);
            
            if (showLabels)
            {
                DrawLabel(arenaCenter + Vector3.forward * (arenaRadius * warningThreshold), 
                         $"Warning ({warningThreshold * 100:F0}%)", Color.yellow);
            }
        }
        
        // 위험 경계선 (주황색)
        if (showDangerBoundary)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            DrawCircle(arenaCenter, arenaRadius * dangerThreshold, 1f);
            
            if (showLabels)
            {
                DrawLabel(arenaCenter + Vector3.right * (arenaRadius * dangerThreshold), 
                         $"Danger ({dangerThreshold * 100:F0}%)", new Color(1f, 0.5f, 0f));
            }
        }
        
        // 치명적 경계선 (빨간색)
        if (showCriticalBoundary)
        {
            Gizmos.color = Color.red;
            DrawCircle(arenaCenter, arenaRadius * criticalThreshold, 1.5f);
            
            if (showLabels)
            {
                DrawLabel(arenaCenter + Vector3.back * (arenaRadius * criticalThreshold), 
                         $"Critical ({criticalThreshold * 100:F0}%)", Color.red);
            }
        }
        
        // 안전 구역 (녹색)
        if (showSafeZone)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // 반투명 녹색
            DrawFilledCircle(arenaCenter, arenaRadius * safeZoneRadius);
            
            Gizmos.color = Color.green;
            DrawCircle(arenaCenter, arenaRadius * safeZoneRadius, 1f);
            
            if (showLabels)
            {
                DrawLabel(arenaCenter + Vector3.left * (arenaRadius * safeZoneRadius), 
                         $"Safe Zone ({safeZoneRadius * 100:F0}%)", Color.green);
            }
        }
        
        // 아레나 중심점
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(arenaCenter, 0.5f);
        
        if (showLabels)
        {
            DrawLabel(arenaCenter + Vector3.up * 2f, "Arena Center", Color.magenta);
        }
    }

    void DrawAgentStatus()
    {
        // 공격형 에이전트 상태 표시
        if (aggressiveAgent != null && aggressiveAgent.Controller != null)
        {
            DrawAgentInfo(aggressiveAgent.Controller, Color.red, "Aggressive");
        }
        
        // 수비형 에이전트 상태 표시
        if (defensiveAgent != null && defensiveAgent.Controller != null)
        {
            DrawAgentInfo(defensiveAgent.Controller, Color.blue, "Defensive");
        }
    }

    void DrawAgentInfo(AgentController agent, Color color, string type)
    {
        Vector3 agentPos = agent.transform.position;
        float distanceFromCenter = Vector3.Distance(agentPos, arenaCenter);
        float normalizedDistance = distanceFromCenter / arenaRadius;
        
        // 에이전트에서 아레나 중심으로의 선
        Gizmos.color = color;
        Gizmos.DrawLine(agentPos, arenaCenter);
        
        // 에이전트 위치 표시
        Gizmos.DrawWireSphere(agentPos, 0.3f);
        
        // 거리 정보 표시
        if (showLabels)
        {
            string status = GetBoundaryStatus(normalizedDistance);
            Color statusColor = GetStatusColor(normalizedDistance);
            
            DrawLabel(agentPos + Vector3.up * 3f, 
                     $"{type}\nDist: {normalizedDistance:F2} ({distanceFromCenter:F1}m)\nStatus: {status}", 
                     statusColor);
        }
    }

    string GetBoundaryStatus(float normalizedDistance)
    {
        if (normalizedDistance >= criticalThreshold) return "CRITICAL!";
        if (normalizedDistance >= dangerThreshold) return "Danger";
        if (normalizedDistance >= warningThreshold) return "Warning";
        if (normalizedDistance <= safeZoneRadius) return "Safe";
        return "Normal";
    }

    Color GetStatusColor(float normalizedDistance)
    {
        if (normalizedDistance >= criticalThreshold) return Color.red;
        if (normalizedDistance >= dangerThreshold) return new Color(1f, 0.5f, 0f);
        if (normalizedDistance >= warningThreshold) return Color.yellow;
        if (normalizedDistance <= safeZoneRadius) return Color.green;
        return Color.white;
    }

    void DrawCircle(Vector3 center, float radius, float lineWidth = 1f)
    {
        float angleStep = 360f / circleSegments;
        
        for (int i = 0; i < circleSegments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
            
            // Unity Gizmos는 선 굵기를 직접 설정할 수 없으므로 여러 선으로 표현
            for (int j = 0; j < lineWidth; j++)
            {
                float offset = j * 0.05f;
                Vector3 offset1 = new Vector3(0, offset, 0);
                Gizmos.DrawLine(point1 + offset1, point2 + offset1);
            }
        }
    }

    void DrawFilledCircle(Vector3 center, float radius)
    {
        // 반투명 원을 그리기 위한 간단한 메쉬 표현
        int segments = 32;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
            
            // 삼각형으로 채우기
            Gizmos.DrawLine(center, point1);
            Gizmos.DrawLine(point1, point2);
            Gizmos.DrawLine(point2, center);
        }
    }

    void DrawLabel(Vector3 position, string text, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(position + Vector3.up * labelHeight, text, 
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = color },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            });
#endif
    }

    /// <summary>
    /// BattleManager에서 아레나 설정을 자동으로 가져오기
    /// </summary>
    public void SyncWithBattleManager()
    {
#if UNITY_2023_1_OR_NEWER
        var battleManager = FindFirstObjectByType<BattleManager>();
#else
        var battleManager = FindObjectOfType<BattleManager>();
#endif
        if (battleManager != null)
        {
            // BattleManager의 아레나 설정과 동기화
            // 실제 필드명은 BattleManager 구현에 따라 조정 필요
            arenaCenter = Vector3.zero; // battleManager.arenaCenter;
            arenaRadius = 10f; // battleManager.arenaRadius;
            
            Debug.Log($"BattleManager와 동기화 완료 - Center: {arenaCenter}, Radius: {arenaRadius}");
        }
        else
        {
            Debug.LogWarning("BattleManager를 찾을 수 없습니다!");
        }
    }
}
