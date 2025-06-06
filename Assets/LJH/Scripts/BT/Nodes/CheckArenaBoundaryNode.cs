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
        private BoundaryLevel lastLevel = BoundaryLevel.Safe;
        private float lastCheckTime = 0f;

        public CheckArenaBoundaryNode() : base("CheckArenaBoundary Node")
        {
        }

        public CheckArenaBoundaryNode(float warning, float danger, float critical) 
            : base($"CheckArenaBoundary Node (W:{warning:F1}/D:{danger:F1}/C:{critical:F1})")
        {
            warningThreshold = warning;
            dangerThreshold = danger;
            criticalThreshold = critical;
        }

        public override NodeState Evaluate(GameObservation observation)
        {
            // 기본 조건 확인 (새로운 BTNode 기능 활용)
            if (!CheckBasicConditions())
            {
                return NodeState.Failure;
            }

            // 현재 위치와 아레나 중심점 간의 거리 계산
            float distanceFromCenter = Vector3.Distance(
                observation.selfPosition, 
                observation.arenaCenter
            );

            // 정규화된 거리 (0~1, 1이 경계)
            float normalizedDistance = distanceFromCenter / observation.arenaRadius;

            // 이전 레벨 저장
            lastLevel = currentLevel;
            lastCheckTime = Time.time;

            // 경계 레벨 판단
            if (normalizedDistance >= criticalThreshold)
            {
                currentLevel = BoundaryLevel.Critical;
                if (enableLogging)
                    Debug.LogError($"[{nodeName}] CRITICAL! Distance: {normalizedDistance:F2} (>= {criticalThreshold})");
                
                state = NodeState.Success; // 즉시 복귀 필요
            }
            else if (normalizedDistance >= dangerThreshold)
            {
                currentLevel = BoundaryLevel.Danger;
                if (enableLogging)
                    Debug.LogWarning($"[{nodeName}] DANGER! Distance: {normalizedDistance:F2} (>= {dangerThreshold})");
                
                state = NodeState.Success; // 복귀 필요
            }
            else if (normalizedDistance >= warningThreshold)
            {
                currentLevel = BoundaryLevel.Warning;
                if (enableLogging)
                    Debug.Log($"[{nodeName}] Warning - Distance: {normalizedDistance:F2} (>= {warningThreshold})");
                
                state = NodeState.Success; // 주의 필요
            }
            else
            {
                currentLevel = BoundaryLevel.Safe;
                state = NodeState.Failure; // 안전 - 다른 행동 수행 가능
            }

            // 레벨 변화 로깅
            if (currentLevel != lastLevel && enableLogging)
            {
                Debug.Log($"[{nodeName}] 경계 레벨 변화: {lastLevel} → {currentLevel}");
            }

            return state;
        }

        #region 추가 기능들
        
        /// <summary>
        /// 경계 임계값들 설정
        /// </summary>
        public void SetThresholds(float warning, float danger, float critical)
        {
            warningThreshold = Mathf.Clamp01(warning);
            dangerThreshold = Mathf.Clamp01(danger);
            criticalThreshold = Mathf.Clamp01(critical);
            
            // 순서 보장 (warning < danger < critical)
            if (warningThreshold > dangerThreshold)
                warningThreshold = dangerThreshold - 0.05f;
            if (dangerThreshold > criticalThreshold)
                dangerThreshold = criticalThreshold - 0.05f;
            
            SetNodeName($"CheckArenaBoundary Node (W:{warningThreshold:F1}/D:{dangerThreshold:F1}/C:{criticalThreshold:F1})");
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 임계값 변경: W={warningThreshold:F2}, D={dangerThreshold:F2}, C={criticalThreshold:F2}");
        }
        
        /// <summary>
        /// 현재 경계 레벨 반환
        /// </summary>
        public BoundaryLevel GetCurrentLevel() => currentLevel;
        
        /// <summary>
        /// 이전 경계 레벨 반환
        /// </summary>
        public BoundaryLevel GetLastLevel() => lastLevel;
        
        /// <summary>
        /// 현재 정규화된 거리 반환
        /// </summary>
        public float GetNormalizedDistance(GameObservation observation)
        {
            float distanceFromCenter = Vector3.Distance(observation.selfPosition, observation.arenaCenter);
            return distanceFromCenter / observation.arenaRadius;
        }
        
        /// <summary>
        /// 다음 경계 레벨까지의 여유 거리
        /// </summary>
        public float GetDistanceToNextLevel(GameObservation observation)
        {
            float currentNormalized = GetNormalizedDistance(observation);
            
            switch (currentLevel)
            {
                case BoundaryLevel.Safe:
                    return (warningThreshold - currentNormalized) * observation.arenaRadius;
                case BoundaryLevel.Warning:
                    return (dangerThreshold - currentNormalized) * observation.arenaRadius;
                case BoundaryLevel.Danger:
                    return (criticalThreshold - currentNormalized) * observation.arenaRadius;
                case BoundaryLevel.Critical:
                    return 0f; // 이미 최대 위험
                default:
                    return 0f;
            }
        }
        
        /// <summary>
        /// 경계 상황인지 확인 (Warning 이상)
        /// </summary>
        public bool IsBoundaryWarning(GameObservation observation)
        {
            return GetNormalizedDistance(observation) >= warningThreshold;
        }
        
        /// <summary>
        /// 위험한 경계 상황인지 확인 (Danger 이상)
        /// </summary>
        public bool IsBoundaryDanger(GameObservation observation)
        {
            return GetNormalizedDistance(observation) >= dangerThreshold;
        }
        
        /// <summary>
        /// 치명적인 경계 상황인지 확인 (Critical)
        /// </summary>
        public bool IsBoundaryCritical(GameObservation observation)
        {
            return GetNormalizedDistance(observation) >= criticalThreshold;
        }
        
        /// <summary>
        /// 경계 위험도 점수 (0.0 ~ 1.0)
        /// </summary>
        public float GetBoundaryRisk(GameObservation observation)
        {
            float normalized = GetNormalizedDistance(observation);
            
            if (normalized < warningThreshold)
                return 0f;
            else if (normalized < dangerThreshold)
                return (normalized - warningThreshold) / (dangerThreshold - warningThreshold) * 0.33f;
            else if (normalized < criticalThreshold)
                return 0.33f + (normalized - dangerThreshold) / (criticalThreshold - dangerThreshold) * 0.33f;
            else
                return 0.66f + (normalized - criticalThreshold) / (1f - criticalThreshold) * 0.34f;
        }
        
        /// <summary>
        /// 아레나 중심 방향 벡터 반환
        /// </summary>
        public Vector3 GetDirectionToCenter(GameObservation observation)
        {
            return (observation.arenaCenter - observation.selfPosition).normalized;
        }
        
        /// <summary>
        /// 권장 복귀 속도 반환 (위험도에 따라)
        /// </summary>
        public float GetRecommendedReturnSpeed()
        {
            switch (currentLevel)
            {
                case BoundaryLevel.Warning: return 1.0f;
                case BoundaryLevel.Danger: return 1.3f;
                case BoundaryLevel.Critical: return 1.8f;
                default: return 1.0f;
            }
        }
        
        /// <summary>
        /// 경계 상태 요약 정보
        /// </summary>
        public string GetBoundaryStatus(GameObservation observation)
        {
            float distance = GetNormalizedDistance(observation);
            float risk = GetBoundaryRisk(observation);
            return $"레벨: {currentLevel}, 거리: {distance:F2}, 위험도: {risk:F2}";
        }
        
        /// <summary>
        /// 상태 정보 문자열 (디버깅용)
        /// </summary>
        public override string GetStatusString()
        {
            return base.GetStatusString() + $", 경계레벨: {currentLevel}";
        }
        
        #endregion

        #region 시각적 디버깅
        
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
        
        #endregion
    }
}
