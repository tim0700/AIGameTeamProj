using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 아레나 경계를 감지하는 노드
    /// 에이전트가 경계에 가까워지면 Success를 반환하여 복귀 행동을 트리거
    /// </summary>
    public class CheckArenaBoundaryNode : BTNode
    {
        [Header("경계 감지 설정")]
        public float warningThreshold = 0.8f;   // 아레나 반지름의 80%
        public float dangerThreshold = 0.9f;    // 아레나 반지름의 90%
        public float criticalThreshold = 0.95f; // 아레나 반지름의 95%

        public enum BoundaryLevel 
        { 
            Safe,      // 안전 (80% 미만)
            Warning,   // 경고 (80-90%)
            Danger,    // 위험 (90-95%)
            Critical   // 치명적 (95% 이상)
        }

        private BoundaryLevel currentLevel = BoundaryLevel.Safe;

        public CheckArenaBoundaryNode() { }

        public CheckArenaBoundaryNode(float warning, float danger, float critical)
        {
            warningThreshold = warning;
            dangerThreshold = danger;
            criticalThreshold = critical;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 현재 위치와 아레나 중심점 간의 거리 계산
            float distanceFromCenter = Vector3.Distance(
                observation.selfPosition, 
                observation.arenaCenter
            );

            // 정규화된 거리 (0~1, 1이 경계)
            float normalizedDistance = distanceFromCenter / observation.arenaRadius;

            // 경계 레벨 판단
            if (normalizedDistance >= criticalThreshold)
            {
                currentLevel = BoundaryLevel.Critical;
                Debug.LogWarning($"[CheckArenaBoundary] CRITICAL! Distance: {normalizedDistance:F2} (>= {criticalThreshold})");
                return NodeState.Success; // 즉시 복귀 필요
            }
            else if (normalizedDistance >= dangerThreshold)
            {
                currentLevel = BoundaryLevel.Danger;
                Debug.LogWarning($"[CheckArenaBoundary] DANGER! Distance: {normalizedDistance:F2} (>= {dangerThreshold})");
                return NodeState.Success; // 복귀 필요
            }
            else if (normalizedDistance >= warningThreshold)
            {
                currentLevel = BoundaryLevel.Warning;
                Debug.Log($"[CheckArenaBoundary] Warning - Distance: {normalizedDistance:F2} (>= {warningThreshold})");
                return NodeState.Success; // 주의 필요
            }
            else
            {
                currentLevel = BoundaryLevel.Safe;
                return NodeState.Failure; // 안전 - 다른 행동 수행 가능
            }
        }

        /// <summary>
        /// 현재 경계 레벨 반환 (디버깅용)
        /// </summary>
        public BoundaryLevel GetCurrentLevel()
        {
            return currentLevel;
        }

        /// <summary>
        /// 시각적 디버깅을 위한 Gizmo 그리기
        /// </summary>
        public void DrawDebugGizmos(Vector3 arenaCenter, float arenaRadius)
        {
            // Warning 경계선 (노란색)
            Gizmos.color = Color.yellow;
            DrawCircle(arenaCenter, arenaRadius * warningThreshold);

            // Danger 경계선 (주황색)
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            DrawCircle(arenaCenter, arenaRadius * dangerThreshold);

            // Critical 경계선 (빨간색)
            Gizmos.color = Color.red;
            DrawCircle(arenaCenter, arenaRadius * criticalThreshold);
        }

        private void DrawCircle(Vector3 center, float radius)
        {
            int segments = 64;
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
                Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

                Gizmos.DrawLine(point1, point2);
            }
        }
    }
}
