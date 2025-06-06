using UnityEngine;

namespace LJH.BT
{
    /// <summary>
    /// 극한 상황에서만 작동하는 아레나 경계 체크 노드
    /// 설정된 임계값 이상의 아레나 경계에서만 Success 반환
    /// </summary>
    public class CriticalBoundaryCheckNode : BTNode
    {
        private float criticalThreshold = 0.95f; // 기본값: 95% 이상에서만 작동
        private float lastDistance = 0f;
        private bool wasTriggered = false;

        public CriticalBoundaryCheckNode(float threshold = 0.95f) 
            : base($"CriticalBoundary Node (>={threshold:F2})")
        {
            criticalThreshold = Mathf.Clamp01(threshold);
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
            lastDistance = normalizedDistance;

            // 극한 상황에서만 Success 반환
            if (normalizedDistance >= criticalThreshold)
            {
                if (!wasTriggered && enableLogging)
                    Debug.LogError($"[{nodeName}] 극한 상황 감지! Distance: {normalizedDistance:F2} (>= {criticalThreshold}) - 즉시 복귀 필요!");
                
                wasTriggered = true;
                state = NodeState.Success;
            }
            else
            {
                if (wasTriggered && enableLogging)
                    Debug.Log($"[{nodeName}] 극한 상황 해제. Distance: {normalizedDistance:F2}");
                
                wasTriggered = false;
                state = NodeState.Failure; // 임계값 미만에서는 다른 행동 허용
            }

            return state;
        }
        
        #region 추가 기능들
        
        /// <summary>
        /// 임계값 설정
        /// </summary>
        public void SetCriticalThreshold(float threshold)
        {
            criticalThreshold = Mathf.Clamp01(threshold);
            SetNodeName($"CriticalBoundary Node (>={criticalThreshold:F2})");
            
            if (enableLogging)
                Debug.Log($"[{nodeName}] 임계값 변경: {criticalThreshold:F2}");
        }
        
        /// <summary>
        /// 현재 임계값 반환
        /// </summary>
        public float GetCriticalThreshold() => criticalThreshold;
        
        /// <summary>
        /// 현재 정규화된 거리 반환
        /// </summary>
        public float GetCurrentDistance() => lastDistance;
        
        /// <summary>
        /// 극한 상황 여부 확인 (실행하지 않고 조건만 검사)
        /// </summary>
        public bool IsCriticalSituation(GameObservation observation)
        {
            if (!CheckBasicConditions()) return false;
            
            float distanceFromCenter = Vector3.Distance(observation.selfPosition, observation.arenaCenter);
            float normalizedDistance = distanceFromCenter / observation.arenaRadius;
            
            return normalizedDistance >= criticalThreshold;
        }
        
        /// <summary>
        /// 임계값까지의 여유 거리 반환
        /// </summary>
        public float GetDistanceToCritical(GameObservation observation)
        {
            float distanceFromCenter = Vector3.Distance(observation.selfPosition, observation.arenaCenter);
            float normalizedDistance = distanceFromCenter / observation.arenaRadius;
            
            if (normalizedDistance >= criticalThreshold)
                return 0f; // 이미 임계값 초과
            
            float distanceToCritical = (criticalThreshold - normalizedDistance) * observation.arenaRadius;
            return Mathf.Max(0f, distanceToCritical);
        }
        
        /// <summary>
        /// 위험도 점수 반환 (0.0 ~ 1.0, 1.0이 가장 위험)
        /// </summary>
        public float GetDangerLevel(GameObservation observation)
        {
            float distanceFromCenter = Vector3.Distance(observation.selfPosition, observation.arenaCenter);
            float normalizedDistance = distanceFromCenter / observation.arenaRadius;
            
            if (normalizedDistance < criticalThreshold)
            {
                // 임계값 이하: 임계값에 가까울수록 위험도 증가
                return (normalizedDistance / criticalThreshold) * 0.8f;
            }
            else
            {
                // 임계값 초과: 매우 위험
                float excessRatio = (normalizedDistance - criticalThreshold) / (1f - criticalThreshold);
                return 0.8f + excessRatio * 0.2f;
            }
        }
        
        /// <summary>
        /// 아레나 중심으로의 방향 벡터 반환
        /// </summary>
        public Vector3 GetDirectionToSafety(GameObservation observation)
        {
            return (observation.arenaCenter - observation.selfPosition).normalized;
        }
        
        /// <summary>
        /// 권장 대응 속도 반환
        /// </summary>
        public float GetUrgencyLevel()
        {
            if (wasTriggered)
                return 2.0f; // 매우 빠른 대응 필요
            else
                return 1.0f; // 일반 속도
        }
        
        /// <summary>
        /// 현재 상황이 트리거되었는지 확인
        /// </summary>
        public bool IsTriggered() => wasTriggered;
        
        /// <summary>
        /// 상황 요약 정보
        /// </summary>
        public string GetSituationSummary(GameObservation observation)
        {
            float danger = GetDangerLevel(observation);
            float distance = GetDistanceToCritical(observation);
            
            return $"위험도: {danger:F2}, 여유거리: {distance:F1}m, 트리거: {wasTriggered}";
        }
        
        /// <summary>
        /// 노드 리셋
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            wasTriggered = false;
            lastDistance = 0f;
        }
        
        /// <summary>
        /// 상태 정보 문자열 (디버깅용)
        /// </summary>
        public override string GetStatusString()
        {
            return base.GetStatusString() + $", 임계값: {criticalThreshold:F2}, 트리거: {wasTriggered}, 거리: {lastDistance:F2}";
        }
        
        #endregion
    }
}
